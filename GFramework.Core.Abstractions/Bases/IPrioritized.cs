// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Bases;

/// <summary>
///     定义具有优先级的对象接口。
///     数值越小优先级越高，越先执行。
///     用于控制服务、系统等组件的执行顺序。
/// </summary>
public interface IPrioritized
{
    /// <summary>
    ///     获取优先级值。
    ///     数值越小优先级越高。
    ///     默认优先级为 0。
    ///     建议范围：-1000 到 1000。
    /// </summary>
    int Priority { get; }
}