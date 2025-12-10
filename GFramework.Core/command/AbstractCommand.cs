using GFramework.Core.architecture;
using GFramework.Core.rule;

namespace GFramework.Core.command;

/// <summary>
/// 抽象命令类，实现 ICommand 接口，为具体命令提供基础架构支持
/// </summary>
public abstract class AbstractCommand : ICommand
{
    private IArchitecture _mArchitecture;

    /// <summary>
    /// 获取命令所属的架构实例
    /// </summary>
    /// <returns>IArchitecture 架构接口实例</returns>
    IArchitecture IBelongToArchitecture.GetArchitecture() => _mArchitecture;

    /// <summary>
    /// 设置命令所属的架构实例
    /// </summary>
    /// <param name="architecture">要设置的架构实例</param>
    void ICanSetArchitecture.SetArchitecture(IArchitecture architecture) => _mArchitecture = architecture;

    /// <summary>
    /// 执行命令，调用抽象方法 OnExecute 来实现具体逻辑
    /// </summary>
    void ICommand.Execute() => OnExecute();

    /// <summary>
    /// 抽象方法，由子类实现具体的命令执行逻辑
    /// </summary>
    protected abstract void OnExecute();
}

/// <summary>
/// 带返回值的抽象命令类，实现 ICommand{TResult} 接口，为需要返回结果的命令提供基础架构支持
/// </summary>
/// <typeparam name="TResult">命令执行后返回的结果类型</typeparam>
public abstract class AbstractCommand<TResult> : ICommand<TResult>
{
    private IArchitecture _mArchitecture;

    /// <summary>
    /// 获取命令所属的架构实例
    /// </summary>
    /// <returns>IArchitecture 架构接口实例</returns>
    IArchitecture IBelongToArchitecture.GetArchitecture() => _mArchitecture;

    /// <summary>
    /// 设置命令所属的架构实例
    /// </summary>
    /// <param name="architecture">要设置的架构实例</param>
    void ICanSetArchitecture.SetArchitecture(IArchitecture architecture) => _mArchitecture = architecture;

    /// <summary>
    /// 执行命令，调用抽象方法 OnExecute 来实现具体逻辑并返回结果
    /// </summary>
    /// <returns>TResult 类型的执行结果</returns>
    TResult ICommand<TResult>.Execute() => OnExecute();

    /// <summary>
    /// 抽象方法，由子类实现具体的命令执行逻辑
    /// </summary>
    /// <returns>TResult 类型的执行结果</returns>
    protected abstract TResult OnExecute();
}
