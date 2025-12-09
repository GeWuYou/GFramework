

using GFramework.framework.command;
using GFramework.framework.events;
using GFramework.framework.model;
using GFramework.framework.query;
using GFramework.framework.system;
using GFramework.framework.utility;

namespace GFramework.framework.architecture;

/// <summary>
/// 架构接口，定义了应用程序架构的核心功能，包括系统、模型、工具的注册和获取，
/// 以及命令、查询、事件的发送和处理机制
/// </summary>
public interface IArchitecture
{
    /// <summary>
    /// 注册系统实例到架构中
    /// </summary>
    /// <typeparam name="T">系统类型，必须实现ISystem接口</typeparam>
    /// <param name="system">要注册的系统实例</param>
    void RegisterSystem<T>(T system) where T : ISystem;

    /// <summary>
    /// 注册模型实例到架构中
    /// </summary>
    /// <typeparam name="T">模型类型，必须实现IModel接口</typeparam>
    /// <param name="model">要注册的模型实例</param>
    void RegisterModel<T>(T model) where T : IModel;

    /// <summary>
    /// 注册工具实例到架构中
    /// </summary>
    /// <typeparam name="T">工具类型，必须实现IUtility接口</typeparam>
    /// <param name="utility">要注册的工具实例</param>
    void RegisterUtility<T>(T utility) where T : IUtility;

    /// <summary>
    /// 从架构中获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="T">系统类型，必须是class且实现ISystem接口</typeparam>
    /// <returns>指定类型的系统实例</returns>
    T GetSystem<T>() where T : class, ISystem;

    /// <summary>
    /// 从架构中获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="T">模型类型，必须是class且实现IModel接口</typeparam>
    /// <returns>指定类型的模型实例</returns>
    T GetModel<T>() where T : class, IModel;

    /// <summary>
    /// 从架构中获取指定类型的工具实例
    /// </summary>
    /// <typeparam name="T">工具类型，必须是class且实现IUtility接口</typeparam>
    /// <returns>指定类型的工具实例</returns>
    T GetUtility<T>() where T : class, IUtility;

    /// <summary>
    /// 发送并执行指定的命令
    /// </summary>
    /// <typeparam name="T">命令类型，必须实现ICommand接口</typeparam>
    /// <param name="command">要执行的命令实例</param>
    void SendCommand<T>(T command) where T : ICommand;

    /// <summary>
    /// 发送并执行带有返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果的类型</typeparam>
    /// <param name="command">要执行的命令实例</param>
    /// <returns>命令执行的结果</returns>
    TResult SendCommand<TResult>(ICommand<TResult> command);

    /// <summary>
    /// 发送并执行查询操作
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型</typeparam>
    /// <param name="query">要执行的查询实例</param>
    /// <returns>查询的结果</returns>
    TResult SendQuery<TResult>(IQuery<TResult> query);

    /// <summary>
    /// 发送无参事件
    /// </summary>
    /// <typeparam name="T">事件类型，必须具有无参构造函数</typeparam>
    void SendEvent<T>() where T : new();

    /// <summary>
    /// 发送指定的事件实例
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="e">要发送的事件实例</param>
    void SendEvent<T>(T e);

    /// <summary>
    /// 注册事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件触发时的回调方法</param>
    /// <returns>用于取消注册的句柄</returns>
    IUnRegister RegisterEvent<T>(Action<T> onEvent);

    /// <summary>
    /// 取消注册事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">要取消注册的事件回调方法</param>
    void UnRegisterEvent<T>(Action<T> onEvent);
}
