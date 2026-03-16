using System.Runtime.CompilerServices;

namespace DotnetPlayground;

public static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RepresentAs<T>(this T value) => value;
}