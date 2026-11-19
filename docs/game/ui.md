---
title: "UI routes and dialogs"
description: "Open pages, windows, and dialogs with typed arguments."
---

# UI routes and dialogs {#overview}

## Define routes and register views {#routes}

Use `UiRoute<TArgs>` for pages and windows and `UiDialogRoute<TArgs, TResult>` for dialogs that return a result. Set layering, modality, world input blocking, and pause behavior in `UiRouteOptions`. Register routes and view factories in `UiRegistry`.

<<< ../../Samples/DodgeTheCreeps/DodgeRoutes.cs{csharp}

## Open, close, and dispatch input {#navigation}

Give `UiService` the registry, an `IUiHost`, and an `IGameDispatcher`. Open a route with its arguments. A view gets its current handle or closes itself through `UiViewContext`. Use the blocking flags in `Snapshot` to gate world input, and send semantic actions through `DispatchActionAsync`.

## Release views {#shutdown}

Have the factory supply a view lease that owns cleanup. Start follow-up navigation outside lifecycle callbacks. On exit, await `StopAsync` before releasing the mount root, dispatcher, and pause service. See [mount native UI](../godot/ui.md) for a Godot example.
