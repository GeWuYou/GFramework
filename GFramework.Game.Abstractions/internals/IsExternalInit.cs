// IsExternalInit.cs
// This type is required to support init-only setters and record types
// when targeting netstandard2.0 or older frameworks.

#if NETSTANDARD2_0 || NETFRAMEWORK || NETCOREAPP2_0
using System.ComponentModel;

namespace System.Runtime.CompilerServices;

/// <summary>
///     用于标记仅初始化 setter 的特殊类型
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IsExternalInit
{
}
#endif