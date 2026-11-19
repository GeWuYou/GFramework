---
title: "Manage resources between scenes"
description: "Own leases by consumer and verify release across transitions."
---

# Manage resources between scenes {#overview}

Have the scene owner acquire resources with `AcquireAsync<T>`. If the next scene needs the same resource, call `Retain()` for its own lease before transitioning. Release the old lease on exit and release the remaining lease when the new scene exits.

Check the loader call count, lease state after cancellation, and final unload. See [Core resource leases](../core/resource.md) for setup and [Godot resources](../godot/resource.md) for indexing project assets.
