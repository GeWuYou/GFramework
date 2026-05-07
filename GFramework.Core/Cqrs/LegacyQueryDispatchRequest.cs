// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     包装 legacy 同步查询，使其能够通过自有 CQRS runtime 调度。
/// </summary>
/// <param name="target">需要在 bridge handler 中接收上下文注入的 legacy 查询目标实例。</param>
/// <param name="execute">封装 legacy 查询执行逻辑并返回装箱结果的委托。</param>
internal sealed class LegacyQueryDispatchRequest(object target, Func<object?> execute)
    : LegacyCqrsDispatchRequestBase(target), IRequest<object?>
{
    private readonly Func<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    /// <summary>
    ///     执行底层 legacy 查询并返回装箱后的结果。
    /// </summary>
    /// <returns>底层 legacy 查询执行后的装箱结果；若查询无返回值则为 <see langword="null" />。</returns>
    public object? Execute() => _execute();
}
