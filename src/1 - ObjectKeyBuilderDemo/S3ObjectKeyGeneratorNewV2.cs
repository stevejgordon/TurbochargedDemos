using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ObjectKeyBuilderDemo
{
    public static class S3ObjectKeyGeneratorNewV2
    {
        private const char JoinChar = '/';
        private static ReadOnlySpan<char> InvalidPart => 
            new [] { 'i', 'n', 'v', 'a', 'l', 'i', 'd' };
        private static ReadOnlySpan<char> UnknownPart => 
            new[] { 'u', 'n', 'k', 'n', 'o', 'w', 'n' };
        private static ReadOnlySpan<char> DateFormat => 
            new[] { 'y', 'y', 'y', 'y', '/', 'M', 'M', '/', 'd', 'd', '/', 'H', 'H', '/' };
        private static ReadOnlySpan<char> JsonSuffix => 
            new[] { '.', 'j', 's', 'o', 'n' };

        public static string GenerateSafeObjectKey(EventContext eventContext)
        {
            var productMetaData = LoadMetaData(eventContext.Product);
            var siteKeyMetaData = LoadMetaData(eventContext.SiteKey);
            var eventNameMetaData = LoadMetaData(eventContext.EventName);

            var length = productMetaData.Length + siteKeyMetaData.Length + 
                eventNameMetaData.Length;

            CalculateLength(ref eventContext, ref length);

            return string.Create(length, (eventContext, productMetaData, siteKeyMetaData, eventNameMetaData), KeyBuilderAction);
        }

        private static readonly SpanAction<char, ValueTuple<EventContext, PartMetaData, PartMetaData, PartMetaData>> KeyBuilderAction = (span, ctx) =>
        {
            var currentPosition = 0;

            var (eventContext, productMetaData, siteKeyMetaData, eventNameMetaData) = ctx;

            BuildPart(eventContext.Product, ref span, ref currentPosition, ref productMetaData);
            BuildPart(eventContext.SiteKey, ref span, ref currentPosition, ref siteKeyMetaData);
            BuildPart(eventContext.EventName, ref span, ref currentPosition, ref eventNameMetaData);

            if (eventContext.EventDateUtc != default)
            {
                eventContext.EventDateUtc.TryFormat(span.Slice(currentPosition), out var bytesWritten, DateFormat, CultureInfo.InvariantCulture);
                currentPosition += bytesWritten;
            }

            MemoryExtensions.ToLowerInvariant(eventContext.MessageId, span.Slice(currentPosition));

            JsonSuffix.CopyTo(span.Slice(span.Length - 5));
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PartMetaData LoadMetaData(ReadOnlySpan<char> input)
        {
            var isEmpty = false;
            var isValid = true;
            var hasSpaces = false;

            if (input.Length > 0 && !input.IsWhiteSpace())
            {
                foreach (var c in input)
                {
                    if (!char.IsLetterOrDigit(c) && c != ' ')
                    {
                        isValid = false;
                        break;
                    }

                    if (c == ' ')
                    {
                        hasSpaces = true;
                    }
                }
            }
            else
            {
                isEmpty = true;
            }

            return new PartMetaData(isEmpty, isValid, hasSpaces, isEmpty || !isValid 
                ? InvalidPart.Length
                : input.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateLength(ref EventContext eventContext, ref int length)
        {
            length += 3; // the separators for the first keys which we always need

            if (eventContext.EventDateUtc != default)
                length += DateFormat.Length;

            length += eventContext.MessageId.Length;
            length += JsonSuffix.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BuildPart(ReadOnlySpan<char> input, ref Span<char> output, ref int currentPosition, ref PartMetaData metaData)
        {
            if (metaData.IsEmpty)
            {
                UnknownPart.CopyTo(output.Slice(currentPosition));
                currentPosition += UnknownPart.Length;
            }
            else if (!metaData.IsValid)
            {
                InvalidPart.CopyTo(output.Slice(currentPosition));
                currentPosition += InvalidPart.Length;
            }
            else
            {
                input.ToLowerInvariant(output.Slice(currentPosition));
                currentPosition += input.Length;

                if (metaData.HasSpaces)
                {
                    RemoveSpaces(output.Slice(currentPosition - input.Length, input.Length));
                }
            }

            output[currentPosition++] = JoinChar;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RemoveSpaces(Span<char> objectKey)
        {
            var indexOfSpace = objectKey.IndexOf(' ');

            if (indexOfSpace < 0)
                return;

            while (indexOfSpace != -1)
            {
                objectKey[indexOfSpace] = '_';
                objectKey = objectKey.Slice(indexOfSpace + 1);
                indexOfSpace = objectKey.IndexOf(' ');
            }
        }

        private readonly struct PartMetaData
        {
            public PartMetaData(bool isEmpty, bool isValid, bool hasSpaces, int length)
            {
                IsEmpty = isEmpty;
                IsValid = isValid;
                HasSpaces = hasSpaces;
                Length = length;
            }

            public bool IsEmpty { get; }
            public int Length { get; }
            public bool IsValid { get; }
            public bool HasSpaces { get; }
        }
    }
}