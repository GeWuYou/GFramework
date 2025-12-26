using GFramework.Core.command;
using GFramework.Core.events;
using GFramework.Core.query;

namespace GFramework.Core.architecture;

/// <summary>
///     架构运行时接口，提供统一的命令、查询、事件操作入口
///     负责委托 ArchitectureContext 的能力执行具体操作
/// </summary>
public interface IArchitectureRuntime
{
    /// <summary>
    ///     发送并执行指定的命令
    /// </summary>
    /// <typeparam name="T">命令类型，必须实现ICommand接口</typeparam>
    /// <param name="command">要执行的命令实例</param>
    void SendCommand<T>(T command) where T : ICommand;

    /// <summary>
    ///     发送并执行带有返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果的类型</typeparam>
    /// <param name="command">要执行的命令实例</param>
    /// <returns>命令执行的结果</returns>
    TResult SendCommand<TResult>(ICommand<TResult> command);

    /// <summary>
    ///     发送并执行查询操作
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型</typeparam>
    /// <param name="query">要执行的查询实例</param>
    /// <returns>查询的结果</returns>
    TResult SendQuery<TResult>(IQuery<TResult> query);

    /// <summary>
    ///     发送无参事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型，必须具有无参构造函数</typeparam>
    void SendEvent<TEvent>() where TEvent : new();

    /// <summary>
    ///     发送指定的事件实例
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="e">要发送的事件实例</param>
    void SendEvent<TEvent>(TEvent e) where TEvent : class;

    /// <summary>
    ///     注册事件监听器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">事件触发时的回调方法</param>
    /// <returns>用于取消注册的句柄</returns>
    IUnRegister RegisterEvent<TEvent>(Action<TEvent> onEvent);

    /// <summary>
    ///     取消注册事件监听器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">要取消注册的事件回调方法</param>
    void UnRegisterEvent<TEvent>(Action<TEvent> onEvent);
}