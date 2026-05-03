// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pool;
using Godot;

namespace GFramework.Godot.Pool;

/// <summary>
///     可池化节点接口，继承自IPoolableObject接口
///     用于定义可以被对象池管理的Godot节点类型
/// </summary>
public interface IPoolableNode : IPoolableObject
{
    /// <summary>
    ///     将当前对象转换为Node类型
    /// </summary>
    /// <returns>返回当前对象对应的Node实例</returns>
    Node AsNode();
}