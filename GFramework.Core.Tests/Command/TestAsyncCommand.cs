using GFramework.Core.Command;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     表示 <see cref="CommandExecutorTests" /> 使用的异步测试命令。
/// </summary>
public sealed class TestAsyncCommand : AbstractAsyncCommand<TestCommandInput>
{
    /// <summary>
    ///     初始化 <see cref="TestAsyncCommand" /> 的新实例。
    /// </summary>
    /// <param name="input">命令输入。</param>
    public TestAsyncCommand(TestCommandInput input) : base(input)
    {
    }

    /// <summary>
    ///     获取一个值，该值指示命令是否已经执行。
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     获取命令记录的执行结果值。
    /// </summary>
    public int ExecutedValue { get; private set; }

    /// <summary>
    ///     执行异步测试命令并记录执行状态。
    /// </summary>
    /// <param name="input">命令输入。</param>
    /// <returns>已完成的异步任务。</returns>
    protected override Task OnExecuteAsync(TestCommandInput input)
    {
        Executed = true;
        ExecutedValue = 42;
        return Task.CompletedTask;
    }
}
