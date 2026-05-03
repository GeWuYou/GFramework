// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.State;

namespace GFramework.Core.Tests.State;

/// <summary>
///     为 <see cref="StateMachineTests" /> 提供异步生命周期路径的测试状态。
/// </summary>
public sealed class TestAsyncState : IState, IAsyncState
{
    /// <summary>
    ///     获取或设置是否允许向目标状态转移。
    /// </summary>
    public bool AllowTransitions { get; set; } = true;

    /// <summary>
    ///     获取异步进入状态是否已被调用。
    /// </summary>
    public bool EnterCalled { get; private set; }

    /// <summary>
    ///     获取异步离开状态是否已被调用。
    /// </summary>
    public bool ExitCalled { get; private set; }

    /// <summary>
    ///     获取异步进入回调被调用的次数。
    /// </summary>
    public int EnterCallCount { get; private set; }

    /// <summary>
    ///     获取异步离开回调被调用的次数。
    /// </summary>
    public int ExitCallCount { get; private set; }

    /// <summary>
    ///     获取最近一次异步进入时的来源状态。
    /// </summary>
    public IState? EnterFrom { get; private set; }

    /// <summary>
    ///     获取最近一次异步离开时的目标状态。
    /// </summary>
    public IState? ExitTo { get; private set; }

    /// <summary>
    ///     获取异步转移检查被调用的次数。
    /// </summary>
    public int CanTransitionToCallCount { get; private set; }

    /// <summary>
    ///     异步记录进入状态的来源状态与调用次数。
    /// </summary>
    /// <param name="from">触发进入的来源状态。</param>
    public async Task OnEnterAsync(IState? from)
    {
        await Task.Delay(1).ConfigureAwait(false);
        EnterCalled = true;
        EnterCallCount++;
        EnterFrom = from;
    }

    /// <summary>
    ///     异步记录离开状态的目标状态与调用次数。
    /// </summary>
    /// <param name="to">即将切换到的目标状态。</param>
    public async Task OnExitAsync(IState? to)
    {
        await Task.Delay(1).ConfigureAwait(false);
        ExitCalled = true;
        ExitCallCount++;
        ExitTo = to;
    }

    /// <summary>
    ///     异步记录转移检查并返回当前是否允许切换。
    /// </summary>
    /// <param name="target">目标状态。</param>
    /// <returns>允许切换时返回 <see langword="true" />，否则返回 <see langword="false" />。</returns>
    public async Task<bool> CanTransitionToAsync(IState target)
    {
        await Task.Delay(1).ConfigureAwait(false);
        CanTransitionToCallCount++;
        return AllowTransitions;
    }

    /// <summary>
    ///     同步进入入口不应被异步状态机路径调用。
    /// </summary>
    /// <param name="from">触发进入的来源状态。</param>
    /// <exception cref="InvalidOperationException">总是抛出，表示当前测试状态只允许异步入口。</exception>
    public void OnEnter(IState? from)
    {
        throw new InvalidOperationException("Sync OnEnter should not be called for async state");
    }

    /// <summary>
    ///     同步离开入口不应被异步状态机路径调用。
    /// </summary>
    /// <param name="to">即将切换到的目标状态。</param>
    /// <exception cref="InvalidOperationException">总是抛出，表示当前测试状态只允许异步入口。</exception>
    public void OnExit(IState? to)
    {
        throw new InvalidOperationException("Sync OnExit should not be called for async state");
    }

    /// <summary>
    ///     同步转移检查入口不应被异步状态机路径调用。
    /// </summary>
    /// <param name="target">目标状态。</param>
    /// <returns>此方法不会正常返回。</returns>
    /// <exception cref="InvalidOperationException">总是抛出，表示当前测试状态只允许异步入口。</exception>
    public bool CanTransitionTo(IState target)
    {
        throw new InvalidOperationException("Sync CanTransitionTo should not be called for async state");
    }
}
