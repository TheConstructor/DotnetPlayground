using System.Text;
using DotnetPlayground.Extensions;
using Xunit;

namespace DotnetPlayground.Tests.Extensions;

public class StringBuilderExtensionsTest
{
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