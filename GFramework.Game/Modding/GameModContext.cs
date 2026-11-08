// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Modding;
using GFramework.Game.Abstractions.Runtime;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.Modding;

/// <summary>Owns one mod's subscriptions and extension scopes, independently of shared host service ownership.</summary>
internal sealed class GameModContext : IGameModContext, IAsyncDisposable, IServiceProvider
{
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private readonly List<Func<ValueTask>> _cleanup = new();
    private readonly List<IGameModServiceScope> _extensions = new();
    private readonly IServiceProvider _shared;
    private IServiceProvider? _local;
    private bool _closed;

    /// <summary>Creates identity and services without invoking any mod entrypoint.</summary>
    internal GameModContext(ContentPackStamp package, GameModHostOptions options)
    {
        Package = package;
        GameId = options.GameId;
        Dispatcher = options.Dispatcher;
        Logger = options.LoggerFactory.CreateLogger("GameMod." + package.Id);
        _shared = options.Services;
    }
    /// <inheritdoc />
    public string GameId { get; }
    /// <inheritdoc />
    public ContentPackStamp Package { get; }
    /// <inheritdoc />
    public IServiceProvider Services => this;
    /// <inheritdoc />
    public IGameDispatcher Dispatcher { get; }
    /// <inheritdoc />
    public ILogger Logger { get; }
    /// <summary>Configures host adapters before the mod receives its context.</summary>
    internal void Configure(GameModHostOptions options, IReadOnlyList<IGameModExtension> extensions)
    {
        _local = options.CreateServices?.Invoke(this);
        foreach (var extension in extensions)
        {
            var scope = extension.CreateScope(this) ?? throw new InvalidOperationException("A mod extension returned no scope.");
            OwnAsync(scope);
            _extensions.Add(scope);
        }
    }
    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        if (_closed) throw new ObjectDisposedException(nameof(GameModContext));
        foreach (var extension in _extensions)
        {
            var service = extension.GetService(serviceType);
            if (service is not null) return service;
        }
        return _local?.GetService(serviceType) ?? _shared.GetService(serviceType);
    }
    /// <inheritdoc />
    public void Own(IDisposable resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        Track(() => { resource.Dispose(); return ValueTask.CompletedTask; });
    }
    /// <inheritdoc />
    public void OwnAsync(IAsyncDisposable resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        Track(resource.DisposeAsync);
    }
    /// <inheritdoc />
    public void OwnRegistration(IUnRegister registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        Track(() => { registration.UnRegister(); return ValueTask.CompletedTask; });
    }
    /// <summary>Serializes registration against shutdown so no accepted resource escapes cleanup.</summary>
    private void Track(Func<ValueTask> cleanup)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            _cleanup.Add(cleanup);
        }
    }
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Func<ValueTask>[] cleanup;
        lock (_gate)
        {
            if (_closed) return;
            _closed = true;
            cleanup = _cleanup.ToArray();
            _cleanup.Clear();
        }
        var failures = new List<Exception>();
        for (var index = cleanup.Length - 1; index >= 0; index--)
        {
            try { await cleanup[index]().ConfigureAwait(true); }
            catch (Exception exception) { failures.Add(exception); }
        }
        _extensions.Clear();
        _local = null;
        if (failures.Count != 0) throw new AggregateException("Mod resource cleanup failed.", failures);
    }
}
