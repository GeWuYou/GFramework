// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Declares one managed mod entrypoint; it does not grant permission to execute it.</summary>
public sealed class ContentCodeEntrypoint
{
    /// <summary>Gets the package-relative managed DLL path.</summary>
    public string AssemblyPath { get; init; } = string.Empty;
    /// <summary>Gets the exact full name of the public, parameterless IGameMod implementation.</summary>
    public string EntryType { get; init; } = string.Empty;
    /// <summary>Gets the game-owned code API versions accepted by this mod.</summary>
    public ContentVersionRange ApiVersions { get; init; } = new();
    /// <summary>Gets explicitly required host extension capability names, such as harmony.</summary>
    public IReadOnlyList<string> RequiredCapabilities { get; init; } = [];
}
