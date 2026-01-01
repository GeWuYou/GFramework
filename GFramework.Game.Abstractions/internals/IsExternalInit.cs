// IsExternalInit.cs
// This type is required to support init-only setters and record types
// when targeting netstandard2.0 or older frameworks.

#pragma warning disable S2094 // Remove this empty class
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
#pragma warning restore S2094