// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.SourceGenerators.Abstractions.Rule;

/// <summary>
///     标记字段需要自动注入服务集合。
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class GetServicesAttribute : Attribute
{
}
