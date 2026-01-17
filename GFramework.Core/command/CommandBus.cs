using GFramework.Core.Abstractions.command;
using IAsyncCommand = GFramework.Core.Abstractions.command.IAsyncCommand;

namespace GFramework.Core.command;

/// <summary>
///     命令总线实现类，用于发送和执行命令
/// </summary>
public sealed class CommandBus : ICommandBus
{
    /// <summary>
    ///     发送并执行无返回值的命令
    /// </summary>
    /// <param name="command">要执行的命令对象，不能为空</param>
    /// <exception cref="ArgumentNullException">当command参数为null时抛出</exception>
    public void Send(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        command.Execute();
    }

    /// <summary>
    ///     发送并执行有返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果的类型</typeparam>
    /// <param name="command">要执行的命令对象，不能为空</param>
    /// <returns>命令执行的结果</returns>
    /// <exception cref="ArgumentNullException">当command参数为null时抛出</exception>
    public TResult Send<TResult>(ICommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.Execute();
    }

    /// <summary>
    ///     发送并异步执行无返回值的命令
    /// </summary>
    /// <param name="command">要执行的命令对象，不能为空</param>
    /// <exception cref="ArgumentNullException">当command参数为null时抛出</exception>
    public async Task SendAsync(IAsyncCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        await command.ExecuteAsync();
    }

    /// <summary>
    ///     发送并异步执行有返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果的类型</typeparam>
    /// <param name="command">要执行的命令对象，不能为空</param>
    /// <returns>命令执行的结果</returns>
    /// <exception cref="ArgumentNullException">当command参数为null时抛出</exception>
    public async Task<TResult> SendAsync<TResult>(IAsyncCommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await command.ExecuteAsync();
    }
}