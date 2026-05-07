// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="AsyncQueryExecutorTests" /> 提供可观察上下文注入的 legacy 异步查询。
/// </summary>
internal sealed class ContextAwareLegacyAsyncQuery(int result) : ContextAwareBase, IAsyncQuery<int>
{
    /// <summary>
    ///     获取执行期间观察到的架构上下文。
    /// </summary>
    public IArchitectureContext? ObservedContext { get; private set; }

    /// <inheritdoc />
    public Task<int> DoAsync()
    {
        ObservedContext = ((GFramework.Core.Abstractions.Rule.IContextAware)this).GetContext();
        return Task.FromResult(result);
    }
}
