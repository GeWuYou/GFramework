---
title: "Application logging"
description: "Inject standard .NET loggers into game services."
---

# Application logging {#overview}

Configure `Microsoft.Extensions.Logging` in the composition root and have services accept `ILogger<T>` through their constructors. Preserve event fields with structured parameters, for example `logger.LogInformation("Player matched: {PlayerId}", playerId)`.

[MinimalApp](../getting-started/quick-start.md) shows console logging and per-instance Architecture logging. See [Godot logging](../godot/logging.md) for engine output.
