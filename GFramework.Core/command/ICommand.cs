using GFramework.Core.events;
using GFramework.Core.model;
using GFramework.Core.query;
using GFramework.Core.rule;
using GFramework.Core.system;
using GFramework.Core.utility;

namespace GFramework.Core.command;

/// <summary>
///     命令接口，定义了无返回值命令的基本契约
///     该接口继承了多个框架能力接口，使命令可以访问架构、系统、模型、工具，并能够发送事件、命令和查询
/// </summary>
public interface ICommand : ICanSetArchitecture, ICanGetSystem, ICanGetModel, ICanGetUtility,
    ICanSendEvent, ICanSendCommand, ICanSendQuery
{
    /// <summary>
    ///     执行命令的核心方法
    ///     该方法不接受参数且无返回值，具体实现由派生类完成
    /// </summary>
    void Execute();
}

/// <summary>
///     带返回值的命令接口，定义了有返回值命令的基本契约
///     该接口继承了多个框架能力接口，使命令可以访问架构、系统、模型、工具，并能够发送事件、命令和查询
/// </summary>
/// <typeparam name="TResult">命令执行后返回的结果类型</typeparam>
public interface ICommand<out TResult> : ICanSetArchitecture, ICanGetSystem, ICanGetModel,
    ICanGetUtility,
    ICanSendEvent, ICanSendCommand, ICanSendQuery
{
    /// <summary>
    ///     执行命令的核心方法
    ///     该方法不接受参数，但会返回指定类型的结果
    /// </summary>
    /// <returns>命令执行后的结果，类型为 TResult</returns>
    TResult Execute();
}