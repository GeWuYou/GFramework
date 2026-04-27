using GFramework.Core.Command;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     表示 <see cref="AbstractAsyncCommandTests" /> 使用的子类化无返回值异步测试命令。
/// </summary>
public sealed class TestAsyncCommandChildV3 : AbstractAsyncCommand<TestCommandInputV2>
{
    /// <summary>
    ///     初始化 <see cref="TestAsyncCommandChildV3" /> 的新实例。
    /// </summary>
    /// <param name="input">命令输入。</param>
    public TestAsyncCommandChildV3(TestCommandInputV2 input) : base(input)
    {
    }

    /// <summary>
    ///     获取一个值，该值指示命令是否已经执行。
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     获取子类记录的执行值。
    /// </summary>
    public int ExecutedValue { get; private set; }

    /// <summary>
    ///     执行子类测试命令并记录经过变换的输入值。
    /// </summary>
    /// <param name="input">命令输入。</param>
    /// <returns>已完成的异步任务。</returns>
    protected override Task OnExecuteAsync(TestCommandInputV2 input)
    {
        Executed = true;
        ExecutedValue = input.Value * 2;
        return Task.CompletedTask;
    }
}
