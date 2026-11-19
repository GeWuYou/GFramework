---
title: "Log to the Godot console"
description: "Connect standard .NET logging to Godot."
---

# Log to the Godot console {#overview}

Use `GodotLog.Provider` in the host's `ILoggerFactory` and inject `ILogger<T>` into application services. Give each architecture its own logging configuration. Configure any additional output providers in the same factory.

Trigger an informational message and an error in the editor and check that the level, category, and exception appear in the expected console. See [application logging](../core/logging.md).
