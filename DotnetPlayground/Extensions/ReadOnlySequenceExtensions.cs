using System;
using System.Buffers;

namespace DotnetPlayground.Extensions;

public static class ReadOnlySequenceExtensions
{
    private const sbyte SequenceTooShort = -1;
    private const sbyte No = 0;
    private const sbyte Yes = 1;
    private const int MaxCharsPerRune = 2;

    extension(in ReadOnlySequence<char> sequence)
    {
        public int IndexOf(
            ReadOnlySpan<char> value,
            int offset = 0,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (sequence.IsSingleSegment)
            {
                return sequence.FirstSpan[offset..].IndexOf(value, comparisonType) is var pos and >= 0
                    ? offset + pos
                    : -1;
            }

            return sequence.MultiSegmentIndexOf(value, offset, comparisonType);
        }

        private int MultiSegmentIndexOf(
            ReadOnlySpan<char> value,
            int offset,
            StringComparison comparisonType)
        {
            int? valueRuneCount = null;

            var startOfCurrentSegment = sequence.GetPosition(offset);
            var startOfNextSegment = startOfCurrentSegment;
            while (sequence.TryGet(ref startOfNextSegment, out var currentSegment, advance: true))
            {
                // Can we find a complete match inside the current segment?
                if (currentSegment.Span.IndexOf(value, comparisonType) is var pos and >= 0)
                {
                    return checked((int) (sequence.GetOffset(startOfCurrentSegment) + pos));
                }
                // Is this the last segment?
                else if (startOfNextSegment.GetObject() == null)
                {
                    return -1;
                }

                // There is another segment, but there was no complete match inside the current
                // => we need at least one Rune from the next segment to form a match.

                // It could be, that the current segment ends with the first half of a Rune and the remaining char is in the next segment
                var lastRuneHalved = currentSegment.Span.IsLastRuneHalved();

                // We count the Runes in value and then start by looking at one less full Rune from the current segment
                // to see, if it could be the start of value
                valueRuneCount ??= value.CountRunes();
                var remainder = (lastRuneHalved
                        ? currentSegment.Span[..^1]
                        : currentSegment.Span)
                    .TakeRunesFromEnd(
                        valueRuneCount.Value - 1,
                        out var runesFound);

                SequencePosition position;
                sbyte startsWith;
                if (!remainder.IsEmpty)
                {
                    var valueStart = value.TakeRunesFromStart(runesFound, out _);
                    do
                    {
                        while (!remainder.Equals(valueStart, comparisonType))
                        {
                            runesFound--;
                            remainder = remainder.SkipFirstRune();
                            valueStart = valueStart.SkipLastRune();
                        }

                        if (remainder.IsEmpty)
                        {
                            break;
                        }

                        position = lastRuneHalved
                            ? sequence.GetPosition(currentSegment.Length - 1, startOfCurrentSegment)
                            : startOfNextSegment;
                        startsWith = sequence.Slice(position)
                            .MultiSegmentStartsWith(
                                value[valueStart.Length..],
                                valueRuneCount.Value - runesFound,
                                comparisonType);
                        switch (startsWith)
                        {
                            case Yes:
                                return checked((int) (sequence.GetOffset(startOfCurrentSegment)
                                                      + (lastRuneHalved
                                                          ? currentSegment.Length - remainder.Length - 1
                                                          : currentSegment.Length - remainder.Length)));
                            case SequenceTooShort:
                                return -1;
                        }

                        runesFound--;
                        remainder = remainder.SkipFirstRune();
                        valueStart = valueStart.SkipLastRune();
                    } while (!remainder.IsEmpty);
                }

                if (lastRuneHalved)
                {
                    position = sequence.GetPosition(
                        currentSegment.Length - 1,
                        startOfCurrentSegment);
                    startsWith = sequence.Slice(position)
                        .MultiSegmentStartsWith(
                            value,
                            valueRuneCount.Value,
                            comparisonType);
                    switch (startsWith)
                    {
                        case Yes:
                            return checked((int) sequence.GetOffset(position));
                        case SequenceTooShort:
                            return -1;
                    }
                }

                // startOfNextSegment will be updated by sequence.TryGet(..) with the start of the segment after, so let's store the previous value
                startOfCurrentSegment = startOfNextSegment;
            }

            return -1;
        }

        public bool StartsWith(
            ReadOnlySpan<char> value,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (sequence.IsSingleSegment)
            {
                return sequence.FirstSpan.StartsWith(value, comparisonType);
            }

            return sequence.MultiSegmentStartsWith(value, value.CountRunes(), comparisonType) == Yes;
        }

        private sbyte MultiSegmentStartsWith(
            ReadOnlySpan<char> value,
            int valueRuneCount,
            StringComparison comparisonType)
        {
            Span<char> buffer = stackalloc char[2];
            var startOfNextSegment = sequence.Start;
            var lastRuneHalved = false;
            while (sequence.TryGet(ref startOfNextSegment, out var currentSegment, advance: true))
            {
                var currentSpan = currentSegment.Span.TakeRunesFromStart(valueRuneCount, out var currentSpanRuneCount);
                if (currentSpan.IsEmpty)
                {
                    continue;
                }

                if (lastRuneHalved)
                {
                    buffer[1] = currentSpan[0];
                    var joinedRune = buffer.TakeRunesFromStart(1, out _);
                    if (!joinedRune
                            .Equals(value.TakeRunesFromStart(1, out _), comparisonType))
                    {
                        return No;
                    }

                    value = value.SkipFirstRune();
                    if (value.IsEmpty)
                    {
                        return Yes;
                    }
                    valueRuneCount--;
                    currentSpan = currentSpan[(joinedRune.Length - 1)..];
                    currentSpanRuneCount--;
                }

                lastRuneHalved = currentSpan.IsLastRuneHalved();
                if (lastRuneHalved)
                {
                    currentSpan = currentSpan[..^1];
                    currentSpanRuneCount--;
                }

                if (currentSpanRuneCount == valueRuneCount)
                {
                    return currentSpan.Equals(value, comparisonType) ? Yes : No;
                }

                var valueStart = value.TakeRunesFromStart(currentSpanRuneCount, out var valueStartRuneCount);
                if (!currentSpan.Equals(valueStart, comparisonType))
                {
                    return No;
                }

                value = value[valueStart.Length..];
                valueRuneCount -= valueStartRuneCount;
                if (lastRuneHalved)
                {
                    buffer[0] = currentSegment.Span[^1];
                }
            }

            if (lastRuneHalved
                && buffer[..1].Equals(value, comparisonType))
            {
                return Yes;
            }

            return SequenceTooShort;
        }
    }

    extension<T>(in ReadOnlySequence<T> sequence)
    {
        public bool TryGetSpanWithoutCopy(int start, int length, out ReadOnlySpan<T> span)
        {
            var subSequence = sequence.Slice(start, length);
            if (subSequence.IsSingleSegment)
            {
                span = subSequence.FirstSpan;
                return true;
            }
            else
            {
                span = ReadOnlySpan<T>.Empty;
                return false;
            }
        }

        public ReadOnlySpan<T> GetSpan(int start, int length)
        {
            var subSequence = sequence.Slice(start, length);
            if (subSequence.IsSingleSegment)
            {
                return subSequence.FirstSpan;
            }
            else
            {
                var result = new T[length];
                subSequence.CopyTo(result);
                return result;
            }
        }
    }
}