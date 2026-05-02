using System;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

internal readonly record struct GodotLogRenderContext(
    DateTime Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    string Color,
    string Properties);
