using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.properties;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;

namespace GFramework.Core.architecture;

/// <summary>
///     架构上下文类，提供对系统、模型、工具等组件的访问以及命令、查询、事件的执行管理
/// </summary>
public class ArchitectureContext(
    IIocContainer container,
    ITypeEventSystem typeEventSystem,
    LoggerProperties loggerProperties)
    : IArchitectureContext
{
    private readonly IIocContainer _container = container ?? throw new ArgumentNullException(nameof(container));

    private readonly ITypeEventSystem _typeEventSystem =
        typeEventSystem ?? throw new ArgumentNullException(nameof(typeEventSystem));

    internal IArchitectureRuntime Runtime { get; set; } = null!;

    #region Query Execution

    /// <summary>
    ///     发送一个查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="query">要发送的查询</param>
    /// <returns>查询结果</returns>
    public TResult SendQuery<TResult>(IQuery<TResult> query)
    {
        return query == null ? throw new ArgumentNullException(nameof(query)) : Runtime.SendQuery(query);
    }

    #endregion

    #region Component Retrieval

    /// <summary>
    ///     从IOC容器中获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <returns>对应的系统实例</returns>
    public TSystem? GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return _container.Get<TSystem>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <returns>对应的模型实例</returns>
    public TModel? GetModel<TModel>() where TModel : class, IModel
    {
        return _container.Get<TModel>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的工具实例
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <returns>对应的工具实例</returns>
    public TUtility? GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return _container.Get<TUtility>();
    }

    #endregion

    #region Command Execution

    /// <summary>
    ///     发送一个无返回结果的命令
    /// </summary>
    /// <param name="command">要发送的命令</param>
    public void SendCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Runtime.SendCommand(command);
    }

    /// <summary>
    ///     发送一个带返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    public TResult SendCommand<TResult>(ICommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Runtime.SendCommand(command);
    }

    #endregion

    #region Event Management

    /// <summary>
    ///     发送一个默认构造的新事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public void SendEvent<TEvent>() where TEvent : new()
    {
        _typeEventSystem.Send<TEvent>();
    }

    /// <summary>
    ///     发送一个具体的事件实例
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="e">事件参数</param>
    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
        if (e == null) throw new ArgumentNullException(nameof(e));
        _typeEventSystem.Send(e);
    }

    /// <summary>
    ///     注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <returns>事件注销接口</returns>
    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        return handler == null ? throw new ArgumentNullException(nameof(handler)) : _typeEventSystem.Register(handler);
    }

    /// <summary>
    ///     取消对某类型事件的监听
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">之前绑定的事件处理器</param>
    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        ArgumentNullException.ThrowIfNull(onEvent);
        _typeEventSystem.UnRegister(onEvent);
    }

    #endregion
}