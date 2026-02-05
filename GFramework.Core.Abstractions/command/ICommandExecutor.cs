namespace GFramework.Core.Abstractions.command;

/// <summary>
/// 定义命令执行器接口，提供同步和异步方式发送并执行命令的方法。
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// 发送并执行一个命令。
    /// </summary>
    /// <param name="command">要执行的命令对象，实现 ICommand 接口。</param>
    public void Send(ICommand command);

    /// <summary>
    /// 发送并执行一个带返回值的命令。
    /// </summary>
    /// <typeparam name="TResult">命令执行结果的类型。</typeparam>
    /// <param name="command">要执行的带返回值的命令对象，实现 ICommand&lt;TResult&gt; 接口。</param>
    /// <returns>命令执行的结果，类型为 TResult。</returns>
    public TResult Send<TResult>(ICommand<TResult> command);

    /// <summary>
    /// 发送并异步执行一个命令。
    /// </summary>
    /// <param name="command">要执行的命令对象，实现 IAsyncCommand 接口。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task SendAsync(IAsyncCommand command);

    /// <summary>
    /// 发送并异步执行一个带返回值的命令。
    /// </summary>
    /// <typeparam name="TResult">命令执行结果的类型。</typeparam>
    /// <param name="command">要执行的带返回值的命令对象，实现 IAsyncCommand&lt;TResult&gt; 接口。</param>
    /// <returns>表示异步操作的任务，其结果为命令执行的结果，类型为 TResult。</returns>
    Task<TResult> SendAsync<TResult>(IAsyncCommand<TResult> command);
}