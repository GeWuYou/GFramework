// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.SourceGenerators.Abstractions.Architectures;

/// <summary>
///     声明架构模块需要自动注册的系统类型。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegisterSystemAttribute(Type systemType) : Attribute
{
    /// <summary>
    ///     获取要注册的系统类型。
    /// </summary>
    public Type SystemType { get; } = systemType;
}
