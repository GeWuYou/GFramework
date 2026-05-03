// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

#nullable enable
namespace GFramework.Godot.SourceGenerators.Abstractions;

/// <summary>
///     标记 Godot 节点事件处理方法，Source Generator 会为其生成事件绑定与解绑逻辑。
/// </summary>
/// <remarks>
///     该特性通过节点字段名与事件名建立声明式订阅关系，适用于将
///     <c>_Ready()</c> / <c>_ExitTree()</c> 中重复的 <c>+=</c> 与 <c>-=</c> 样板代码
///     收敛到生成器中统一维护。
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class BindNodeSignalAttribute : Attribute
{
    /// <summary>
    ///     初始化 <see cref="BindNodeSignalAttribute" /> 的新实例。
    /// </summary>
    /// <param name="nodeFieldName">目标节点字段名。</param>
    /// <param name="signalName">目标节点上的 CLR 事件名。</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="nodeFieldName" /> 或 <paramref name="signalName" /> 为 <see langword="null" />。
    /// </exception>
    public BindNodeSignalAttribute(
        string nodeFieldName,
        string signalName)
    {
        NodeFieldName = nodeFieldName ?? throw new ArgumentNullException(nameof(nodeFieldName));
        SignalName = signalName ?? throw new ArgumentNullException(nameof(signalName));
    }

    /// <summary>
    ///     获取目标节点字段名。
    /// </summary>
    public string NodeFieldName { get; }

    /// <summary>
    ///     获取目标节点上的 CLR 事件名。
    /// </summary>
    public string SignalName { get; }
}