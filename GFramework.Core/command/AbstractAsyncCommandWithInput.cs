using GFramework.Core.Abstractions.command;
using GFramework.Core.rule;

namespace GFramework.Core.command;

/// <summary>
///     抽象异步命令基类，用于处理无返回值的异步命令操作
/// </summary>
/// <typeparam name="TInput">命令输入类型，必须实现ICommandInput接口</typeparam>
public abstract class AbstractAsyncCommand<TInput>(TInput input) : ContextAwareBase, IAsyncCommand
    where TInput : ICommandInput
{
    /// <summary>
    ///     执行异步命令的实现方法
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    async Task IAsyncCommand.ExecuteAsync()
    {
        await OnExecuteAsync(input);
    }

    /// <summary>
    ///     定义异步执行逻辑的抽象方法，由派生类实现具体业务逻辑
    /// </summary>
    /// <param name="input">命令输入参数</param>
    /// <returns>表示异步操作的任务</returns>
    protected abstract Task OnExecuteAsync(TInput input);
}