using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.Ioc;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using ICommand = GFramework.Core.Abstractions.Command.ICommand;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="ArchitectureServicesTests" /> 提供最小实现的架构上下文测试桩。
/// </summary>
/// <remarks>
///     该类型仅用于验证 <see cref="ArchitectureServices" /> 的上下文传递行为，因此仅保留当前测试切片需要的容器、
///     环境与接口实现。所有不在本测试范围内的 CQRS 调用均明确抛出 <see cref="NotSupportedException" />，
///     以避免误把测试桩当作真实运行时上下文使用。
/// </remarks>
public class TestArchitectureContextV3 : IArchitectureContext
{
    private readonly MicrosoftDiContainer _container = new();
    private readonly DefaultEnvironment _environment = new();

    /// <summary>
    ///     获取或初始化用于区分测试上下文实例的标识。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    ///     获取指定类型的服务实例。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>已注册的服务实例。</returns>
    /// <exception cref="InvalidOperationException">未注册或存在多个同类型实例时抛出。</exception>
    public TService GetService<TService>() where TService : class
    {
        return _container.GetRequired<TService>();
    }

    /// <summary>
    ///     获取指定类型的所有服务实例。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>所有已注册的服务实例。</returns>
    public IReadOnlyList<TService> GetServices<TService>() where TService : class
    {
        return _container.GetAll<TService>();
    }

    /// <summary>
    ///     获取指定类型的模型实例。
    /// </summary>
    /// <typeparam name="TModel">模型类型。</typeparam>
    /// <returns>已注册的模型实例。</returns>
    /// <exception cref="InvalidOperationException">未注册或存在多个同类型实例时抛出。</exception>
    public TModel GetModel<TModel>() where TModel : class, IModel
    {
        return _container.GetRequired<TModel>();
    }

    /// <summary>
    ///     获取指定类型的所有模型实例。
    /// </summary>
    /// <typeparam name="TModel">模型类型。</typeparam>
    /// <returns>所有已注册的模型实例。</returns>
    public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
    {
        return _container.GetAll<TModel>();
    }

