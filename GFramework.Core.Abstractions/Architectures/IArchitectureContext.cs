// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
///     架构上下文接口，统一暴露框架组件访问、服务解析与事件发布入口。
/// </summary>
/// <remarks>
///     <para>该接口只承载架构自身的服务解析、组件检索、事件总线与环境访问能力，不包含任何消息分发契约。</para>
///     <para>请求、通知与流式分发由可选安装的 CQRS runtime 提供；推荐通过解析 <c>ICqrsRuntime</c> 服务或
///     <c>GFramework.Cqrs.Extensions</c> 中的上下文扩展方法进入 dispatcher。</para>
/// </remarks>
public interface IArchitectureContext
{
    /// <summary>
    ///     获取指定类型的服务实例
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>服务实例，如果不存在则抛出异常</returns>
    TService GetService<TService>() where TService : class;

    /// <summary>
    ///     获取指定类型的所有服务实例
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>所有符合条件的服务实例列表</returns>
    IReadOnlyList<TService> GetServices<TService>() where TService : class;

    /// <summary>
    ///     获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="TSystem">系统类型，必须继承自ISystem接口</typeparam>
    /// <returns>系统实例，如果不存在则抛出异常</returns>
    TSystem GetSystem<TSystem>() where TSystem : class, ISystem;

    /// <summary>
    ///     获取指定类型的所有系统实例
    /// </summary>
    /// <typeparam name="TSystem">系统类型，必须继承自ISystem接口</typeparam>
    /// <returns>所有符合条件的系统实例列表</returns>
    IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem;

    /// <summary>
    ///     获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="TModel">模型类型，必须继承自IModel接口</typeparam>
    /// <returns>模型实例，如果不存在则抛出异常</returns>
    TModel GetModel<TModel>() where TModel : class, IModel;

    /// <summary>
    ///     获取指定类型的所有模型实例
    /// </summary>
    /// <typeparam name="TModel">模型类型，必须继承自IModel接口</typeparam>
    /// <returns>所有符合条件的模型实例列表</returns>
    IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel;

    /// <summary>
    ///     获取指定类型的工具类实例
    /// </summary>
    /// <typeparam name="TUtility">工具类类型，必须继承自IUtility接口</typeparam>
    /// <returns>工具类实例，如果不存在则抛出异常</returns>
    TUtility GetUtility<TUtility>() where TUtility : class, IUtility;

    /// <summary>
    ///     获取指定类型的所有工具类实例
    /// </summary>
    /// <typeparam name="TUtility">工具类类型，必须继承自IUtility接口</typeparam>
    /// <returns>所有符合条件的工具类实例列表</returns>
    IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility;

    /// <summary>
    ///     获取指定类型的所有服务实例，并按优先级排序
    ///     实现 IPrioritized 接口的服务将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>按优先级排序后的服务实例列表</returns>
    IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class;

    /// <summary>
    ///     获取指定类型的所有系统实例，并按优先级排序
    ///     实现 IPrioritized 接口的系统将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TSystem">系统类型，必须继承自ISystem接口</typeparam>
    /// <returns>按优先级排序后的系统实例列表</returns>
    IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem;

    /// <summary>
    ///     获取指定类型的所有模型实例，并按优先级排序
    ///     实现 IPrioritized 接口的模型将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TModel">模型类型，必须继承自IModel接口</typeparam>
    /// <returns>按优先级排序后的模型实例列表</returns>
    IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel;

    /// <summary>
    ///     获取指定类型的所有工具类实例，并按优先级排序
    ///     实现 IPrioritized 接口的工具将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TUtility">工具类类型，必须继承自IUtility接口</typeparam>
    /// <returns>按优先级排序后的工具类实例列表</returns>
    IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility;

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

    /// <summary>
    ///     获取环境对象
    /// </summary>
    /// <returns>环境对象实例</returns>
    IEnvironment GetEnvironment();
}
