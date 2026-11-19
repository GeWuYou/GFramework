---
title: "Load Godot resources"
description: "Access Godot resources by application key through a typed repository."
---

# Load Godot resources {#overview}

Use a `GodotResourceRepository<TKey, TResource>` for `Resource` assets. Give indexed resources stable application keys. Prepare the repository at a Godot stage that can access resources and pass it to consumers.

<<< ../../Samples/GodotIntegration/GettingStarted/QuickStartResources.cs{csharp}

Use the repository to index project assets. For acquisition and unloading across consumers, use [Core resource leases](../core/resource.md) with a suitable loader.
