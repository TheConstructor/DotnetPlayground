using System;
using System.Buffers;
using System.Text;

namespace DotnetPlayground;

public static class StringBuilderExtensions
{
    public static ReadOnlySequence<char> AsReadOnlySequence(this StringBuilder stringBuilder)
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