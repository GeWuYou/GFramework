// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.SourceGenerators.Abstractions.UI;

/// <summary>
///     声明导出集合应当转发到哪个注册器成员及其方法。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class RegisterExportedCollectionAttribute(string registryMemberName, string registerMethodName)
    : Attribute
{
    /// <summary>
    ///     获取注册器字段或属性名称。
    /// </summary>
    public string RegistryMemberName { get; } = registryMemberName;

    /// <summary>
    ///     获取注册方法名称。
    /// </summary>
    public string RegisterMethodName { get; } = registerMethodName;
}
