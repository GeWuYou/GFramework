// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     用于验证 legacy 查询桥接时会把当前 <see cref="IArchitectureContext" /> 注入到查询对象。
/// </summary>
public sealed class LegacyArchitectureBridgeQuery : ContextAwareBase, IQuery<int>
{
    /// <summary>
    ///     获取执行期间观察到的上下文实例。
    /// </summary>
    public IArchitectureContext? ObservedContext { get; private set; }

    /// <summary>
    ///     执行查询并返回测试结果。
    /// </summary>
    public int Do()
    {
        ObservedContext = ((GFramework.Core.Abstractions.Rule.IContextAware)this).GetContext();
        return 24;
    }
}
