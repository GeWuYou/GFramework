// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs.Command;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     表示 <see cref="AbstractAsyncCommandTests" /> 使用的测试命令输入。
/// </summary>
public sealed class TestCommandInputV2 : ICommandInput
{
    /// <summary>
    ///     获取或设置用于驱动测试断言的输入值。
    /// </summary>
    public int Value { get; init; }
}
