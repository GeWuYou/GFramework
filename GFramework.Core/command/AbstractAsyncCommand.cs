using GFramework.Core.Abstractions.command;
using GFramework.Core.rule;

namespace GFramework.Core.command;

public abstract class AbstractAsyncCommand<TInput>(TInput input) : ContextAwareBase, IAsyncCommand
    where TInput : ICommandInput
{
    async Task IAsyncCommand.ExecuteAsync()
    {
        await OnExecuteAsync(input);
    }

    protected abstract Task OnExecuteAsync(TInput input);
}

public abstract class AbstractAsyncCommand<TInput, TResult>(TInput input) : ContextAwareBase, IAsyncCommand<TResult>
    where TInput : ICommandInput
{
    async Task<TResult> IAsyncCommand<TResult>.ExecuteAsync()
    {
        return await OnExecuteAsync(input);
    }

    protected abstract Task<TResult> OnExecuteAsync(TInput input);
}