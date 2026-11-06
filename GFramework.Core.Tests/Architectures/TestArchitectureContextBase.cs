// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.Ioc;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为架构相关测试替身提供共享的 <see cref="IArchitectureContext" /> 基础实现。
/// </summary>
/// <remarks>
///     该基类统一维护容器解析与共享 <see cref="EventBus" /> 语义，
///     以避免多个测试上下文因为并行复制实现而在后续演进中发生语义漂移。
///     请求/通知/流式分发不属于 <see cref="IArchitectureContext" /> 契约，
///     需要分发能力的测试应通过 <c>GFramework.Cqrs.Extensions</c> 的上下文扩展进入 CQRS runtime。
/// </remarks>
public abstract class TestArchitectureContextBase : IArchitectureContext
{
    private readonly MicrosoftDiContainer _container = new();
    private readonly DefaultEnvironment _environment = new();
    private readonly EventBus _eventBus = new();

    /// <summary>
    ///     获取用于解析测试服务的依赖注入容器。
    /// </summary>
    public IIocContainer Container => _container;

    /// <summary>
    ///     获取测试事件总线实例。
    /// </summary>
    /// <remarks>
    ///     返回同一个缓存事件总线，以便 <see cref="RegisterEvent{TEvent}" />、<see cref="SendEvent{TEvent}()" /> 与
    ///     <see cref="UnRegisterEvent{TEvent}" /> 在同一份订阅状态上协作。
    /// </remarks>
    public IEventBus EventBus => _eventBus;

    /// <summary>
    ///     获取默认测试环境对象。
    /// </summary>
    public IEnvironment Environment => _environment;

    /// <summary>
    ///     获取指定类型的服务实例。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>已注册的服务实例。</returns>
    /// <exception cref="InvalidOperationException">未注册服务时抛出。</exception>
    public TService GetService<TService>() where TService : class
    {
        return _container.GetRequired<TService>();
    }

    /// <summary>
    ///     获取指定类型的所有服务实例。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>服务实例列表。</returns>
    public IReadOnlyList<TService> GetServices<TService>() where TService : class
    {
        return _container.GetAll<TService>();
    }

    /// <summary>
    ///     获取指定类型的模型实例。
    /// </summary>
    /// <typeparam name="TModel">模型类型。</typeparam>
    /// <returns>已注册的模型实例。</returns>
    /// <exception cref="InvalidOperationException">未注册模型时抛出。</exception>
    public TModel GetModel<TModel>() where TModel : class, IModel
    {
        return _container.GetRequired<TModel>();
    }

    /// <summary>
    ///     获取指定类型的所有模型实例。
    /// </summary>
    /// <typeparam name="TModel">模型类型。</typeparam>
    /// <returns>模型实例列表。</returns>
    public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
    {
        return _container.GetAll<TModel>();
    }

    /// <summary>
    ///     获取指定类型的系统实例。
    /// </summary>
    /// <typeparam name="TSystem">系统类型。</typeparam>
    /// <returns>已注册的系统实例。</returns>
    /// <exception cref="InvalidOperationException">未注册系统时抛出。</exception>
    public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetRequired<TSystem>();
    }

    /// <summary>
    ///     获取指定类型的所有系统实例。
    /// </summary>
    /// <typeparam name="TSystem">系统类型。</typeparam>
    /// <returns>系统实例列表。</returns>
    public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetAll<TSystem>();
    }

    /// <summary>
    ///     获取指定类型的工具实例。
    /// </summary>
    /// <typeparam name="TUtility">工具类型。</typeparam>
    /// <returns>已注册的工具实例。</returns>
    /// <exception cref="InvalidOperationException">未注册工具时抛出。</exception>
    public virtual TUtility GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return _container.GetRequired<TUtility>();
    }

    /// <summary>
    ///     获取指定类型的所有工具实例。
    /// </summary>
    /// <typeparam name="TUtility">工具类型。</typeparam>
    /// <returns>工具实例列表。</returns>
    public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
    {
        return _container.GetAll<TUtility>();
    }

    /// <summary>
    ///     获取指定类型的所有服务实例，并按优先级排序。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>按优先级排序后的服务实例列表。</returns>
    public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
    {
        return _container.GetAllByPriority<TService>();
    }

    /// <summary>
    ///     获取指定类型的所有系统实例，并按优先级排序。
    /// </summary>
    /// <typeparam name="TSystem">系统类型。</typeparam>
    /// <returns>按优先级排序后的系统实例列表。</returns>
    public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetAllByPriority<TSystem>();
    }

    /// <summary>
    ///     获取指定类型的所有模型实例，并按优先级排序。
    /// </summary>
    /// <typeparam name="TModel">模型类型。</typeparam>
    /// <returns>按优先级排序后的模型实例列表。</returns>
    public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
    {
        return _container.GetAllByPriority<TModel>();
    }

    /// <summary>
    ///     获取指定类型的所有工具实例，并按优先级排序。
    /// </summary>
    /// <typeparam name="TUtility">工具类型。</typeparam>
    /// <returns>按优先级排序后的工具实例列表。</returns>
    public IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility
    {
        return _container.GetAllByPriority<TUtility>();
    }

    /// <summary>
    ///     发送无参数事件。
    /// </summary>
    /// <typeparam name="TEvent">事件类型。</typeparam>
    public void SendEvent<TEvent>() where TEvent : new()
    {
        _eventBus.Send<TEvent>();
    }

    /// <summary>
    ///     发送带参数事件。
    /// </summary>
    /// <typeparam name="TEvent">事件类型。</typeparam>
    /// <param name="e">事件实例。</param>
    /// <exception cref="ArgumentNullException"><paramref name="e" /> 为 <see langword="null" />。</exception>
    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(e);
        _eventBus.Send(e);
    }

    /// <summary>
    ///     注册事件处理器。
    /// </summary>
    /// <typeparam name="TEvent">事件类型。</typeparam>
    /// <param name="handler">事件处理委托。</param>
    /// <returns>用于测试的事件注销句柄。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler" /> 为 <see langword="null" />。</exception>
    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _eventBus.Register(handler);
    }

    /// <summary>
    ///     取消注册事件处理器。
    /// </summary>
    /// <typeparam name="TEvent">事件类型。</typeparam>
    /// <param name="onEvent">事件处理委托。</param>
    /// <exception cref="ArgumentNullException"><paramref name="onEvent" /> 为 <see langword="null" />。</exception>
    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        ArgumentNullException.ThrowIfNull(onEvent);
        _eventBus.UnRegister(onEvent);
    }

    /// <summary>
    ///     获取当前环境对象。
    /// </summary>
    /// <returns>默认测试环境对象。</returns>
    public IEnvironment GetEnvironment()
    {
        return Environment;
    }
}
