---
title: "Mount Godot UI"
description: "Open native Control scenes, pass arguments, and confirm dialog results."
---

# Mount Godot UI {#overview}

## Create the host {#host}

Add `GodotUiRoot` to the scene tree on the engine thread. Create `UiService` with the host's `IGameDispatcher`, and register routes and native view factories in `UiRegistry`. Share one pause manager with the UI if you use pause menus.

<<< ../../Samples/GodotIntegration/GettingStarted/QuickStartArchitecture.cs{csharp}

## Open a page or dialog {#navigation}

Build views from regular `Control` scenes. Pages receive arguments and actions and can close themselves through `UiViewContext`. A dialog uses its context to complete a typed result.

<<< ../../Samples/GodotIntegration/GettingStarted/QuickStartConfirm.cs{csharp}

## View lifecycle {#view-lifecycle}

<span id="可选生命周期"></span>

Implement `IGodotUiLifecycle` to prepare state in `OpenAsync` and release application subscriptions in `CloseAsync`. The view is mounted and hidden when it opens. Its lease handles node cleanup.

Use `GodotUiInputBridge` to dispatch semantic actions from Godot. Await UI shutdown before removing the mount root. See [the integration tutorial](../tutorials/godot-integration.md) for the full interaction.
