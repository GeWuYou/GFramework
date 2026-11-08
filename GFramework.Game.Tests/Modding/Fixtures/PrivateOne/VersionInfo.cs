// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace FixturePrivate;

/// <summary>Private dependency intentionally built twice with identical assembly and type names.</summary>
public static class VersionInfo
{
    /// <summary>Gets this package's private dependency value.</summary>
    public static int Value => 1;
}
