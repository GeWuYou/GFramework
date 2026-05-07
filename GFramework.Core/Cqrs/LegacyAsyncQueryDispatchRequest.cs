// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     包装 legacy 异步查询，使其能够通过自有 CQRS runtime 调度。
/// </summary>
/// <param name="target">需要在 bridge handler 中接收上下文注入的 legacy 查询目标实例。</param>
/// <param name="executeAsync">封装 legacy 异步查询执行逻辑并返回装箱结果的委托。</param>
internal sealed class LegacyAsyncQueryDispatchRequest(object target, Func<Task<object?>> executeAsync)
    : LegacyCqrsDispatchRequestBase(target), IRequest<object?>
{
    private readonly Func<Task<object?>> _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));

    /// <summary>
    ///     异步执行底层 legacy 查询并返回装箱后的结果。
    /// </summary>
    /// <returns>表示异步执行结果的任务；任务结果为底层 legacy 查询返回的装箱值。</returns>
    public Task<object?> ExecuteAsync() => _executeAsync();
}
