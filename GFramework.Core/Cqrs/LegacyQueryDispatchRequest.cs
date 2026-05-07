// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     包装 legacy 同步查询，使其能够通过自有 CQRS runtime 调度。
/// </summary>
internal sealed class LegacyQueryDispatchRequest(object target, Func<object?> execute)
    : LegacyCqrsDispatchRequestBase(target), IRequest<object?>
{
    private readonly Func<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    /// <summary>
    ///     执行底层 legacy 查询并返回装箱后的结果。
    /// </summary>
    public object? Execute() => _execute();
}
