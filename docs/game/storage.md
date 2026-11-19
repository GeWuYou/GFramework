---
title: "File storage"
description: "Map application keys to files and scope data by subsystem."
---

# File storage {#overview}

```csharp
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

using var files = new FileStorage("GameData", new JsonSerializer(), ".json");
var profile = new ScopedStorage(files, "profile");

await profile.WriteAsync("score", 42);
int score = await profile.ReadAsync<int>("score");
```

`FileStorage` uses relative keys under a root directory. Define keys as application identifiers and use `ScopedStorage` to assign a prefix to each subsystem sharing the store. The creator disposes the underlying storage.

For game saves and settings, choose a [repository](../game/data.md) over storage. See [Godot storage](../godot/storage.md) for native path mapping.
