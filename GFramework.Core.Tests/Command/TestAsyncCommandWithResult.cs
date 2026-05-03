// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Command;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     表示 <see cref="CommandExecutorTests" /> 使用的带返回值异步测试命令。
/// </summary>
public sealed class TestAsyncCommandWithResult : AbstractAsyncCommand<TestCommandInput, int>
{
    /// <summary>
    ///     初始化 <see cref="TestAsyncCommandWithResult" /> 的新实例。
    /// </summary>
    /// <param name="input">命令输入。</param>
    public TestAsyncCommandWithResult(TestCommandInput input) : base(input)
    {
    }

    /// <summary>
    ///     获取一个值，该值指示命令是否已经执行。
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     执行异步测试命令并返回基于输入值计算的结果。
    /// </summary>
    /// <param name="input">命令输入。</param>
    /// <returns>输入值两倍的异步结果。</returns>
    protected override Task<int> OnExecuteAsync(TestCommandInput input)
    {
        Executed = true;
        return Task.FromResult(input.Value * 2);
    }
}
