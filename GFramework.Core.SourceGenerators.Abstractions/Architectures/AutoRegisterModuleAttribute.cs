// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.SourceGenerators.Abstractions.Architectures;

/// <summary>
///     标记架构模块类型，Source Generator 会根据注册特性生成 <c>Install</c> 方法。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoRegisterModuleAttribute : Attribute
{
}
