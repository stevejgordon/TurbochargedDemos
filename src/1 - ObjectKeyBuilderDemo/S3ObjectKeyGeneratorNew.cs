using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ObjectKeyBuilderDemo
{
    // demoware - this prototype example doesn't cover all edge cases!
    public partial class S3ObjectKeyGeneratorNew
    {
        [GeneratedRegex("^[a-z0-9_]+$", RegexOptions.IgnoreCase)]
        private static partial Regex ValidationRegex();

        private const int MaxStackAllocationSize = 256;
        private const char JoinChar = '/';

        // data is stored in metadata within the binary, so no allocations
        private static ReadOnlySpan<char> InvalidPart =>
            ['i', 'n', 'v', 'a', 'l', 'i', 'd'];
        private static ReadOnlySpan<char> UnknownPart =>
            ['u', 'n', 'k', 'n', 'o', 'w', 'n'];
        private static ReadOnlySpan<char> DateFormat =>
            ['y', 'y', 'y', 'y', '/', 'M', 'M', '/', 'd', 'd', '/', 'H', 'H', '/'];
        private static ReadOnlySpan<char> JsonSuffix =>
            ['.', 'j', 's', 'o', 'n'];

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
                eventContext.EventDateUtc.TryFormat(objectKeySpan[currentPosition..],
                    out var charsWritten, DateFormat, CultureInfo.InvariantCulture);

                currentPosition += charsWritten;
            }

            MemoryExtensions.ToLowerInvariant(eventContext.MessageId,
                objectKeySpan[currentPosition..]);

            currentPosition += eventContext.MessageId.Length;

            JsonSuffix.CopyTo(objectKeySpan[currentPosition..]); // copy suffix

            var key = objectKeySpan.ToString(); // allocate the final string

            return key;
        }

        private static void BuildPart(string input, Span<char> output, ref int currentPosition)
        {
            var length = input?.Length ?? 0;

            // check if empty string, if so, unknown
            if (length == 0 || MemoryExtensions.IsWhiteSpace(input))
            {
                UnknownPart.CopyTo(output[currentPosition..]);
                currentPosition += UnknownPart.Length;
            }
            else
            {
                // check valid
                if (ValidationRegex().IsMatch(input))
                {
                    // if valid, lowercase
                    MemoryExtensions.ToLowerInvariant(input, output[currentPosition..]);
                    currentPosition += length;
                }
                else
                {
                    InvalidPart.CopyTo(output[currentPosition..]);
                    currentPosition += InvalidPart.Length;
                }
            }

            output[currentPosition++] = JoinChar;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RemoveSpaces(Span<char> objectKey, ref int currentPosition)
        {
            var remaining = objectKey[..currentPosition];

            var indexOfSpace = remaining.IndexOf(' '); // do we have spaces

            if (indexOfSpace < 0)
                return;

            while (indexOfSpace != -1) // while there are space
            {
                remaining[indexOfSpace] = '_'; // replace at the index of the space
                remaining = remaining[(indexOfSpace + 1)..]; // slice past the replaced char
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
        private static int CalculatePartLength(ReadOnlySpan<char> input) =>
            ValidationRegex().IsMatch(input) ? input.Length + 1 : InvalidPart.Length + 1;
    }
}
