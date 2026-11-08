// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using DodgeTheCreeps.ModApi;
using GFramework.Game.Abstractions.Modding;

namespace DodgeTheCreeps.BonusMod;

/// <summary>Separately compiled gameplay extension using only the game's published API.</summary>
public sealed class BonusScoreMod : IGameMod
{
    private IDodgeModApi? _api;

    /// <inheritdoc />
    public Task InitializeAsync(IGameModContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _api = context.Services.GetService(typeof(IDodgeModApi)) as IDodgeModApi
            ?? throw new InvalidOperationException("Dodge mod API is not available.");
        context.Own(_api.RegisterScoreModifier(increment =>
        {
            var state = _api.ReadState<BonusState>("score") ?? new BonusState();
            state.Awards++;
            _api.WriteState("score", state);
            return checked(increment + 1);
        }));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _api = null;
        return Task.CompletedTask;
    }

    /// <summary>Portable, mod-owned state restored without constructing mod-specific CLR types from the file.</summary>
    public sealed class BonusState
    {
        /// <summary>Gets or sets the number of bonus awards in this run.</summary>
        public int Awards { get; set; }
    }
}
