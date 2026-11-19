---
title: "Host environment"
description: "Pass paths, platform information, and environment settings to the services that use them."
---

# Host environment {#overview}

In a .NET Host, get the environment and content root from `IHostEnvironment`. In Godot, resolve resource locations and the user data directory in the host, then pass paths or a dedicated path service to adapters.

Keep environment resolution in the composition root. See [application configuration](../core/configuration.md) and [Godot storage](../godot/storage.md).
