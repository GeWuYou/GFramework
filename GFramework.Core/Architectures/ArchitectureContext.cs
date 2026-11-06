// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构上下文类，提供对系统、模型、工具等组件的访问、服务解析与事件管理。
///     请求/通知/流式分发由可选安装的 CQRS runtime 提供，推荐通过
///     <c>GFramework.Cqrs.Extensions</c> 的上下文扩展方法或解析 <c>ICqrsRuntime</c> 服务进入 dispatcher。
/// </summary>
public class ArchitectureContext : IArchitectureContext
{
    private readonly IIocContainer _container;

    /// <summary>
    ///     初始化新的架构上下文，并绑定其依赖容器。
    /// </summary>
    /// <param name="container">
    ///     当前架构使用的 IOC 容器。
    ///     框架服务会通过该容器延迟解析，以避免在上下文构造阶段强制拉起整条运行时链路。
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="container" /> 为 <see langword="null" />。</exception>
    public ArchitectureContext(IIocContainer container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    /// <summary>共享自有标准容器的实例状态使包装上下文遵守同一窗口；自定义容器负责自身访问与释放契约。</summary>
    private void EnsureAvailable()
    {
        if (_container is global::GFramework.Core.Ioc.MicrosoftDiContainer ownedContainer)
            ownedContainer.ContextAccess?.EnsureAvailable();
    }

    /// <summary>
    /// 按正式容器的生命周期获取服务；Transient 不会被上下文额外缓存
    /// </summary>
    /// <typeparam name="TService">服务类型，必须为引用类型</typeparam>
    /// <returns>服务实例，如果不存在则抛出异常</returns>
    public TService GetService<TService>() where TService : class
    {
        return GetOrCache<TService>();
    }

    /// <summary>
    /// 从当前正式容器解析实例，保持标准 DI 的 Singleton、Scoped 和 Transient 语义
    /// </summary>
    /// <typeparam name="TService">服务类型，必须为引用类型</typeparam>
    /// <returns>服务实例，如果不存在则抛出异常</returns>
    private TService GetOrCache<TService>() where TService : class
    {
        EnsureAvailable();
        return _container.Get<TService>()
               ?? throw new InvalidOperationException($"Service {typeof(TService)} not registered");
    }


    #region Component Retrieval

    /// <summary>
    ///     获取指定类型的所有服务实例
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>所有符合条件的服务实例列表</returns>
    public IReadOnlyList<TService> GetServices<TService>() where TService : class
    {
        EnsureAvailable();
        return _container.GetAll<TService>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <returns>对应的系统实例</returns>
    public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return GetService<TSystem>();
    }

    /// <summary>
    ///     获取指定类型的所有系统实例
    /// </summary>
    /// <typeparam name="TSystem">系统类型</typeparam>
    /// <returns>所有符合条件的系统实例列表</returns>
    public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
    {
        EnsureAvailable();
        return _container.GetAll<TSystem>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <returns>对应的模型实例</returns>
    public TModel GetModel<TModel>() where TModel : class, IModel
    {
        return GetService<TModel>();
    }

    /// <summary>
    ///     获取指定类型的所有模型实例
    /// </summary>
    /// <typeparam name="TModel">模型类型</typeparam>
    /// <returns>所有符合条件的模型实例列表</returns>
    public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
    {
        EnsureAvailable();
        return _container.GetAll<TModel>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的工具实例
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <returns>对应的工具实例</returns>
    public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return GetService<TUtility>();
    }

    /// <summary>
    ///     获取指定类型的所有工具实例
    /// </summary>
    /// <typeparam name="TUtility">工具类型</typeparam>
    /// <returns>所有符合条件的工具实例列表</returns>
    public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
    {
        EnsureAvailable();
        return _container.GetAll<TUtility>();
    }

    /// <summary>
    /// 获取指定类型的所有服务实例，并按优先级排序
    /// 实现 IPrioritized 接口的服务将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>按优先级排序后的服务实例列表</returns>
    public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
    {
        EnsureAvailable();
        return _container.GetAllByPriority<TService>();
    }

    /// <summary>
    /// 获取指定类型的所有系统实例，并按优先级排序
    /// 实现 IPrioritized 接口的系统将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TSystem">系统类型</typeparam>
    /// <returns>按优先级排序后的系统实例列表</returns>
    public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
    {
        EnsureAvailable();
        return _container.GetAllByPriority<TSystem>();
    }

    /// <summary>
    /// 获取指定类型的所有模型实例，并按优先级排序
    /// 实现 IPrioritized 接口的模型将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TModel">模型类型</typeparam>
    /// <returns>按优先级排序后的模型实例列表</returns>
    public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
    {
        EnsureAvailable();
        return _container.GetAllByPriority<TModel>();
    }

    /// <summary>
    /// 获取指定类型的所有工具实例，并按优先级排序
    /// 实现 IPrioritized 接口的工具将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TUtility">工具类型</typeparam>
    /// <returns>按优先级排序后的工具实例列表</returns>
    public IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility
    {
        EnsureAvailable();
        return _container.GetAllByPriority<TUtility>();
    }

    #endregion

    #region Event Management

    /// <summary>
    ///     发送一个默认构造的新事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public void SendEvent<TEvent>() where TEvent : new()
    {
        var eventBus = GetOrCache<IEventBus>();
        eventBus.Send<TEvent>();
    }

    /// <summary>
    ///     发送一个具体的事件实例
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="e">事件参数</param>
    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(e);
        var eventBus = GetOrCache<IEventBus>();
        eventBus.Send(e);
    }

    /// <summary>
    ///     注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <returns>事件注销接口</returns>
    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var eventBus = GetOrCache<IEventBus>();
        if (eventBus == null) throw new InvalidOperationException("IEventBus not registered");
        return eventBus.Register(handler);
    }

    /// <summary>
    ///     取消对某类型事件的监听
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">之前绑定的事件处理器</param>
    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        ArgumentNullException.ThrowIfNull(onEvent);
        var eventBus = GetOrCache<IEventBus>();
        eventBus.UnRegister(onEvent);
    }

    /// <summary>
    ///     获取当前环境对象
    /// </summary>
    /// <returns>环境对象实例</returns>
    public IEnvironment GetEnvironment()
    {
        var environment = GetOrCache<IEnvironment>();
        return environment ?? throw new InvalidOperationException("IEnvironment not registered");
    }

    #endregion
}
