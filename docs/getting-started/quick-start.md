---
title: "Quickstart"
description: "Run the minimal C# example and see standard DI and Architecture in use."
---

# Quickstart {#overview}

Run this command from the repository root. See [requirements](../getting-started/installation.md#requirements) for the SDK.

```bash
dotnet run --project Samples/MinimalApp -c Release
```

The output shows a greeting from a standalone service, followed by the architecture startup and sample order processing.

## Application entry point {#program}

<<< ../../Samples/MinimalApp/Program.cs

## Register application services {#services}

`OrderArchitecture` registers services in `ConfigureServices`. The host calls services after startup and awaits `DestroyAsync` on exit.

<<< ../../Samples/MinimalApp/OrderArchitecture.cs

For a new project, follow the [installation guide](../getting-started/installation.md) to add Core, then register your application classes and their constructor dependencies in the composition root. For an engine project, [run the Godot example](../tutorials/godot-integration.md) next.
