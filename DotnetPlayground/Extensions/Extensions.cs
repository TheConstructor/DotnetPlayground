using System.Runtime.CompilerServices;

namespace DotnetPlayground.Extensions;

public static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RepresentAs<T>(this T value) => value;
}