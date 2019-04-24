using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BulkResponseParsingDemo
{
    public static class BulkResponseParserNew
    {
        private static readonly byte[] errorsPropertyNameBytes = Encoding.UTF8.GetBytes("errors");
        private static readonly byte[] idPropertyNameBytes = Encoding.UTF8.GetBytes("_id");
        private static readonly byte[] statusPropertyNameBytes = Encoding.UTF8.GetBytes("status");

        public static async Task<(bool success, IEnumerable<string> failedIds)> FromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1024);

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
                    int dataLength = await stream.ReadAsync(buffer.AsMemory(leftOver, buffer.Length - leftOver), cancellationToken);

                    int dataSize = dataLength + leftOver;
                    bool isFinalBlock = dataSize == 0;

                    state = ParseErrors(buffer.AsSpan(0, dataSize), isFinalBlock, state, ref foundErrorsProperty, ref hasErrors,
                        ref insideMainObject, ref insideItemsArray, ref errors);

                    if (foundErrorsProperty && !hasErrors)
                    {
                        break; // there are no errors so we can short-circuit here.
                    }

                    leftOver = dataSize - (int)state.BytesConsumed;

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

        public static JsonReaderState ParseErrors(ReadOnlySpan<byte> dataUtf8, bool isFinalBlock, JsonReaderState state,
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
                        if (json.ValueSpan.SequenceEqual(errorsPropertyNameBytes))
                        {
                            foundErrorsProperty = true;
                        }

                        if (startObjectCount == 2)
                        {
                            if (json.ValueSpan.SequenceEqual(idPropertyNameBytes))
                            {
                                idPropertyFound = true;
                            }
                            if (json.ValueSpan.SequenceEqual(statusPropertyNameBytes))
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

            return json.CurrentState;
        }
    }
}
