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

using DodgeTheCreeps.Advanced.Gameplay;

namespace DodgeTheCreeps.Advanced.Modding;

/// <summary>Explicit game APIs and per-run data scopes for independently compiled desktop mods.</summary>
internal sealed class ModService
{
    private readonly List<Func<int, int>> _scoreModifiers = [];
    /// <summary>The active save envelope, replaced on new run or confirmed continuation.</summary>
    internal ModSaveSession State { get; set; } = null!;
    private readonly IGameDispatcher _gameDispatcher;
    private readonly WorldState _worldState;
    private Content.DodgeContent? _content;

    /// <summary>Creates the mod boundary before content discovery, without executing a mod.</summary>
    internal ModService(IGameDispatcher dispatcher, WorldState worldState)
    {
        _gameDispatcher = dispatcher;
        _worldState = worldState;
    }

    /// <summary>Attaches the explicit content authority before mod trust checks can run.</summary>
    internal void SetContent(Content.DodgeContent content) => _content = content;

    /// <summary>Current API registration count for native acceptance.</summary>
    internal int RegistrationCount => _scoreModifiers.Count;

    /// <summary>Shares exact contract and target assemblies while keeping mod dependencies private.</summary>
    internal GameModHostOptions CreateOptions() => new()
    {
        GameId = "dodge-the-creeps-advanced",
        CodeApiVersion = "1.0.0",
        Dispatcher = _gameDispatcher,
        SharedAssemblies = [typeof(IDodgeModApi).Assembly, typeof(GameApplication).Assembly],
        TrustPolicy = stamp => (_content ?? throw new InvalidOperationException("Content trust authority is not attached.")).IsCodeApproved(stamp),
        CreateServices = context => new DodgeModServices(this, context.Package.Id),
        Extensions = [new HarmonyModExtension()],
    };

    /// <summary>Creates a bounded mod-owned API facade for the same contract used by loaded code.</summary>
    internal IDodgeModApi CreateApi(string modId) => new DodgeModServices(this, modId);

    /// <summary>Applies API registrations in deterministic initialization order on the gameplay thread.</summary>
    internal int ModifyScore(int increment)
    {
        foreach (var modifier in _scoreModifiers.ToArray())
            increment = modifier(increment);
        return increment;
    }

    /// <summary>Distinguishes mod-host startup from per-run data lifetime with an actionable contract error.</summary>
    private ModSaveScope ActiveModState(string modId)
    {
        if (_worldState.World is not { IsRunning: true })
            throw new InvalidOperationException("Start or load a run before accessing mod save data.");
        return State.ForMod(modId);
    }

    /// <summary>Supplies one mod-bound facade; the facade resolves the current run at each access.</summary>
    private sealed class DodgeModServices(ModService app, string modId) : IServiceProvider, IDodgeModApi
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
