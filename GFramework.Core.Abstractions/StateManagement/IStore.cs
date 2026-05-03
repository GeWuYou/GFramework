// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     可写状态容器接口，提供统一的状态分发入口。
///     所有状态变更都应通过分发 action 触发，以保持单向数据流和可测试性。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public interface IStore<out TState> : IReadonlyStore<TState>
{
    /// <summary>
    ///     获取当前是否可以撤销到更早的历史状态。
    ///     当未启用历史缓冲区，或当前已经位于最早历史点时，返回 <see langword="false"/>。
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    ///     获取当前是否可以重做到更晚的历史状态。
    ///     当未启用历史缓冲区，或当前已经位于最新历史点时，返回 <see langword="false"/>。
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    ///     分发一个 action 以触发状态演进。
    ///     Store 会按注册顺序执行与该 action 类型匹配的 reducer，并在状态变化后通知订阅者。
    /// </summary>
    /// <typeparam name="TAction">action 的具体类型。</typeparam>
    /// <param name="action">要分发的 action 实例。</param>
    void Dispatch<TAction>(TAction action);

    /// <summary>
    ///     将多个状态操作合并到一个批处理中执行。
    ///     批处理内部的每次分发仍会立即更新 Store 状态和历史，但订阅通知会延迟到最外层批处理结束后再统一触发一次。
    /// </summary>
    /// <param name="batchAction">批处理主体；调用方应在其中执行若干次 <see cref="Dispatch{TAction}(TAction)"/>、<see cref="Undo"/> 或 <see cref="Redo"/>。</param>
    void RunInBatch(Action batchAction);

    /// <summary>
    ///     将当前状态回退到上一个历史点。
    /// </summary>
    /// <exception cref="InvalidOperationException">当历史缓冲区未启用，或当前已经没有可撤销的历史点时抛出。</exception>
    void Undo();

    /// <summary>
    ///     将当前状态前进到下一个历史点。
    /// </summary>
    /// <exception cref="InvalidOperationException">当历史缓冲区未启用，或当前已经没有可重做的历史点时抛出。</exception>
    void Redo();

    /// <summary>
    ///     跳转到指定索引的历史点。
    ///     该能力适合调试面板或开发工具实现时间旅行查看。
    /// </summary>
    /// <param name="historyIndex">目标历史索引，从 0 开始。</param>
    /// <exception cref="InvalidOperationException">当历史缓冲区未启用时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="historyIndex"/> 超出当前历史范围时抛出。</exception>
    void TimeTravelTo(int historyIndex);

    /// <summary>
    ///     清空当前撤销/重做历史，并以当前状态作为新的历史锚点。
    ///     该操作不会修改当前状态，也不会触发额外通知。
    /// </summary>
    void ClearHistory();
}