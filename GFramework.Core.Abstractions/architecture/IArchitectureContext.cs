using System;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;

namespace GFramework.Core.Abstractions.architecture;

/// <summary>
///     架构上下文接口，提供对系统、模型、工具类的访问以及命令、查询、事件的发送和注册功能
/// </summary>
public interface IArchitectureContext
{
    /// <summary>
    ///     获取日志工厂
    /// </summary>
    ILoggerFactory LoggerFactory { get; }

    /// <summary>
    ///     获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="TSystem">系统类型，必须继承自ISystem接口</typeparam>
    /// <returns>系统实例，如果不存在则返回null</returns>
    TSystem? GetSystem<TSystem>() where TSystem : class, ISystem;

    /// <summary>
    ///     获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="TModel">模型类型，必须继承自IModel接口</typeparam>
    /// <returns>模型实例，如果不存在则返回null</returns>
    TModel? GetModel<TModel>() where TModel : class, IModel;

    /// <summary>
    ///     获取指定类型的工具类实例
    /// </summary>
    /// <typeparam name="TUtility">工具类类型，必须继承自IUtility接口</typeparam>
    /// <returns>工具类实例，如果不存在则返回null</returns>
    TUtility? GetUtility<TUtility>() where TUtility : class, IUtility;

    /// <summary>
    ///     发送一个命令
    /// </summary>
    /// <param name="command">要发送的命令</param>
    void SendCommand(ICommand command);

    /// <summary>
    ///     发送一个带返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    TResult SendCommand<TResult>(ICommand<TResult> command);

    /// <summary>
    ///     发送一个查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="query">要发送的查询</param>
    /// <returns>查询结果</returns>
    TResult SendQuery<TResult>(IQuery<TResult> query);

    /// <summary>
    ///     发送一个事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型，必须具有无参构造函数</typeparam>
    void SendEvent<TEvent>() where TEvent : new();

    /// <summary>
    ///     发送一个带参数的事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="e">事件参数</param>
    void SendEvent<TEvent>(TEvent e) where TEvent : class;

    /// <summary>
    ///     注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <returns>事件注销接口</returns>
    IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler);

    /// <summary>
    ///     取消注册事件监听器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">要取消注册的事件回调方法</param>
    void UnRegisterEvent<TEvent>(Action<TEvent> onEvent);
}