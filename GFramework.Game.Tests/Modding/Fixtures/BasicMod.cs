// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Modding;

namespace FixtureMod;

/// <summary>Externally compiled mod used to observe actual load contexts and lifecycle behavior.</summary>
public sealed class BasicMod : IGameMod
{
    private Action<string>? _observe;
    private string _id = string.Empty;
    /// <inheritdoc />
    public Task InitializeAsync(IGameModContext context, CancellationToken cancellationToken)
    {
        _id = context.Package.Id;
        _observe = (Action<string>?)context.Services.GetService(typeof(Action<string>));
        _observe?.Invoke(_id + ":init:" + FixturePrivate.VersionInfo.Value);
        context.Own(new CallbackDisposable(() =>
        {
            _observe?.Invoke(_id + ":dispose");
            if (string.Equals(_id, "dispose-failing", StringComparison.Ordinal)) throw new InvalidOperationException("Fixture resource cleanup failed.");
        }));
        if (string.Equals(_id, "failing", StringComparison.Ordinal)) throw new InvalidOperationException("Fixture initialization failed.");
        return Task.CompletedTask;
    }
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _observe?.Invoke(_id + ":start");
        return Task.CompletedTask;
    }
    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _observe?.Invoke(_id + ":stop");
        if (string.Equals(_id, "stop-failing", StringComparison.Ordinal)) throw new InvalidOperationException("Fixture stop failed.");
        return Task.CompletedTask;
    }
    /// <summary>Reports tracked resource cleanup across the real assembly boundary.</summary>
    private sealed class CallbackDisposable : IDisposable
    {
        private readonly Action _action;
        /// <summary>Captures the cleanup observer.</summary>
        internal CallbackDisposable(Action action) => _action = action;
        /// <inheritdoc />
        public void Dispose() => _action();
    }
}
