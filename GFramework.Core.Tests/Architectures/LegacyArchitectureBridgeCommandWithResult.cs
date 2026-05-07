// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     用于验证 legacy 带返回值命令桥接时会沿用统一 runtime。
/// </summary>
public sealed class LegacyArchitectureBridgeCommandWithResult : ContextAwareBase, ICommand<int>
{
    /// <summary>
    ///     获取执行期间观察到的上下文实例。
    /// </summary>
    public IArchitectureContext? ObservedContext { get; private set; }

    /// <summary>
    ///     执行命令并返回测试结果。
    /// </summary>
    public int Execute()
    {
        ObservedContext = ((GFramework.Core.Abstractions.Rule.IContextAware)this).GetContext();
        return 42;
    }
}
