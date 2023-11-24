using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ObjectKeyBuilderDemo
{
    public static partial class S3ObjectKeyGeneratorNewV2
    {
        [GeneratedRegex("^[a-z0-9 ]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ValidationRegex();

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
            var result = string.Create(length, eventContext, KeyBuilderAction);
            return result;
        }

        private static readonly SpanAction<char, EventContext> KeyBuilderAction = (span, eventContext) =>
        {
            var currentPosition = 0;

            BuildPart(eventContext.Product, span, ref currentPosition);
            BuildPart(eventContext.SiteKey, span, ref currentPosition);
            BuildPart(eventContext.EventName, span, ref currentPosition);

            span[..(currentPosition - 1)].Replace(' ', '_'); // Since .NET 8 - Optimised way to replace

            if (eventContext.EventDateUtc != default)
            {
                eventContext.EventDateUtc.TryFormat(span[currentPosition..], out var bytesWritten, 
                    DateFormat, CultureInfo.InvariantCulture);
                currentPosition += bytesWritten;
            }

            MemoryExtensions.ToLowerInvariant(eventContext.MessageId, span[currentPosition..]);

            JsonSuffix.CopyTo(span[^5..]);
        };

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

            static int CalculatePartLength(ReadOnlySpan<char> input) =>
                ValidationRegex().IsMatch(input) ? input.Length + 1 : InvalidPart.Length + 1;
        }
    }
}