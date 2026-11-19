---
title: "Run the Godot runtime sample"
description: "Try startup, pause and settings, and resuming after restart."
---

# Run the Godot runtime sample {#overview}

Build and open the project from the repository root:

```bash
dotnet build Samples/GodotIntegration/GodotIntegration.csproj -c Debug
godot --editor --path Samples/GodotIntegration
```

Run the main scene with the matching Godot .NET version. Start a new game, move with WASD, and press Escape to open the pause menu. Change a binding in Settings, save and return to the menu, then restart and choose Continue.

## Command-line smoke test {#smoke}

```bash
godot --headless --path Samples/GodotIntegration -- --smoke
```

To verify loading after restart, run `--smoke-continue=<id>` with the identifier printed by the first run. To set up a single page and dialog, use the [minimal Godot scene](../tutorials/godot-integration.md).
