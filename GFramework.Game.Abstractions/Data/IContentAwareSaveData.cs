// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Abstractions.Data;

/// <summary>
/// Opts a save DTO into content dependency checks. Metadata travels in the same atomic file as game data.
/// Leave <see cref="Content"/> null only for a legacy save awaiting an explicit import.
/// </summary>
public interface IContentAwareSaveData : IData
{
    /// <summary>Gets or sets the effective content configuration used by this save.</summary>
    ContentSnapshot? Content { get; set; }
}
