using GFramework.Core.Abstractions.command;
using GFramework.Core.rule;

namespace GFramework.Core.command;

/// <summary>
/// 异步命令的抽象基类，实现了IAsyncCommand接口
/// 提供异步命令执行的基础框架和上下文感知功能
/// </summary>
public abstract class AbstractAsyncCommand : ContextAwareBase, IAsyncCommand
{
    /// <summary>
    /// 执行异步命令的实现方法
    /// 该方法通过调用受保护的抽象方法OnExecuteAsync来执行具体的命令逻辑
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    async Task IAsyncCommand.ExecuteAsync()
    {
        await OnExecuteAsync();
    }

    /// <summary>
    /// 子类必须实现的异步执行方法
    /// 包含具体的命令执行逻辑
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    protected abstract Task OnExecuteAsync();
}
