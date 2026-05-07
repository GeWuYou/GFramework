// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     用于验证 legacy 命令桥接时会把当前 <see cref="IArchitectureContext" /> 注入到命令对象。
/// </summary>
public sealed class LegacyArchitectureBridgeCommand : ContextAwareBase, ICommand
{
    /// <summary>
    ///     获取执行期间观察到的上下文实例。
    /// </summary>
    public IArchitectureContext? ObservedContext { get; private set; }

    /// <summary>
    ///     获取当前命令是否已经执行。
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     执行命令并记录 bridge handler 注入的上下文。
    /// </summary>
    public void Execute()
    {
        Executed = true;
        ObservedContext = ((GFramework.Core.Abstractions.Rule.IContextAware)this).GetContext();
    }
}
