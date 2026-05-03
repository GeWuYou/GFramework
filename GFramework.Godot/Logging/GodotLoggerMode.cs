// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Logging;

/// <summary>
///     Selects the Godot logger output behavior.
/// </summary>
public enum GodotLoggerMode
{
    /// <summary>
    ///     Uses rich BBCode console output and mirrors warnings/errors to the Godot debugger panel.
    /// </summary>
    Debug,

    /// <summary>
    ///     Uses plain console output without rich text or debugger panel mirroring.
    /// </summary>
    Release
}
