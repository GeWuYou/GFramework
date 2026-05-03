// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Carries the already-resolved values that <see cref="GodotLogTemplate"/> needs to render one log line.
/// </summary>
/// <param name="Timestamp">The UTC timestamp assigned to the log entry.</param>
/// <param name="Level">The severity level used for filtering, formatting, and Godot debug routing.</param>
/// <param name="Category">The source logger category name.</param>
/// <param name="Message">The formatted log message body.</param>
/// <param name="Color">The Godot BBCode color name resolved for <paramref name="Level"/>.</param>
/// <param name="Properties">The preformatted structured property suffix, or an empty string when none exist.</param>
internal readonly record struct GodotLogRenderContext(
    DateTime Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    string Color,
    string Properties);
