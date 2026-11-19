---
title: "Player settings"
description: "Load, apply, and save player preferences such as audio and interface options."
---

# Player settings {#overview}

1. Register setting types, locations, applicators, and migrations with `SettingsModel<TRepository>`.
2. Get the model-owned data through `GetData<T>` and give the applicator the same instance.
3. Await `InitializeAsync` to load stored data, then apply it through `SettingsSystem.ApplyAllAsync`.
4. Call `SaveAllAsync` when the player confirms. Keep the existing file if loading fails and report an actionable error.

Use `UnifiedSettingsDataRepository` to put multiple sections in one file. Use the host `IGameDispatcher` for applicators that require thread dispatch. See [Godot settings](../godot/setting.md).
