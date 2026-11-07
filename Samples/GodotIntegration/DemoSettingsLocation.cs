// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Enums;

namespace GodotIntegration;

/// <summary>Defines the project-owned settings section key without leaking native paths into its data.</summary>
internal sealed class DemoSettingsLocation : IDataLocation
{
    /// <inheritdoc />
    public string Key => "preferences";

    /// <inheritdoc />
    public StorageKinds Kinds => StorageKinds.Local;

    /// <inheritdoc />
    public string? Namespace => null;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? Metadata => null;
}
