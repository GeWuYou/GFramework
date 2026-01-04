using GFramework.Core.Abstractions.command;
using GFramework.Core.rule;

namespace GFramework.Core.command;

/// <summary>
///     抽象命令类，实现 ICommand 接口，为具体命令提供基础架构支持
/// </summary>
/// <typeparam name="TInput">命令输入参数类型，必须实现 ICommandInput 接口</typeparam>
/// <param name="input">命令执行所需的输入参数</param>
public abstract class AbstractCommand<TInput>(TInput input) : ContextAwareBase, ICommand
    where TInput : ICommandInput
{
    /// <summary>
    ///     执行命令的入口方法，实现 ICommand 接口的 Execute 方法
    /// </summary>
    void ICommand.Execute()
    {
        OnExecute(input);
    }

    /// <summary>
    ///     命令执行的抽象方法，由派生类实现具体的命令逻辑
    /// </summary>
    /// <param name="input">命令执行所需的输入参数</param>
    protected abstract void OnExecute(TInput input);
}

/// <summary>
///     带返回值的抽象命令类，实现 ICommand{TResult} 接口，为需要返回结果的命令提供基础架构支持
/// </summary>
/// <typeparam name="TInput">命令输入参数类型，必须实现 ICommandInput 接口</typeparam>
/// <typeparam name="TResult">命令执行后返回的结果类型</typeparam>
/// <param name="input">命令执行所需的输入参数</param>
public abstract class AbstractCommand<TInput, TResult>(TInput input) : ContextAwareBase, ICommand<TResult>
    where TInput : ICommandInput
{
    /// <summary>
    ///     执行命令的入口方法，实现 ICommand{TResult} 接口的 Execute 方法
    /// </summary>
    /// <returns>命令执行后的结果</returns>
    TResult ICommand<TResult>.Execute()
    {
        return OnExecute(input);
    }

    /// <summary>
    ///     命令执行的抽象方法，由派生类实现具体的命令逻辑
    /// </summary>
    /// <param name="input">命令执行所需的输入参数</param>
    /// <returns>命令执行后的结果</returns>
    protected abstract TResult OnExecute(TInput input);
}