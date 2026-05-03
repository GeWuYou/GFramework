// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Command;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     表示 <see cref="AbstractAsyncCommandTests" /> 使用的子类化带返回值异步测试命令。
/// </summary>
public sealed class TestAsyncCommandWithResultChildV3 : AbstractAsyncCommand<TestCommandInputV2, int>
{
    /// <summary>
    ///     初始化 <see cref="TestAsyncCommandWithResultChildV3" /> 的新实例。
    /// </summary>
    /// <param name="input">命令输入。</param>
    public TestAsyncCommandWithResultChildV3(TestCommandInputV2 input) : base(input)
    {
    }

    /// <summary>
    ///     获取一个值，该值指示命令是否已经执行。
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     执行子类测试命令并返回经过变换的输入值。
    /// </summary>
    /// <param name="input">命令输入。</param>
    /// <returns>输入值三倍的异步结果。</returns>
    protected override Task<int> OnExecuteAsync(TestCommandInputV2 input)
    {
        Executed = true;
        return Task.FromResult(input.Value * 3);
    }
}
