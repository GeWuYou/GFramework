// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     用于验证 legacy 异步查询桥接时也会显式注入当前架构上下文。
/// </summary>
public sealed class LegacyArchitectureBridgeAsyncQuery : ContextAwareBase, IAsyncQuery<int>
{
    /// <summary>
    ///     获取执行期间观察到的上下文实例。
    /// </summary>
    public IArchitectureContext? ObservedContext { get; private set; }

    /// <summary>
    ///     执行异步查询并返回测试结果。
    /// </summary>
    public Task<int> DoAsync()
    {
        ObservedContext = ((GFramework.Core.Abstractions.Rule.IContextAware)this).GetContext();
        return Task.FromResult(64);
    }
}
