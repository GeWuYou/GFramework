// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Identifies a row in a game-owned extensible table.</summary>
/// <param name="TableName">Registered table name.</param>
/// <param name="Id">Stable content identity.</param>
public sealed record ContentReference(string TableName, ContentId Id);
