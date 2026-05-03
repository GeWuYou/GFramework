// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Enums;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
/// 架构阶段监听器接口，用于监听和响应架构生命周期中的不同阶段变化。
/// 实现此接口的类可以在架构进入特定阶段时执行相应的逻辑处理。
/// </summary>
public interface IArchitecturePhaseListener
{
    /// <summary>
    /// 当架构进入指定阶段时触发的回调方法。
    /// </summary>
    /// <param name="phase">架构阶段枚举值，表示当前所处的架构阶段</param>
    void OnArchitecturePhase(ArchitecturePhase phase);
}