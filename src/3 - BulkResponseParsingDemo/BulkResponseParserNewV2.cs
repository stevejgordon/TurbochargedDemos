using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BulkResponseParsingDemo
{
    public static class BulkResponseParserNewV2
    {
        //private static ReadOnlySpan<byte> IdPropertyNameBytes => new[] { (byte)'_', (byte)'i', (byte)'d' };
        private static ReadOnlySpan<byte> StatusPropertyNameBytes => new[] { (byte)'s', (byte)'t', (byte)'a', (byte)'t', (byte)'u', (byte)'s' };
        private static ReadOnlySpan<byte> ErrorSpan => new[] { (byte)'"', (byte)'e', (byte)'r', (byte)'r', (byte)'o', (byte)'r', (byte)'s', (byte)'"', (byte)':', (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };

        public static async Task<int> ParseFromStreamAsync(Stream stream, string[] errors)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(32768); // 2^15 should fit an existing bucket size

            var processingState = new ProcessingState();

            try
            {
                await stream.ReadAsync(buffer.AsMemory().Slice(0, 32));

                if (buffer.AsSpan().IndexOf(ErrorSpan) > -1)
                    return 0;

                JsonReaderState state = default;
                var leftOver = 32;
                var isFinalBlock = false;

                while (!isFinalBlock)
                {
                    var dataLength = await stream.ReadAsync(buffer.AsMemory(leftOver, buffer.Length - leftOver));

                    var dataSize = dataLength + leftOver;
                    isFinalBlock = dataSize == stream.Length || dataSize == 0;

                    state = ParseErrors(buffer.AsSpan(0, dataSize), isFinalBlock, ref state, errors, ref processingState);

                    leftOver = dataSize - (int)state.BytesConsumed;

                    if (leftOver != 0)
                        buffer.AsSpan(dataSize - leftOver).CopyTo(buffer); // can we avoid this copy?
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return processingState.TotalErrors;
        }

        public struct ProcessingState
        {
            public bool InsideArray; // never reset
            public int TotalErrors; // never reset

            public bool StatusPropertyFound;
            public int PropertyCount;

            public void Reset()
            {
                StatusPropertyFound = false;
                PropertyCount = 0;
            }
        }

        public static JsonReaderState ParseErrors(ReadOnlySpan<byte> dataUtf8, bool isFinalBlock, ref JsonReaderState state, string[] errors, ref ProcessingState processingState)
        {
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock, state);

            ReadOnlySpan<byte> tempBytes = null; // this works in a single call where we are immediately working with the full JSON bytes, but is safely re-entrant!! Dangerous and flaky!

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;

                switch (tokenType)
                {
                    case JsonTokenType.StartArray:
                        processingState.InsideArray = true;
                        break;

                    case JsonTokenType.PropertyName:
                        if (processingState.InsideArray)
                        {
                            processingState.PropertyCount++;

                            if (processingState.PropertyCount > 4 && json.ValueSpan.SequenceEqual(StatusPropertyNameBytes))
                            {
                                processingState.StatusPropertyFound = true;
                            }
                        }

                        break;

                    case JsonTokenType.String:
                        if (processingState.PropertyCount == 4) // may not be safe for all responses - investigation required
                        {
                            tempBytes = json.ValueSpan; // avoids a string allocation per ID since we don't need all strings, only errors
                        }

                        break;

                    case JsonTokenType.Number:
                        if (processingState.StatusPropertyFound) // this property moves on errors so can't cheat using the property count
                        {
                            if (json.GetInt32() == 400 && tempBytes != null)
                                errors[processingState.TotalErrors++] = Encoding.UTF8.GetString(tempBytes);

                            processingState.Reset();
                        }
                        break;
                }
            }

            return json.CurrentState;
        }
    }
}