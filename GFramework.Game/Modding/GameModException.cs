// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Modding;

/// <summary>Identifies the exact mod and lifecycle phase that failed without hiding its original exception.</summary>
public sealed class GameModException : Exception
{
    /// <summary>Creates a package-local lifecycle failure.</summary>
    /// <param name="packId">Responsible package.</param>
    /// <param name="phase">Lifecycle phase.</param>
    /// <param name="innerException">Original mod exception.</param>
    public GameModException(string packId, string phase, Exception innerException)
        : base($"Mod '{packId}' failed during {phase}: {innerException.Message}", innerException)
    {
        PackId = packId;
        Phase = phase;
    }
    /// <summary>Gets the responsible package.</summary>
    public string PackId { get; }
    /// <summary>Gets the failed lifecycle phase.</summary>
    public string Phase { get; }
}
