// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Core.Architectures;

/// <summary>
/// 基于 GameContext 的默认上下文提供者
/// </summary>
public sealed class GameContextProvider : IArchitectureContextProvider
{
    /// <summary>
    /// 获取当前的架构上下文（返回第一个注册的架构上下文）
    /// </summary>
    /// <returns>架构上下文实例</returns>
    public IArchitectureContext GetContext()
    {
        return GameContext.GetFirstArchitectureContext();
    }

    /// <summary>
    /// 尝试获取指定类型的架构上下文
    /// </summary>
    /// <typeparam name="T">架构上下文类型</typeparam>
    /// <param name="context">输出的上下文实例</param>
    /// <returns>如果成功获取则返回true，否则返回false</returns>
    public bool TryGetContext<T>(out T? context) where T : class, IArchitectureContext
    {
        return GameContext.TryGet(out context);
    }
}