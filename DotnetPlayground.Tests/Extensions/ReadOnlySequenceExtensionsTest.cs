using System;
using System.Buffers;
using System.Text;
using DotnetPlayground.Extensions;
using Xunit;

namespace DotnetPlayground.Tests.Extensions;

public class ReadOnlySequenceExtensionsTest
{
    [Theory]
    [InlineData("", 0, StringComparison.Ordinal, 0)]
    [InlineData("", 6, StringComparison.Ordinal, 6)]
    [InlineData("Hello", 0, StringComparison.Ordinal, 0)]
    [InlineData("Hello", 1, StringComparison.Ordinal, -1)]
    [InlineData("hello", 0, StringComparison.Ordinal, -1)]
    [InlineData("hello", 0, StringComparison.OrdinalIgnoreCase, 0)]
    [InlineData("Hello World", 0, StringComparison.Ordinal, 0)]
    [InlineData("hello world", 0, StringComparison.Ordinal, -1)]
    [InlineData("hello world", 0, StringComparison.OrdinalIgnoreCase, 0)]
    [InlineData("World", 0, StringComparison.Ordinal, 6)]
    [InlineData("world", 0, StringComparison.Ordinal, -1)]
    [InlineData("world", 0, StringComparison.OrdinalIgnoreCase, 6)]
    [InlineData("World", 3, StringComparison.Ordinal, 6)]
    [InlineData("world", 3, StringComparison.Ordinal, -1)]
    [InlineData("world", 3, StringComparison.OrdinalIgnoreCase, 6)]
    [InlineData("World", 6, StringComparison.Ordinal, 6)]
    [InlineData("world", 6, StringComparison.Ordinal, -1)]
    [InlineData("world", 6, StringComparison.OrdinalIgnoreCase, 6)]
    [InlineData("World", 9, StringComparison.Ordinal, -1)]
    [InlineData("world", 9, StringComparison.Ordinal, -1)]
    [InlineData("world", 9, StringComparison.OrdinalIgnoreCase, -1)]
    [InlineData("World!!", 0, StringComparison.Ordinal, -1)]
    [InlineData("ello World!", 0, StringComparison.Ordinal, 1)]
    [InlineData("ello World!!", 0, StringComparison.Ordinal, -1)]
    public void IndexOfTest(string needle, int offset, StringComparison stringComparison, int result)
    {
        var sb = new StringBuilder("Hello!", 6); // Only enough for Hello! -> two segments
        sb.Insert(5, " World");

        Assert.Equal(result, sb.AsReadOnlySequence().IndexOf(needle, offset, stringComparison));
    }

    [Fact]
    public void MatchingWorksAcrossSegmentsWithSplitRunes()
    {
        var complete = ">🤷🤷🤷🤷<".ToCharArray();

        var sequence2Chars = AsEvenlySplitSequence(complete, 2);

        Assert.Equal(0, sequence2Chars.IndexOf(complete));
        Assert.Equal(0, sequence2Chars.IndexOf(">🤷"));
        Assert.Equal(0, sequence2Chars.IndexOf(">🤷🤷"));
        Assert.Equal(1, sequence2Chars.IndexOf("🤷"));
        Assert.Equal(1, sequence2Chars.IndexOf("🤷🤷"));
        Assert.Equal(3, sequence2Chars.IndexOf("🤷", 2));
        Assert.Equal(3, sequence2Chars.IndexOf("🤷🤷", 2));
        Assert.Equal(7, sequence2Chars.IndexOf("🤷<"));
        Assert.Equal(5, sequence2Chars.IndexOf("🤷🤷<"));

        var sequence4Chars = AsEvenlySplitSequence(complete, 4);

        Assert.Equal(0, sequence4Chars.IndexOf(complete));
        Assert.Equal(0, sequence4Chars.IndexOf(">🤷"));
        Assert.Equal(0, sequence4Chars.IndexOf(">🤷🤷"));
        Assert.Equal(1, sequence4Chars.IndexOf("🤷"));
        Assert.Equal(1, sequence4Chars.IndexOf("🤷🤷"));
        Assert.Equal(3, sequence2Chars.IndexOf("🤷", 2));
        Assert.Equal(3, sequence2Chars.IndexOf("🤷🤷", 2));
        Assert.Equal(7, sequence4Chars.IndexOf("🤷<"));
        Assert.Equal(5, sequence4Chars.IndexOf("🤷🤷<"));
    }

    private static ReadOnlySequence<char> AsEvenlySplitSequence(char[] array, int segmentLength)
    {
        var firstSegment = new Segment(new ReadOnlyMemory<char>(array, 0, Math.Min(segmentLength, array.Length)), 0);
        var lastSegment = firstSegment;

        for (var runningIndex = segmentLength; runningIndex < array.Length; runningIndex += segmentLength)
        {
            var currentSegment = lastSegment;
            lastSegment = new Segment(new ReadOnlyMemory<char>(array, runningIndex, Math.Min(segmentLength, array.Length - runningIndex)), runningIndex);
            currentSegment.SetNext(lastSegment);
        }

        return new ReadOnlySequence<char>(firstSegment, 0, lastSegment, lastSegment.Length);
    }

    private class Segment : ReadOnlySequenceSegment<char>
    {
        public int Length => Memory.Length;
        
        public Segment(ReadOnlyMemory<char> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public void SetNext(Segment next)
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