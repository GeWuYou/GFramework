// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="QueryExecutorTests" /> 提供可观察上下文注入的 legacy 查询。
/// </summary>
internal sealed class ContextAwareLegacyQuery(int result) : ContextAwareBase, IQuery<int>
{
    /// <summary>
    ///     获取执行期间观察到的架构上下文。
    /// </summary>
    public IArchitectureContext? ObservedContext { get; private set; }

    /// <inheritdoc />
    public int Do()
    {
        ObservedContext = ((GFramework.Core.Abstractions.Rule.IContextAware)this).GetContext();
        return result;
    }
}
