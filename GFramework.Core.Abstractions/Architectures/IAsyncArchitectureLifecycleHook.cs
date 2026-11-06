// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Enums;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
///     支持可取消异步阶段工作的生命周期钩子。异步启动只调用异步入口，避免重复执行同步回调。
/// </summary>
public interface IAsyncArchitectureLifecycleHook : IArchitectureLifecycleHook
{
    /// <summary>等待当前阶段的异步工作；取消时必须先结束其拥有的操作再返回。</summary>
    /// <param name="phase">目标阶段。</param>
    /// <param name="architecture">当前架构。</param>
    /// <param name="cancellationToken">启动取消令牌；销毁阶段不使用已取消的启动令牌。</param>
    /// <returns>阶段工作完成的值任务。</returns>
    ValueTask OnPhaseAsync(
        ArchitecturePhase phase,
        IArchitecture architecture,
        CancellationToken cancellationToken = default);
}
