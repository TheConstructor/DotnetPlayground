using System;
using System.Buffers;

namespace DotnetPlayground.Extensions;

public static class ReadOnlySequenceExtensions
{
    public static int IndexOf(
        this in ReadOnlySequence<char> sequence,
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

        return MultiSegmentIndexOf(sequence, value, offset, comparisonType);
    }

    private static int MultiSegmentIndexOf(
        in ReadOnlySequence<char> sequence,
        ReadOnlySpan<char> value,
        int offset,
        StringComparison comparisonType)
    {
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

                for (var i = Math.Max(0, currentSegment.Length - value.Length); i < currentSegment.Length - 1; i++)
                {
                    if (!value.StartsWith(currentSegment.Span[i..], comparisonType))
                    {
                        continue;
                    }

                    var sequenceStartingAtI = sequence.Slice(startOfCurrentSegment).Slice(i);
                    if (sequenceStartingAtI.Length < value.Length)
                    {
                        goto nextSegment;
                    }

                    buffer ??= MemoryPool<char>.Shared.Rent(value.Length);
                    var bufferSpan = buffer.Memory.Span[..value.Length];
                    sequenceStartingAtI.Slice(0, value.Length).CopyTo(bufferSpan);
                    if (bufferSpan.Equals(value, comparisonType))
                    {
                        return checked((int) (sequence.GetOffset(startOfCurrentSegment) + i));
                    }
                }

                nextSegment: startOfCurrentSegment = startOfNextSegment;
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