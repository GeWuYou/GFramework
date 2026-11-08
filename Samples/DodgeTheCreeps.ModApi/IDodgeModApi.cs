// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;

namespace DodgeTheCreeps.ModApi;

/// <summary>Game-owned desktop mod contract, shared by the host and separately compiled mods.</summary>
/// <remarks>Invoke on the game dispatcher. Registrations last until disposed; state belongs to a running loaded/new run,
/// and is unavailable during mod initialization or while the main menu has no active run.</remarks>
public interface IDodgeModApi
{
    /// <summary>Registers a score transformation, applied in effective package order on the game thread.</summary>
    /// <param name="modifier">Transforms a positive score increment without accessing native nodes.</param>
    /// <returns>An independently disposable registration owned by the mod.</returns>
    IDisposable RegisterScoreModifier(Func<int, int> modifier);

    /// <summary>Reads a plain JSON value from this mod's current run, or returns null when absent.</summary>
    /// <typeparam name="T">A plain model with no native objects or runtime type metadata.</typeparam>
    /// <param name="key">A local key inside this mod's namespace.</param>
    /// <returns>A detached model; changing it does not alter the stored state.</returns>
    /// <exception cref="InvalidOperationException">There is no active run.</exception>
    T? ReadState<T>(string key) where T : class;

    /// <summary>Stages a plain JSON value for the next explicit run save.</summary>
    /// <typeparam name="T">A plain serializable model.</typeparam>
    /// <param name="key">A local key inside this mod's namespace.</param>
    /// <param name="value">A detached value to stage.</param>
    /// <exception cref="InvalidOperationException">There is no active run.</exception>
    void WriteState<T>(string key, T value) where T : class;
}
