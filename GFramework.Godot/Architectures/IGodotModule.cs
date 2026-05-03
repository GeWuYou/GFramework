// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Architectures;
using Godot;

namespace GFramework.Godot.Architectures;

/// <summary>
///     Godot模块接口，定义了Godot引擎中模块的基本行为和属性
/// </summary>
public interface IGodotModule : IArchitectureModule
{
    /// <summary>
    ///     获取模块关联的Godot节点
    /// </summary>
    Node Node { get; }

    /// <summary>
    ///     当模块被附加到架构时调用
    /// </summary>
    /// <param name="architecture">要附加到的架构实例</param>
    void OnAttach(Architecture architecture);

    /// <summary>
    ///     当模块从架构分离时调用
    /// </summary>
    void OnDetach();
}