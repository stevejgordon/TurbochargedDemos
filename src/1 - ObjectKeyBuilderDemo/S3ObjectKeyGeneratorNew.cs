﻿using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ObjectKeyBuilderDemo
{
    // demoware - this prototype example doesn't cover all edge cases!
    public class S3ObjectKeyGeneratorNew
    {
        private const int MaxStackAllocationSize = 256;
        private const char JoinChar = '/';

        // ugly but compiler can optmise away a memcopy
        private static ReadOnlySpan<char> InvalidPart =>
            new[] { 'i', 'n', 'v', 'a', 'l', 'i', 'd' };
        private static ReadOnlySpan<char> UnknownPart =>
            new[] { 'u', 'n', 'k', 'n', 'o', 'w', 'n' };
        private static ReadOnlySpan<char> DateFormat =>
            new[] { 'y', 'y', 'y', 'y', '/', 'M', 'M', '/', 'd', 'd', '/', 'H', 'H', '/' };
        private static ReadOnlySpan<char> JsonSuffix =>
            new[] { '.', 'j', 's', 'o', 'n' };

        public static string GenerateSafeObjectKey(EventContext eventContext)
        {
            var length = CalculateLength(eventContext);

            Span<char> objectKeySpan = length <= MaxStackAllocationSize
                ? stackalloc char[length]
                : new char[length]; // this allocations. If we actually expected this to
                                    // ever be the case using the ArrayPool would be more efficient.

            var currentPosition = 0;

            BuildPart(eventContext.Product, objectKeySpan, ref currentPosition);
            BuildPart(eventContext.SiteKey, objectKeySpan, ref currentPosition);
            BuildPart(eventContext.EventName, objectKeySpan, ref currentPosition);

            RemoveSpaces(objectKeySpan, ref currentPosition);

            if (eventContext.EventDateUtc != default)
            {
                eventContext.EventDateUtc.TryFormat(objectKeySpan.Slice(currentPosition),
                    out var charsWritten, DateFormat, CultureInfo.InvariantCulture);

                currentPosition += charsWritten;
            }

            MemoryExtensions.ToLowerInvariant(eventContext.MessageId,
                objectKeySpan.Slice(currentPosition)); 

            currentPosition += eventContext.MessageId.Length;

            JsonSuffix.CopyTo(objectKeySpan.Slice(currentPosition)); // copy suffix

            var key = objectKeySpan.ToString(); // allocate the final string

            return key;
        }

        private static void BuildPart(string input, Span<char> output, ref int currentPosition)
        {
            var productLength = input?.Length ?? 0;

            // check if empty string, if so, unknown
            if (productLength == 0 || MemoryExtensions.IsWhiteSpace(input))
            {
                UnknownPart.CopyTo(output.Slice(currentPosition));
                currentPosition += UnknownPart.Length;
            }
            else
            {
                // check valid
                var isValid = true;
                foreach (var c in input)
                {
                    if (!char.IsLetterOrDigit(c) && c != ' ')
                    {
                        isValid = false;
                        break;
                    }
                }

                if (!isValid) // not valid
                {
                    InvalidPart.CopyTo(output.Slice(currentPosition));
                    currentPosition += InvalidPart.Length;
                }
                else
                {
                    // if valid, lowercase
                    MemoryExtensions.ToLowerInvariant(input, output.Slice(currentPosition));
                    currentPosition += productLength;
                }
            }

            output[currentPosition++] = JoinChar;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RemoveSpaces(Span<char> objectKey, ref int currentPosition)
        {
            var remaining = objectKey.Slice(0, currentPosition);

            var indexOfSpace = remaining.IndexOf(' '); // do we have spaces

            if (indexOfSpace < 0)
                return;

            while (indexOfSpace != -1) // while there are space
            {
                remaining[indexOfSpace] = '_'; // replace at the index of the space
                remaining = remaining.Slice(indexOfSpace + 1); // slice past the replaced char
                indexOfSpace = remaining.IndexOf(' '); // do we have spaces
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateLength(EventContext eventContext)
        {
            var length = 0;

            length += string.IsNullOrEmpty(eventContext.Product)
                ? UnknownPart.Length + 1
                : CalculatePartLength(eventContext.Product);
            length += string.IsNullOrEmpty(eventContext.SiteKey)
                ? UnknownPart.Length + 1
                : CalculatePartLength(eventContext.SiteKey);
            length += string.IsNullOrEmpty(eventContext.EventName)
                ? UnknownPart.Length + 1
                : CalculatePartLength(eventContext.EventName);

            if (eventContext.EventDateUtc != default)
                length += DateFormat.Length;

            length += eventContext.MessageId.Length;
            length += JsonSuffix.Length;

            return length;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculatePartLength(ReadOnlySpan<char> input)
        {
            var isValid = true;

            foreach (var c in input)
            {
                if (!char.IsLetterOrDigit(c) && c != ' ')
                {
                    isValid = false;
                    break;
                }
            }

            return isValid ? input.Length + 1 : InvalidPart.Length + 1;
        }
    }
}
