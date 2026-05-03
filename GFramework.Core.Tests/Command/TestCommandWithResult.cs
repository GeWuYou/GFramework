// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Command;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     表示 <see cref="CommandExecutorTests" /> 使用的带返回值同步测试命令。
/// </summary>
public sealed class TestCommandWithResult : AbstractCommand<TestCommandInput, int>
{
    /// <summary>
    ///     初始化 <see cref="TestCommandWithResult" /> 的新实例。
    /// </summary>
    /// <param name="input">命令输入。</param>
    public TestCommandWithResult(TestCommandInput input) : base(input)
    {
    }

    /// <summary>
    ///     获取一个值，该值指示命令是否已经执行。
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     执行测试命令并返回基于输入值计算的结果。
    /// </summary>
    /// <param name="input">命令输入。</param>
    /// <returns>输入值的两倍。</returns>
    protected override int OnExecute(TestCommandInput input)
    {
        Executed = true;
        return input.Value * 2;
    }
}
