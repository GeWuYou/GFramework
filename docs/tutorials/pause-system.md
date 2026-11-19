---
title: "Implement nested pause menus"
description: "Verify that pause and settings hold independent pause requests."
---

# Implement nested pause menus {#overview}

1. Create one `PauseStackManager` in the host and register a `GodotPauseHandler`.
2. Set `PauseWhileVisible` for the pause and settings routes. Use the same manager to apply UI pause policy to the scene tree.
3. Start the game, open Pause, and then open Settings. Close Settings and check that gameplay stays paused.
4. Return to gameplay and check that it resumes. Stop the UI before disposing the pause service on exit.

<<< ../../Samples/DodgeTheCreeps/DodgeRoutes.cs{csharp}

See [Godot pause](../godot/pause.md) for host setup.
