---
title: "JSON serialization"
description: "Use the default JSON serializer with storage and data services."
---

# JSON serialization {#overview}

`GFramework.Game.Serializer.JsonSerializer` uses Newtonsoft.Json. Use `ISerializer` for statically known types and `IRuntimeTypeSerializer` when the type is known only at runtime. Set a consistent serialization policy for storage and repositories in the composition root.

For saves, the [save repository](../game/data.md) manages the versioned document. See [file storage](../game/storage.md) for a small read and write example.
