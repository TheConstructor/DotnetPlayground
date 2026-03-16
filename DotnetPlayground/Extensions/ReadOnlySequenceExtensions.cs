using System;
using System.Buffers;
using System.Text;

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
                    if (currentSegment.Span.IndexOf(value, comparisonType) is var pos and >= 0)
                    {
                        return checked((int) (sequence.GetOffset(startOfCurrentSegment) + pos));
                    }
                    else if (startOfNextSegment.GetObject() == null)
                    {
                        return -1;
                    }

                    valueRuneCount ??= value.CountRunes();
                    var remainder = currentSegment.Span.TakeRunesFromEnd(valueRuneCount.Value - 1, out var runesFound);
                    if (remainder.IsEmpty)
                    {
                        goto nextSegment;
                    }

                    var lastRuneHalved = Rune.DecodeLastFromUtf16(remainder, out _, out var charsConsumed) == OperationStatus.NeedMoreData
                        && charsConsumed == 1;
                    if (lastRuneHalved)
                    {
                        remainder = remainder[..^1];
                        runesFound--;
                    }

                    var valueStart = value.TakeRunesFromStart(runesFound, out var valueRunesFound);
                    if (valueRunesFound != runesFound)
                    {
                        goto nextSegment;
                    }

                    while (!remainder.IsEmpty)
                    {
                        if (!remainder.Equals(valueStart, comparisonType))
                        {
                            remainder = remainder.SkipFirstRune();
                            valueStart = valueStart.SkipLastRune();
                            continue;
                        }

                        var offsetFromSegmentStart = lastRuneHalved
                            ? currentSegment.Span.Length - remainder.Length - 1
                            : currentSegment.Span.Length - remainder.Length;
                        var sequenceStartingAtI = sequence.Slice(startOfCurrentSegment)
                            .Slice(offsetFromSegmentStart);
                        if (sequenceStartingAtI.Length < valueRuneCount.Value)
                        {
                            return -1;
                        }

                        buffer ??= MemoryPool<char>.Shared.Rent(valueRuneCount.Value * MaxCharsPerRune);
                        var lengthToCopy = Math.Min(valueRuneCount.Value * MaxCharsPerRune, checked((int)sequenceStartingAtI.Length));
                        var bufferSpan = buffer.Memory.Span[..lengthToCopy];
                        sequenceStartingAtI.Slice(0, lengthToCopy).CopyTo(bufferSpan);
                        if (bufferSpan.TakeRunesFromStart(valueRuneCount.Value, out _).Equals(value, comparisonType))
                        {
                            return checked((int) sequence.GetOffset(startOfCurrentSegment) + offsetFromSegmentStart);
                        }
                    }

                    nextSegment:
                    startOfCurrentSegment = startOfNextSegment;
                }
            }
            finally
            {
                if (buffer != null)
                {
                    buffer.Memory.Span[..value.Length].Clear();
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