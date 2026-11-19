---
title: "Declare service and node dependencies"
description: "Use constructor injection and explicit host composition."
---

# Declare service and node dependencies {#overview}

Declare long-lived service dependencies in constructors and pass operation-specific data through method parameters. Resolve `Architecture.Services` in the composition root, then pass the required interfaces to application services and views.

For nodes created by Godot, have the owning scene call an initialization method with the state and actions the node needs once its services are ready. See the [advanced Godot example](../tutorials/godot-advanced-project.md).
