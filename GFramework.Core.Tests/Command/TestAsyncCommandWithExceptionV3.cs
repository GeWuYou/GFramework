using GFramework.Core.Command;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     表示 <see cref="AbstractAsyncCommandTests" /> 使用的异常路径异步测试命令。
/// </summary>
public sealed class TestAsyncCommandWithExceptionV3 : AbstractAsyncCommand<TestCommandInputV2>
{
    /// <summary>
    ///     初始化 <see cref="TestAsyncCommandWithExceptionV3" /> 的新实例。
    /// </summary>
    /// <param name="input">命令输入。</param>
    public TestAsyncCommandWithExceptionV3(TestCommandInputV2 input) : base(input)
    {
    }

    /// <summary>
    ///     执行测试命令并始终抛出预期异常。
    /// </summary>
    /// <param name="input">命令输入。</param>
    /// <returns>此方法不会正常返回。</returns>
    /// <exception cref="InvalidOperationException">始终抛出，用于验证异常传播行为。</exception>
    protected override Task OnExecuteAsync(TestCommandInputV2 input)
    {
        throw new InvalidOperationException("Test exception");
    }
}
