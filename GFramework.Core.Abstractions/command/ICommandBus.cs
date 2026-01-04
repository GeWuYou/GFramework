namespace GFramework.Core.Abstractions.command;

/// <summary>
///     定义命令总线接口，用于执行各种命令
/// </summary>
public interface ICommandBus
{
    /// <summary>
    ///     发送并执行一个命令
    /// </summary>
    /// <param name="command">要执行的命令对象</param>
    public void Send(ICommand command);

    /// <summary>
    ///     发送并执行一个带返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果的类型</typeparam>
    /// <param name="command">要执行的带返回值的命令对象</param>
    /// <returns>命令执行的结果</returns>
    public TResult Send<TResult>(ICommand<TResult> command);
}