    /// <summary>
    ///     获取指定类型的系统实例。
    /// </summary>
    /// <typeparam name="TSystem">系统类型。</typeparam>
    /// <returns>已注册的系统实例。</returns>
    /// <exception cref="InvalidOperationException">未注册或存在多个同类型实例时抛出。</exception>
    public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetRequired<TSystem>();
    }

    /// <summary>
    ///     获取指定类型的所有系统实例。
    /// </summary>
    /// <typeparam name="TSystem">系统类型。</typeparam>
    /// <returns>所有已注册的系统实例。</returns>
    public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetAll<TSystem>();
    }

    /// <summary>
    ///     获取指定类型的工具实例。
    /// </summary>
    /// <typeparam name="TUtility">工具类型。</typeparam>
    /// <returns>已注册的工具实例。</returns>
    /// <exception cref="InvalidOperationException">未注册或存在多个同类型实例时抛出。</exception>
    public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return _container.GetRequired<TUtility>();
    }

    /// <summary>
    ///     获取指定类型的所有工具实例。
    /// </summary>
    /// <typeparam name="TUtility">工具类型。</typeparam>
    /// <returns>所有已注册的工具实例。</returns>
    public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
    {
        return _container.GetAll<TUtility>();
    }

    /// <summary>
    ///     获取指定类型的所有服务实例，并按优先级排序。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>按优先级排序后的服务实例。</returns>
    public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
    {
        return _container.GetAllByPriority<TService>();
    }

    /// <summary>
    ///     获取指定类型的所有系统实例，并按优先级排序。
    /// </summary>
    /// <typeparam name="TSystem">系统类型。</typeparam>
    /// <returns>按优先级排序后的系统实例。</returns>
    public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetAllByPriority<TSystem>();
    }

    /// <summary>
    ///     获取指定类型的所有模型实例，并按优先级排序。
    /// </summary>
    /// <typeparam name="TModel">模型类型。</typeparam>
    /// <returns>按优先级排序后的模型实例。</returns>
    public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
    {
        return _container.GetAllByPriority<TModel>();
    }

    /// <summary>
    ///     获取指定类型的所有工具实例，并按优先级排序。
    /// </summary>
    /// <typeparam name="TUtility">工具类型。</typeparam>
    /// <returns>按优先级排序后的工具实例。</returns>
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
    }

    /// <summary>
    ///     发送带参数事件。
    /// </summary>
    /// <typeparam name="TEvent">事件类型。</typeparam>
    /// <param name="e">事件实例。</param>
    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
    }

    /// <summary>
    ///     注册事件处理器。
    /// </summary>
    /// <typeparam name="TEvent">事件类型。</typeparam>
    /// <param name="handler">事件处理回调。</param>
    /// <returns>用于注销回调的句柄。</returns>
    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        return new DefaultUnRegister(() => { });
    }

    /// <summary>
    ///     取消事件处理器注册。
    /// </summary>
    /// <typeparam name="TEvent">事件类型。</typeparam>
    /// <param name="onEvent">要取消的事件回调。</param>
    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
    }

    /// <summary>
    ///     异步发送新版 CQRS 请求。
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
    ///     同步发送新版 CQRS 请求。
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
    ///     异步发送新版 CQRS 命令并返回响应。
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
    ///     同步发送新版 CQRS 命令并返回响应。
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
    ///     异步发送新版 CQRS 查询并返回结果。
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
    ///     同步发送新版 CQRS 查询并返回结果。
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
    ///     发布通知消息。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="notification">通知实例。</param>
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
    ///     创建流式请求结果。
    /// </summary>
    /// <typeparam name="TResponse">流元素类型。</typeparam>
    /// <param name="request">流式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>流式响应序列。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持此成员。</exception>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     异步发送无返回值请求命令。
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
    ///     异步发送带返回值请求。
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
    ///     发送旧版无返回值命令。
    /// </summary>
    /// <param name="command">要发送的命令。</param>
    /// <exception cref="NotSupportedException">该测试桩不支持旧版命令执行入口。</exception>
    public void SendCommand(ICommand command)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     发送旧版带返回值命令。
    /// </summary>
    /// <typeparam name="TResult">命令响应类型。</typeparam>
    /// <param name="command">要发送的命令。</param>
    /// <returns>此方法始终抛出异常，不返回结果。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持旧版命令执行入口。</exception>
    public TResult SendCommand<TResult>(GFramework.Core.Abstractions.Command.ICommand<TResult> command)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     异步发送旧版无返回值命令。
    /// </summary>
    /// <param name="command">要发送的命令。</param>
    /// <returns>已失败的任务。</returns>
    public Task SendCommandAsync(IAsyncCommand command)
    {
        return Task.FromException(new NotSupportedException());
    }

    /// <summary>
    ///     异步发送旧版带返回值命令。
    /// </summary>
    /// <typeparam name="TResult">命令响应类型。</typeparam>
    /// <param name="command">要发送的命令。</param>
    /// <returns>已失败的任务。</returns>
    public Task<TResult> SendCommandAsync<TResult>(IAsyncCommand<TResult> command)
    {
        return Task.FromException<TResult>(new NotSupportedException());
    }

    /// <summary>
    ///     发送旧版查询请求。
    /// </summary>
    /// <typeparam name="TResult">查询结果类型。</typeparam>
    /// <param name="query">要发送的查询。</param>
    /// <returns>此方法始终抛出异常，不返回结果。</returns>
    /// <exception cref="NotSupportedException">该测试桩不支持旧版查询执行入口。</exception>
    public TResult SendQuery<TResult>(GFramework.Core.Abstractions.Query.IQuery<TResult> query)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     异步发送旧版查询请求。
    /// </summary>
    /// <typeparam name="TResult">查询结果类型。</typeparam>
    /// <param name="query">要发送的查询。</param>
    /// <returns>已失败的任务。</returns>
    public Task<TResult> SendQueryAsync<TResult>(IAsyncQuery<TResult> query)
    {
        return Task.FromException<TResult>(new NotSupportedException());
    }

    /// <summary>
    ///     获取当前测试上下文绑定的环境实例。
    /// </summary>
    /// <returns>默认测试环境实例。</returns>
    public IEnvironment GetEnvironment()
    {
        return _environment;
    }
}
