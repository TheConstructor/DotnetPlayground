using System;
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
}