using System;
using System.Buffers;
using System.Diagnostics;

namespace DotnetPlayground.Extensions;

public static class ReadOnlySequenceExtensions
{
    private const int MaxCharsPerRune = 2;

    extension(in ReadOnlySequence<char> sequence)
    {
        public int IndexOf(ReadOnlySpan<char> value,
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
            IMemoryOwner<char>? buffer = null;
            try
            {
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
                    var remainder = currentSegment.Span.TakeRunesFromEnd(
                        lastRuneHalved
                            ? valueRuneCount.Value
                            : valueRuneCount.Value - 1,
                        out var runesFound);

                    if (!remainder.IsEmpty)
                    {
                        if (lastRuneHalved)
                        {
                            remainder = remainder[..^1];
                            runesFound--;
                        }

                        var valueStart = value.TakeRunesFromStart(runesFound, out var valueRunesFound);
                        // We counted the Runes in value and runesFound should be less
                        Debug.Assert(valueRunesFound == runesFound);

                        while (true)
                        {
                            // Do the last Runes of this segment match the first Runes of value?
                            while (!remainder.Equals(valueStart, comparisonType))
                            {
                                // No -> retry with one Rune less
                                remainder = remainder.SkipFirstRune();
                                valueStart = valueStart.SkipLastRune();
                            }

                            if (remainder.IsEmpty && !lastRuneHalved)
                            {
                                break;
                            }

                            // We already matched the start of value, let's see if we can find the rest.
                            // 1. Get a ReadOnlySpan<char> that starts with the contents of remainder -> sequenceStartingWithRemainder
                            var offsetFromSegmentStart = lastRuneHalved
                                ? currentSegment.Span.Length - remainder.Length - 1
                                : currentSegment.Span.Length - remainder.Length;
                            var sequenceStartingWithRemainder =
                                sequence.Slice(startOfCurrentSegment).Slice(offsetFromSegmentStart);
                            // 2. Check if there are enough chars in sequenceStartingWithRemainder, to be able to contain value
                            if (sequenceStartingWithRemainder.Length < valueRuneCount.Value)
                            {
                                // There is no need to look any further, there are too few characters left in the sequence
                                return -1;
                            }

                            // 3. Copy all Runes from sequenceStartingWithRemainder, that could be value, to buffer
                            buffer ??= MemoryPool<char>.Shared.Rent(valueRuneCount.Value * MaxCharsPerRune);
                            var lengthToCopy = Math.Min(
                                valueRuneCount.Value * MaxCharsPerRune,
                                checked((int) sequenceStartingWithRemainder.Length));
                            var bufferSpan = buffer.Memory.Span[..lengthToCopy];
                            sequenceStartingWithRemainder.Slice(0, lengthToCopy).CopyTo(bufferSpan);

                            // 4. Did we find value?
                            if (bufferSpan.TakeRunesFromStart(valueRuneCount.Value, out _)
                                .Equals(value, comparisonType))
                            {
                                return checked((int) sequence.GetOffset(startOfCurrentSegment) +
                                               offsetFromSegmentStart);
                            }

                            if (remainder.IsEmpty)
                            {
                                break;
                            }

                            // Retry with one Rune less
                            remainder = remainder.SkipFirstRune();
                            valueStart = valueStart.SkipLastRune();
                        }
                    }

                    // startOfNextSegment will be updated by sequence.TryGet(..) with the start of the segment after, so let's store the previous value
                    startOfCurrentSegment = startOfNextSegment;
                }
            }
            finally
            {
                if (buffer != null)
                {
                    buffer.Memory.Span[..(valueRuneCount.GetValueOrDefault() * MaxCharsPerRune)].Clear();
                    buffer.Dispose();
                }
            }

            return -1;
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