// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     为 <see cref="CommandExecutorTests" /> 提供可观察上下文注入的 legacy 命令。
/// </summary>
internal sealed class ContextAwareLegacyCommand : ContextAwareBase, ICommand
{
    /// <summary>
    ///     获取执行期间观察到的架构上下文。
    /// </summary>
    public IArchitectureContext? ObservedContext { get; private set; }

    /// <summary>
    ///     获取命令是否已经执行。
    /// </summary>
    public bool Executed { get; private set; }

    /// <inheritdoc />
    public void Execute()
    {
        Executed = true;
        ObservedContext = ((GFramework.Core.Abstractions.Rule.IContextAware)this).GetContext();
    }
}
