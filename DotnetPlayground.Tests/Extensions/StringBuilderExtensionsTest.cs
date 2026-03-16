using System;
using System.Text;
using DotnetPlayground.Extensions;
using Xunit;

namespace DotnetPlayground.Tests.Extensions;

public class StringBuilderExtensionsTest
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
        var sb = new StringBuilder("Hello World!");

        Assert.Equal(result, sb.IndexOf(needle, offset, stringComparison));
    }

    [Theory]
    [InlineData(2, 1, "/", "He/lo World!")]
    [InlineData(6, 5, "You", "Hello You!")]
    [InlineData(0, 5, "Welcome", "Welcome World!")]
    [InlineData(4, 2, ", ", "Hell, World!")]
    public void Replace(int start, int length, string newValue, string expected)
    {
        var sb = new StringBuilder("Hello!", 6);
        sb.Insert(5, " World");
        
        Assert.Equal(expected, sb.Replace(start, length, newValue).ToString());
    }

    [Theory]
    [InlineData(6)] // Only enough for Hello! -> two segments
    [InlineData(12)] // Enough for the full value -> one segment
    public void AsReadOnlySequenceTest(int capacity)
    {
        var sb = new StringBuilder("Hello!", capacity);
        sb.Insert(5, " World");
        
        Assert.Equal("Hello World!", sb.AsReadOnlySequence().ToString());
    }
}