using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.cqrs.command;
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