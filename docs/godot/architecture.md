---
title: "Compose a Godot host"
description: "Start services in the scene tree and stop native components in order."
---

# Compose a Godot host {#overview}

Create a host dispatcher on the main thread once the scene tree is ready. Use `AbstractArchitecture` when the architecture should attach to the tree. Register services in `InstallModules` and use `AddModule` for modules. Record registrations during configuration and mount the nodes you need after the host starts.

Give views their specific dependencies. On exit, await shutdown of scene, UI, pause, and other registered components before freeing the roots and dispatcher they use.

<<< ../../Samples/GodotIntegration/GettingStarted/QuickStartArchitecture.cs{csharp}

Continue with the [integration tutorial](../tutorials/godot-integration.md).
