// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

// IsExternalInit.cs
// This type is required to support init-only setters and record types
// when targeting netstandard2.0 or older frameworks.

#if !NET5_0_OR_GREATER
using System.ComponentModel;

// ReSharper disable CheckNamespace

namespace System.Runtime.CompilerServices;

/// <summary>
/// 提供一个占位符类型，用于支持 C# 9.0 的 init 访问器功能。
/// 该类型在 .NET 5.0 及更高版本中已内置，因此仅在较低版本的 .NET 中定义。
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}
#endif