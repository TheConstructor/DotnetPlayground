using System;
using System.Buffers;
using System.Text;

namespace DotnetPlayground.Extensions;

public static class StringBuilderExtensions
{
    extension(StringBuilder stringBuilder)
    {
        public int IndexOf(
            ReadOnlySpan<char> value,
            int offset = 0,
            StringComparison comparisonType = StringComparison.Ordinal) =>
            stringBuilder.AsReadOnlySequence().IndexOf(value, offset, comparisonType);

        public int IndexOf(
            ReadOnlySpan<char> value,
            StringComparison comparisonType = StringComparison.Ordinal) =>
            stringBuilder.AsReadOnlySequence().IndexOf(value, 0, comparisonType);

        public bool StartsWith(
            ReadOnlySpan<char> value,
            StringComparison comparisonType = StringComparison.Ordinal) =>
            stringBuilder.AsReadOnlySequence().StartsWith(value, comparisonType);

        public StringBuilder Replace(int start, int length, ReadOnlySpan<char> newValue)
        {
            if (stringBuilder.AsReadOnlySequence()
                .TryGetSpanWithoutCopy(start, length, out var oldValue))
            {
                return stringBuilder.Replace(oldValue, newValue, start, length);
            }
            else
            {
                return stringBuilder.Remove(start, length)
                    .Insert(start, newValue);
            }
        }

        public ReadOnlySequence<char> AsReadOnlySequence()
        {
            var chunks = stringBuilder.GetChunks();

            if (!chunks.MoveNext())
            {
                return ReadOnlySequence<char>.Empty;
            }

            var runningIndex = 0L;
            var firstSegment = new StringBuilderSegment(chunks.Current, runningIndex);
            var lastSegment = firstSegment;

            while (chunks.MoveNext())
            {
                var currentSegment = lastSegment;
                runningIndex += currentSegment.Length;
                lastSegment = new StringBuilderSegment(chunks.Current, runningIndex);
                currentSegment.SetNext(lastSegment);
            }

            return new ReadOnlySequence<char>(firstSegment, 0, lastSegment, lastSegment.Length);
        }
    }

    private class StringBuilderSegment : ReadOnlySequenceSegment<char>
    {
        public int Length => Memory.Length;
        
        public StringBuilderSegment(ReadOnlyMemory<char> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public void SetNext(StringBuilderSegment next)
        {
            if (Next != null
                || next.RunningIndex != RunningIndex + Memory.Length)
            {
                throw new ArgumentException();
            }

            Next = next;
        }
    }
}