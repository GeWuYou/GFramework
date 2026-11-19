---
title: "Test game rules and use cases"
description: "Verify rules in plain C# tests and native wiring in Godot."
---

# Test game rules and use cases {#overview}

Assert observable outcomes from use cases. For example, simulate a persistence failure and check that the current run remains usable. For a valid snapshot, check that the world restoration port is called. Supply small, explicit test doubles as dependencies.

```bash
dotnet test Samples/DodgeTheCreeps.Advanced.Tests -c Release
```

Use the [Godot smoke test](../tutorials/godot-integration.md#smoke) for native nodes, signals, and file paths.
