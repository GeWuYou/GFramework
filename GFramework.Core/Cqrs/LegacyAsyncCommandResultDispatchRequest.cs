// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     包装 legacy 异步带返回值命令，使其能够通过自有 CQRS runtime 调度。
/// </summary>
internal sealed class LegacyAsyncCommandResultDispatchRequest(object target, Func<Task<object?>> executeAsync)
    : LegacyCqrsDispatchRequestBase(target), IRequest<object?>
{
    private readonly Func<Task<object?>> _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));

    /// <summary>
    ///     异步执行底层 legacy 命令并返回装箱后的结果。
    /// </summary>
    public Task<object?> ExecuteAsync() => _executeAsync();
}
