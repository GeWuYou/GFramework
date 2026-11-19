---
title: "Godot storage paths"
description: "Choose paths for exported resources and player files."
---

# Godot storage paths {#overview}

Use `GodotFileStorage` to connect game repositories to Godot paths. Use `res://` for bundled resources and `user://` for settings, progress, and backups. Give subsystems relative keys or assign each a prefix with `ScopedStorage`.

Use the [versioned save repository](../game/data.md) for slots. If reading or writing fails, check the key, the path root, and the export preset.
