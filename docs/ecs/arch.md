---
title: "Run Arch ECS systems"
description: "Set up a World, drive native systems, and check the sample result."
---

# Run Arch ECS systems {#overview}

## Direct construction {#direct}

<<< ../../Samples/ArchRuntimeDemo/Program.cs#native-runtime

```bash
dotnet run --project Samples/ArchRuntimeDemo -c Release
```

`ARCH_RUNTIME_PASS` confirms that the native system ran and the entity moved as expected. The runtime borrows the supplied World and systems. Stop the runtime, then dispose the systems and finally the World.

## Use DI {#di}

Call `AddArch` in the composition root to configure the runtime and options, then register systems. Call `Update` with the host loop's actual timestep. Enable `EnableStatistics` for per-instance metrics and measure the cost in your own workload.
