using System;
using System.Text;

namespace DotnetPlayground.Extensions;

public static class ReadOnlySpanExtensions
{
    extension(ReadOnlySpan<char> span)
    {
        public int CountRunes()
        {
            var count = 0;

            using var enumerator = span.EnumerateRunes();
            while (enumerator.MoveNext())
            {
                count++;
            }

            return count;
        }

        public ReadOnlySpan<char> SkipFirstRune()
        {
            if (span.IsEmpty)
            {
                return ReadOnlySpan<char>.Empty;
            }

            Rune.DecodeFromUtf16(span, out _, out var charsConsumed);
            return span[charsConsumed..];
        }

        public ReadOnlySpan<char> SkipLastRune()
        {
            if (span.IsEmpty)
            {
                return ReadOnlySpan<char>.Empty;
            }

            Rune.DecodeLastFromUtf16(span, out _, out var charsConsumed);
            return span[..^charsConsumed];
        }

        public ReadOnlySpan<char> TakeRunesFromStart(int count, out int runes)
        {
            runes = 0;
            if (span.IsEmpty || count < 1)
            {
                return ReadOnlySpan<char>.Empty;
            }

            var pos = 0;
            var remainder = span;
            while (!remainder.IsEmpty && runes < count)
            {
                Rune.DecodeFromUtf16(remainder, out _, out var charsConsumed);
                if (charsConsumed == 0)
                {
                    break;
                }

                runes++;
                pos += charsConsumed;
                remainder = remainder[charsConsumed..];
            }

            return span[..pos];
        }

        public ReadOnlySpan<char> TakeRunesFromEnd(int count, out int runes)
        {
            runes = 0;
            if (span.IsEmpty || count < 1)
            {
                return ReadOnlySpan<char>.Empty;
            }

            var pos = 0;
            var remainder = span;
            while (!remainder.IsEmpty && runes < count)
            {
                Rune.DecodeLastFromUtf16(remainder, out _, out var charsConsumed);
                if (charsConsumed == 0)
                {
                    break;
                }

                runes++;
                pos += charsConsumed;
                remainder = remainder[..^charsConsumed];
            }

            return span[^pos..];
        }
    }
}