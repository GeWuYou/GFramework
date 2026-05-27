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
internal sealed class GodotArchitectureContext(
    IArchitectureContext innerContext,
    Func<bool>? shouldGuard = null)
    : IArchitectureContext
{
    private readonly IArchitectureContext _innerContext =
        innerContext ?? throw new ArgumentNullException(nameof(innerContext));

    private readonly Func<bool> _shouldGuard = shouldGuard ?? ShouldGuardSyncCqrsOnCurrentThread;

    public TService GetService<TService>() where TService : class
    {
        return _innerContext.GetService<TService>();
    }

    public IReadOnlyList<TService> GetServices<TService>() where TService : class
    {
        return _innerContext.GetServices<TService>();
    }

    public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return _innerContext.GetSystem<TSystem>();
    }

    public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
    {
        return _innerContext.GetSystems<TSystem>();
    }

    public TModel GetModel<TModel>() where TModel : class, IModel
    {
        return _innerContext.GetModel<TModel>();
    }

    public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
    {
        return _innerContext.GetModels<TModel>();
    }

    public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return _innerContext.GetUtility<TUtility>();
    }

    public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
    {
        return _innerContext.GetUtilities<TUtility>();
    }

    public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
    {
        return _innerContext.GetServicesByPriority<TService>();
    }

    public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
    {
        return _innerContext.GetSystemsByPriority<TSystem>();
    }

    public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
    {
        return _innerContext.GetModelsByPriority<TModel>();
    }

    public IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility
    {
        return _innerContext.GetUtilitiesByPriority<TUtility>();
    }

    public void SendCommand(ICommand command)
    {
        GuardSyncCqrs(nameof(SendCommand));
        _innerContext.SendCommand(command);
    }

    public TResult SendCommand<TResult>(ICommand<TResult> command)
    {
        GuardSyncCqrs(nameof(SendCommand));
        return _innerContext.SendCommand(command);
    }

    public TResponse SendCommand<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command)
    {
        GuardSyncCqrs(nameof(SendCommand));
        return _innerContext.SendCommand(command);
    }

    public Task SendCommandAsync(IAsyncCommand command)
    {
        return _innerContext.SendCommandAsync(command);
    }

    public ValueTask<TResponse> SendCommandAsync<TResponse>(
        GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        return _innerContext.SendCommandAsync(command, cancellationToken);
    }

    public Task<TResult> SendCommandAsync<TResult>(IAsyncCommand<TResult> command)
    {
        return _innerContext.SendCommandAsync(command);
    }

    public TResult SendQuery<TResult>(IQuery<TResult> query)
    {
        GuardSyncCqrs(nameof(SendQuery));
        return _innerContext.SendQuery(query);
    }

    public TResponse SendQuery<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query)
    {
        GuardSyncCqrs(nameof(SendQuery));
        return _innerContext.SendQuery(query);
    }

    public Task<TResult> SendQueryAsync<TResult>(IAsyncQuery<TResult> query)
    {
        return _innerContext.SendQueryAsync(query);
    }

    public ValueTask<TResponse> SendQueryAsync<TResponse>(
        GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        return _innerContext.SendQueryAsync(query, cancellationToken);
    }

    public void SendEvent<TEvent>() where TEvent : new()
    {
        _innerContext.SendEvent<TEvent>();
    }

    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
        _innerContext.SendEvent(e);
    }

    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        return _innerContext.RegisterEvent(handler);
    }

    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        _innerContext.UnRegisterEvent(onEvent);
    }

    public IEnvironment GetEnvironment()
    {
        return _innerContext.GetEnvironment();
    }

    public ValueTask<TResponse> SendRequestAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        return _innerContext.SendRequestAsync(request, cancellationToken);
    }

    public TResponse SendRequest<TResponse>(IRequest<TResponse> request)
    {
        GuardSyncCqrs(nameof(SendRequest));
        return _innerContext.SendRequest(request);
    }

    public ValueTask PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        return _innerContext.PublishAsync(notification, cancellationToken);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        return _innerContext.CreateStream(request, cancellationToken);
    }

    public ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : IRequest<Unit>
    {
        return _innerContext.SendAsync(command, cancellationToken);
    }

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

        throw new InvalidOperationException(
            $"Godot main-thread synchronous CQRS call '{apiName}' is not allowed. " +
            "Legacy synchronous CQRS dispatch may execute handlers on a worker thread, which can break thread-affine Godot APIs " +
            "such as Node, SceneTree, UI routing, and pause handlers. Use SendAsync(...), SendCommandAsync(...), or RunCommandCoroutine(...) instead.");
    }

    private static bool ShouldGuardSyncCqrsOnCurrentThread()
    {
        return Engine.GetMainLoop() is SceneTree && GodotThread.IsMainThread();
    }
}
