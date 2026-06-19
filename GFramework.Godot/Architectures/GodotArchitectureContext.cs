// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Cqrs.Abstractions.Cqrs;
using Godot;
using ICommand = GFramework.Core.Abstractions.Command.ICommand;

namespace GFramework.Godot.Architectures;

/// <summary>
///     为 Godot 架构上下文增加主线程同步 CQRS 保护，避免线程亲和 API 被 legacy 同步 bridge 静默切到线程池执行。
/// </summary>
/// <remarks>
///     当调用发生在 Godot 主线程时，该包装器会拒绝同步 CQRS 入口并显式提示对应的异步迁移路径，
///     以避免节点、场景树、UI 路由和暂停处理器在错误线程上运行。
/// </remarks>
internal sealed class GodotArchitectureContext(
    IArchitectureContext innerContext,
    Func<bool>? shouldGuard = null)
    : IArchitectureContext
{
    private readonly IArchitectureContext _innerContext =
        innerContext ?? throw new ArgumentNullException(nameof(innerContext));

    private readonly Func<bool> _shouldGuard = shouldGuard ?? ShouldGuardSyncCqrsOnCurrentThread;

    /// <inheritdoc />
    public TService GetService<TService>() where TService : class
    {
        return _innerContext.GetService<TService>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TService> GetServices<TService>() where TService : class
    {
        return _innerContext.GetServices<TService>();
    }

    /// <inheritdoc />
    public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return _innerContext.GetSystem<TSystem>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
    {
        return _innerContext.GetSystems<TSystem>();
    }

    /// <inheritdoc />
    public TModel GetModel<TModel>() where TModel : class, IModel
    {
        return _innerContext.GetModel<TModel>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
    {
        return _innerContext.GetModels<TModel>();
    }

    /// <inheritdoc />
    public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return _innerContext.GetUtility<TUtility>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
    {
        return _innerContext.GetUtilities<TUtility>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
    {
        return _innerContext.GetServicesByPriority<TService>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
    {
        return _innerContext.GetSystemsByPriority<TSystem>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
    {
        return _innerContext.GetModelsByPriority<TModel>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility
    {
        return _innerContext.GetUtilitiesByPriority<TUtility>();
    }

    /// <summary>
    ///     拦截旧版同步命令入口，并在 Godot 主线程保护启用时拒绝继续派发。
    /// </summary>
    /// <param name="command">要发送的命令。</param>
    /// <exception cref="InvalidOperationException">当当前线程不允许同步 CQRS 调用时抛出。</exception>
    public void SendCommand(ICommand command)
    {
        GuardSyncCqrs(nameof(SendCommand));
        _innerContext.SendCommand(command);
    }

    /// <summary>
    ///     拦截旧版同步命令入口，并在 Godot 主线程保护启用时拒绝继续派发。
    /// </summary>
    /// <typeparam name="TResult">命令结果类型。</typeparam>
    /// <param name="command">要发送的命令。</param>
    /// <returns>命令结果。</returns>
    /// <exception cref="InvalidOperationException">当当前线程不允许同步 CQRS 调用时抛出。</exception>
    public TResult SendCommand<TResult>(ICommand<TResult> command)
    {
        GuardSyncCqrs(nameof(SendCommand));
        return _innerContext.SendCommand(command);
    }

    /// <summary>
    ///     拦截新版同步 CQRS 命令入口，并在 Godot 主线程保护启用时拒绝继续派发。
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型。</typeparam>
    /// <param name="command">要发送的命令。</param>
    /// <returns>命令结果。</returns>
    /// <exception cref="InvalidOperationException">当当前线程不允许同步 CQRS 调用时抛出。</exception>
    public TResponse SendCommand<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command)
    {
        GuardSyncCqrs(nameof(SendCommand));
        return _innerContext.SendCommand(command);
    }

    /// <inheritdoc />
    public Task SendCommandAsync(IAsyncCommand command)
    {
        return _innerContext.SendCommandAsync(command);
    }

    /// <inheritdoc />
    public ValueTask<TResponse> SendCommandAsync<TResponse>(
        GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        return _innerContext.SendCommandAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TResult> SendCommandAsync<TResult>(IAsyncCommand<TResult> command)
    {
        return _innerContext.SendCommandAsync(command);
    }

    /// <summary>
    ///     拦截旧版同步查询入口，并在 Godot 主线程保护启用时拒绝继续派发。
    /// </summary>
    /// <typeparam name="TResult">查询结果类型。</typeparam>
    /// <param name="query">要发送的查询。</param>
    /// <returns>查询结果。</returns>
    /// <exception cref="InvalidOperationException">当当前线程不允许同步 CQRS 调用时抛出。</exception>
    public TResult SendQuery<TResult>(IQuery<TResult> query)
    {
        GuardSyncCqrs(nameof(SendQuery));
        return _innerContext.SendQuery(query);
    }

    /// <summary>
    ///     拦截新版同步 CQRS 查询入口，并在 Godot 主线程保护启用时拒绝继续派发。
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型。</typeparam>
    /// <param name="query">要发送的查询。</param>
    /// <returns>查询结果。</returns>
    /// <exception cref="InvalidOperationException">当当前线程不允许同步 CQRS 调用时抛出。</exception>
    public TResponse SendQuery<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query)
    {
        GuardSyncCqrs(nameof(SendQuery));
        return _innerContext.SendQuery(query);
    }

    /// <inheritdoc />
    public Task<TResult> SendQueryAsync<TResult>(IAsyncQuery<TResult> query)
    {
        return _innerContext.SendQueryAsync(query);
    }

    /// <inheritdoc />
    public ValueTask<TResponse> SendQueryAsync<TResponse>(
        GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        return _innerContext.SendQueryAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public void SendEvent<TEvent>() where TEvent : new()
    {
        _innerContext.SendEvent<TEvent>();
    }

    /// <inheritdoc />
    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
        _innerContext.SendEvent(e);
    }

    /// <inheritdoc />
    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        return _innerContext.RegisterEvent(handler);
    }

    /// <inheritdoc />
    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        _innerContext.UnRegisterEvent(onEvent);
    }

    /// <inheritdoc />
    public IEnvironment GetEnvironment()
    {
        return _innerContext.GetEnvironment();
    }

    /// <inheritdoc />
    public ValueTask<TResponse> SendRequestAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        return _innerContext.SendRequestAsync(request, cancellationToken);
    }

    /// <summary>
    ///     拦截同步 CQRS request 入口，并在 Godot 主线程保护启用时拒绝继续派发。
    /// </summary>
    /// <typeparam name="TResponse">请求响应类型。</typeparam>
    /// <param name="request">要发送的请求。</param>
    /// <returns>请求结果。</returns>
    /// <exception cref="InvalidOperationException">当当前线程不允许同步 CQRS 调用时抛出。</exception>
    public TResponse SendRequest<TResponse>(IRequest<TResponse> request)
    {
        GuardSyncCqrs(nameof(SendRequest));
        return _innerContext.SendRequest(request);
    }

    /// <inheritdoc />
    public ValueTask PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        return _innerContext.PublishAsync(notification, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        return _innerContext.CreateStream(request, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : IRequest<Unit>
    {
        return _innerContext.SendAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        return _innerContext.SendAsync(command, cancellationToken);
    }

    private void GuardSyncCqrs(string apiName)
    {
        if (!_shouldGuard())
            return;

        var asyncAlternative = apiName switch
        {
            nameof(SendQuery) => "Use SendQueryAsync(...), SendAsync(...), or RunCommandCoroutine(...) instead.",
            nameof(SendRequest) => "Use SendRequestAsync(...), SendAsync(...), or RunCommandCoroutine(...) instead.",
            _ => "Use SendCommandAsync(...), SendAsync(...), or RunCommandCoroutine(...) instead."
        };

        throw new InvalidOperationException(
            $"Godot main-thread synchronous CQRS call '{apiName}' is not allowed. " +
            "Legacy synchronous CQRS dispatch may execute handlers on a worker thread, which can break thread-affine Godot APIs " +
            $"such as Node, SceneTree, UI routing, and pause handlers. {asyncAlternative}");
    }

    private static bool ShouldGuardSyncCqrsOnCurrentThread()
    {
        return Engine.GetMainLoop() is SceneTree && GodotThread.IsMainThread();
    }
}
