// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Records one exact loaded package without embedding installation paths in saves.</summary>
/// <param name="Id">Canonical package identity.</param>
/// <param name="Version">Declared content version.</param>
/// <param name="Fingerprint">SHA-256 fingerprint of package files.</param>
public sealed record ContentPackStamp(string Id, string Version, string Fingerprint);
