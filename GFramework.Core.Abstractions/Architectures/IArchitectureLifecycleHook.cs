// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Enums;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
/// 架构生命周期钩子接口，用于在架构的不同生命周期阶段执行自定义逻辑。
/// 实现此接口的类可以监听架构阶段变化并访问相关的架构实例。
/// </summary>
public interface IArchitectureLifecycleHook
{
    /// <summary>
    /// 当架构进入指定阶段时触发的回调方法。
    /// </summary>
    /// <param name="phase">当前的架构阶段</param>
    /// <param name="architecture">相关的架构实例</param>
    void OnPhase(ArchitecturePhase phase, IArchitecture architecture);
}