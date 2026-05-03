// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Command;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.Ioc;
using GFramework.Core.Query;
using GFramework.Cqrs.Abstractions.Cqrs;
using ICommand = GFramework.Core.Abstractions.Command.ICommand;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为架构相关测试替身提供共享的 <see cref="IArchitectureContext" /> 基础实现。
/// </summary>
/// <remarks>
///     该基类统一维护容器解析、共享 <see cref="EventBus" /> 语义，以及 legacy / CQRS 入口的显式失败契约，
///     以避免多个测试上下文因为并行复制实现而在后续演进中发生语义漂移。
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
    ///     获取测试命令执行器实例。
    /// </summary>
    public ICommandExecutor CommandExecutor => new CommandExecutor();

    /// <summary>
    ///     获取测试查询执行器实例。
    /// </summary>
    public IQueryExecutor QueryExecutor => new QueryExecutor();

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
    ///     测试桩：异步发送统一 CQRS 请求。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">要发送的请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>请求响应任务。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public ValueTask<TResponse> SendRequestAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试桩：同步发送统一 CQRS 请求。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">要发送的请求。</param>
    /// <returns>请求响应。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public TResponse SendRequest<TResponse>(IRequest<TResponse> request)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试桩：异步发送 CQRS 命令并返回响应。
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型。</typeparam>
    /// <param name="command">要发送的命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>命令响应任务。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public ValueTask<TResponse> SendCommandAsync<TResponse>(
        GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试桩：同步发送 CQRS 命令并返回响应。
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型。</typeparam>
    /// <param name="command">要发送的命令。</param>
    /// <returns>命令响应。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public TResponse SendCommand<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试桩：异步发送 CQRS 查询并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">查询结果类型。</typeparam>
    /// <param name="query">要发送的查询。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>查询结果任务。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public ValueTask<TResponse> SendQueryAsync<TResponse>(
        GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试桩：同步发送 CQRS 查询并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">查询结果类型。</typeparam>
    /// <param name="query">要发送的查询。</param>
    /// <returns>查询结果。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public TResponse SendQuery<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试桩：异步发布 CQRS 通知。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="notification">要发布的通知。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>通知发布任务。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public ValueTask PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试桩：创建 CQRS 流式请求响应序列。
    /// </summary>
    /// <typeparam name="TResponse">流式响应元素类型。</typeparam>
    /// <param name="request">流式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步响应流。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试桩：异步发送无返回值 CQRS 命令。
    /// </summary>
    /// <typeparam name="TCommand">命令类型。</typeparam>
    /// <param name="command">要发送的命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>命令发送任务。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : IRequest<Unit>
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试桩：异步发送带返回值的 CQRS 请求。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="command">要发送的请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>请求响应任务。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     发送旧版命令。
    /// </summary>
    /// <param name="command">命令对象。</param>
    /// <exception cref="NotSupportedException">该测试桩不支持旧版命令执行入口。</exception>
    public void SendCommand(ICommand command)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     发送旧版带返回值命令。
    /// </summary>
    /// <typeparam name="TResult">返回值类型。</typeparam>
    /// <param name="command">命令对象。</param>
    /// <returns>此方法始终抛出异常，不返回结果。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持旧版命令执行入口。</exception>
    public TResult SendCommand<TResult>(ICommand<TResult> command)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     异步发送旧版命令。
    /// </summary>
    /// <param name="command">命令对象。</param>
    /// <returns>已失败的任务。</returns>
    public Task SendCommandAsync(IAsyncCommand command)
    {
        return Task.FromException(new NotSupportedException());
    }

    /// <summary>
    ///     异步发送旧版带返回值命令。
    /// </summary>
    /// <typeparam name="TResult">返回值类型。</typeparam>
    /// <param name="command">命令对象。</param>
    /// <returns>已失败的任务。</returns>
    public Task<TResult> SendCommandAsync<TResult>(IAsyncCommand<TResult> command)
    {
        return Task.FromException<TResult>(new NotSupportedException());
    }

    /// <summary>
    ///     发送旧版查询请求。
    /// </summary>
    /// <typeparam name="TResult">查询结果类型。</typeparam>
    /// <param name="query">查询对象。</param>
    /// <returns>此方法始终抛出异常，不返回结果。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持旧版查询执行入口。</exception>
    public TResult SendQuery<TResult>(IQuery<TResult> query)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     异步发送旧版查询请求。
    /// </summary>
    /// <typeparam name="TResult">查询结果类型。</typeparam>
    /// <param name="query">异步查询对象。</param>
    /// <returns>已失败的任务。</returns>
    public Task<TResult> SendQueryAsync<TResult>(IAsyncQuery<TResult> query)
    {
        return Task.FromException<TResult>(new NotSupportedException());
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
