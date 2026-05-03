// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.SourceGenerators.Abstractions.Rule;

/// <summary>
///     标记该类需要自动实现 IContextAware
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ContextAwareAttribute : Attribute
{
}
