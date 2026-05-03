// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Systems;

namespace GFramework.Ecs.Arch.Abstractions;

/// <summary>
///     Arch 系统适配器接口 - 桥接 Arch.System.ISystem&lt;T&gt; 到框架上下文
/// </summary>
/// <typeparam name="T">系统数据类型（通常是 float 表示 deltaTime）</typeparam>
public interface IArchSystemAdapter<T> : ISystem
{
    /// <summary>
    ///     更新系统
    /// </summary>
    /// <param name="t">系统数据参数（通常是 deltaTime）</param>
    void Update(in T t);
}