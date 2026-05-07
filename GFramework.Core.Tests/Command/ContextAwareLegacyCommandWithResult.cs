// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     为 <see cref="CommandExecutorTests" /> 提供可观察上下文注入的带返回值 legacy 命令。
/// </summary>
internal sealed class ContextAwareLegacyCommandWithResult(int result) : ContextAwareBase, ICommand<int>
{
    /// <summary>
    ///     获取执行期间观察到的架构上下文。
    /// </summary>
    public IArchitectureContext? ObservedContext { get; private set; }

    /// <inheritdoc />
    public int Execute()
    {
        ObservedContext = ((GFramework.Core.Abstractions.Rule.IContextAware)this).GetContext();
        return result;
    }
}
