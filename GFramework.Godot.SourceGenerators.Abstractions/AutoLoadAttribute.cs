// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

#nullable enable
namespace GFramework.Godot.SourceGenerators.Abstractions;

/// <summary>
///     显式声明某个 Godot 节点类型与 <c>project.godot</c> 中 AutoLoad 名称之间的映射关系。
/// </summary>
/// <remarks>
///     当 AutoLoad 条目无法仅靠类型名唯一推断到 C# 节点类型时，
///     可以通过该特性为生成器提供稳定的强类型映射入口。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoLoadAttribute : Attribute
{
    /// <summary>
    ///     初始化 <see cref="AutoLoadAttribute" /> 的新实例。
    /// </summary>
    /// <param name="name">在 <c>project.godot</c> 中声明的 AutoLoad 名称。</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="name" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="name" /> 为空字符串或仅包含空白字符。
    /// </exception>
    public AutoLoadAttribute(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("AutoLoad name cannot be empty or whitespace.", nameof(name));
        }

        Name = name;
    }

    /// <summary>
    ///     获取在 <c>project.godot</c> 中声明的 AutoLoad 名称。
    /// </summary>
    public string Name { get; }
}
