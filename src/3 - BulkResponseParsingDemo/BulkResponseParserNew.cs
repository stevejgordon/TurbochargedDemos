using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BulkResponseParsingDemo
{
    public static class BulkResponseParserNew
    {
        // demoware - this prototype example doesn't cover all edge cases!

        private static ReadOnlySpan<byte> ErrorsPropertyNameBytes => new[] { (byte)'e', (byte)'r', (byte)'r', (byte)'o', (byte)'r', (byte)'s' };
        private static ReadOnlySpan<byte> IdPropertyNameBytes => new[] { (byte)'_', (byte)'i', (byte)'d' };
        private static ReadOnlySpan<byte> StatusPropertyNameBytes => new[] { (byte)'s', (byte)'t', (byte)'a', (byte)'t', (byte)'u', (byte)'s' };

        public static async Task<(bool success, IEnumerable<string> failedIds)> FromStreamAsync(Stream stream, CancellationToken ct = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1024); // hardcoded as I know the length in this sample

            JsonReaderState state = default;
            int leftOver = 0;

            bool foundErrorsProperty = false;
            bool hasErrors = true;

            List<string> errors = null;
            var insideMainObject = false;
            var insideItemsArray = false;

            try
            {
                while (true)
                {
                    // could use pipelines here too
                    int dataLength = await stream.ReadAsync(buffer.AsMemory(leftOver, buffer.Length - leftOver), ct);

                    int dataSize = dataLength + leftOver;
                    bool isFinalBlock = dataSize == 0;

                    var consumed = ParseErrors(buffer.AsSpan(0, dataSize), isFinalBlock, ref state, ref foundErrorsProperty, ref hasErrors,
                        ref insideMainObject, ref insideItemsArray, ref errors);

                    if (foundErrorsProperty && !hasErrors)
                    {
                        break; // there are no errors so we can short-circuit here.
                    }

                    leftOver = dataSize - (int)consumed;

                    if (leftOver != 0)
                    {
                        buffer.AsSpan(dataSize - leftOver).CopyTo(buffer); // can we avoid this copy?
                    }

                    if (isFinalBlock)
                    {
                        break;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return (!hasErrors, errors);
        }

        public static long ParseErrors(ReadOnlySpan<byte> dataUtf8, bool isFinalBlock, ref JsonReaderState state,
            ref bool foundErrorsProperty, ref bool hasErrors, ref bool insideMainObject, ref bool insideItemsArray, ref List<string> errors)
        {
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock, state);

            var startObjectCount = 0; // risky if we've partially read into an object - rethink this - probably have a parsing state struct
            var idPropertyFound = false;
            string currentEventId = null;
            var statusPropertyFound = false;

            while (json.Read() && hasErrors)
            {
                JsonTokenType tokenType = json.TokenType;

                switch (tokenType)
                {
                    case JsonTokenType.StartObject:
                        if (!insideMainObject)
                            insideMainObject = true;

                        if (insideItemsArray && startObjectCount < 2)
                            startObjectCount++;

                        break;

                    case JsonTokenType.StartArray:
                        insideItemsArray = true;
                        break;

                    case JsonTokenType.PropertyName:
                        if (json.ValueSpan.SequenceEqual(ErrorsPropertyNameBytes))
                        {
                            foundErrorsProperty = true;
                        }

                        if (startObjectCount == 2)
                        {
                            if (json.ValueSpan.SequenceEqual(IdPropertyNameBytes))
                            {
                                idPropertyFound = true;
                            }
                            if (json.ValueSpan.SequenceEqual(StatusPropertyNameBytes))
                            {
                                statusPropertyFound = true;
                            }
                        }

                        break;

                    case JsonTokenType.False:
                        if (foundErrorsProperty && !insideItemsArray && startObjectCount == 0)
                        {
                            hasErrors = false;
                        }
                        break;

                    case JsonTokenType.True:
                        if (foundErrorsProperty && !insideItemsArray && startObjectCount == 0)
                        {
                            hasErrors = true;
                            errors = new List<string>();
                        }
                        break;

                    case JsonTokenType.String:
                        if (idPropertyFound == true)
                        {
                            currentEventId = json.GetString();
                        }

                        break;

                    case JsonTokenType.Number:
                        if (statusPropertyFound)
                        {
                            if (json.GetInt32() == 400)
                            {
                                errors.Add(currentEventId);
                            }

                            idPropertyFound = false;
                            currentEventId = null;
                            statusPropertyFound = false;
                            startObjectCount = 0;
                        }
                        break;
                }
            }

            state = json.CurrentState;

            return json.BytesConsumed;
        }
    }
}
