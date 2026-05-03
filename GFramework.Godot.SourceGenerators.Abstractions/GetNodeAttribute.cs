// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

#nullable enable
namespace GFramework.Godot.SourceGenerators.Abstractions;

/// <summary>
///     标记 Godot 节点字段，Source Generator 会为其生成节点获取逻辑。
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class GetNodeAttribute : Attribute
{
    /// <summary>
    ///     初始化 <see cref="GetNodeAttribute" /> 的新实例。
    /// </summary>
    public GetNodeAttribute()
    {
    }

    /// <summary>
    ///     初始化 <see cref="GetNodeAttribute" /> 的新实例，并指定节点路径。
    /// </summary>
    /// <param name="path">节点路径。</param>
    public GetNodeAttribute(string path)
    {
        Path = path;
    }

    /// <summary>
    ///     获取或设置节点路径。未设置时将根据字段名推导。
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///     获取或设置节点是否必填。默认为 true。
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    ///     获取或设置节点查找模式。默认为 <see cref="NodeLookupMode.Auto" />。
    /// </summary>
    public NodeLookupMode Lookup { get; set; } = NodeLookupMode.Auto;
}