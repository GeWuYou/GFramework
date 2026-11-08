// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using DodgeTheCreeps.ModApi;
using GFramework.Game.Data;
using GFramework.Game.Harmony;
using GFramework.Game.Modding;
using GFramework.Game.Abstractions.Runtime;
using Newtonsoft.Json.Linq;

namespace DodgeTheCreeps;

/// <summary>Explicit game APIs and per-run data scopes for independently compiled desktop mods.</summary>
public partial class DodgeApp
{
    private readonly List<Func<int, int>> _scoreModifiers = [];
    private ModSaveSession _modState = null!;
    private IGameDispatcher _gameDispatcher = null!;

    /// <summary>Shares exact contract and target assemblies while keeping mod dependencies private.</summary>
    private GameModHostOptions CreateModOptions() => new()
    {
        GameId = "dodge-the-creeps",
        CodeApiVersion = "1.0.0",
        Dispatcher = _gameDispatcher,
        SharedAssemblies = [typeof(IDodgeModApi).Assembly, typeof(DodgeApp).Assembly],
        TrustPolicy = stamp => _content.IsCodeApproved(stamp),
        CreateServices = context => new DodgeModServices(this, context.Package.Id),
        Extensions = [new HarmonyModExtension()],
    };

    /// <summary>Applies API registrations in deterministic initialization order on the gameplay thread.</summary>
    private int ModifyScore(int increment)
    {
        foreach (var modifier in _scoreModifiers.ToArray())
            increment = modifier(increment);
        return increment;
    }

    /// <summary>Distinguishes mod-host startup from per-run data lifetime with an actionable contract error.</summary>
    private ModSaveScope ActiveModState(string modId)
    {
        if (_world is not { IsRunning: true })
            throw new InvalidOperationException("Start or load a run before accessing mod save data.");
        return _modState.ForMod(modId);
    }

    /// <summary>Supplies one mod-bound facade; the facade resolves the current run at each access.</summary>
    private sealed class DodgeModServices(DodgeApp app, string modId) : IServiceProvider, IDodgeModApi
    {
        /// <inheritdoc />
        public object? GetService(Type serviceType) => serviceType == typeof(IDodgeModApi) ? this : null;

        /// <inheritdoc />
        public IDisposable RegisterScoreModifier(Func<int, int> modifier)
        {
            ArgumentNullException.ThrowIfNull(modifier);
            app._scoreModifiers.Add(modifier);
            return new ScoreRegistration(app._scoreModifiers, modifier);
        }

        /// <inheritdoc />
        public T? ReadState<T>(string key) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return app.ActiveModState(modId).Read<JObject>()?[key]?.ToObject<T>();
        }

        /// <inheritdoc />
        public void WriteState<T>(string key, T value) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(value);
            var scope = app.ActiveModState(modId);
            var data = scope.Read<JObject>() ?? [];
            data[key] = JToken.FromObject(value);
            scope.Write(data);
        }
    }

    /// <summary>Removes only the corresponding callback, including when cleanup is requested twice.</summary>
    private sealed class ScoreRegistration(List<Func<int, int>> registrations, Func<int, int> modifier) : IDisposable
    {
        private bool _disposed;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            registrations.Remove(modifier);
        }
    }
}
