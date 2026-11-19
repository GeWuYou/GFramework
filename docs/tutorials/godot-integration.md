---
title: "Integrate Godot"
description: "Start services, open a page, and complete a dialog in a minimal Godot scene."
---

# Integrate Godot {#overview}

## Build and run {#run}

Use Godot 4.7.2 .NET and the repository SDK. Run from the repository root:

```bash
dotnet build Samples/GodotIntegration/GodotIntegration.csproj -c Debug
godot --headless --editor --path Samples/GodotIntegration --import
godot --path Samples/GodotIntegration res://GettingStarted/QuickStart.tscn
```

The window displays a resource title selected by key. Open the dialog and choose an action. The status shows `Confirmed` or `Cancelled`.

## Compose services and views {#composition}

Provide exported resources and views in the scene root. Register the resource component, native mount root, and typed routes in the composition root. After resource initialization, the page factory looks up its resource and configures the view.

<<< ../../Samples/GodotIntegration/GettingStarted/QuickStartArchitecture.cs{csharp}

## Return a result and close {#dialog}

Register the dialog factory with `RegisterGodotDialog` and pass actions and context to the view. Confirm completes the `bool` result and Close dismisses the current dialog. The host stops the UI before it quits the scene tree.

<<< ../../Samples/GodotIntegration/GettingStarted/QuickStartConfirm.cs{csharp}

## Smoke test {#smoke}

```bash
godot --headless --path Samples/GodotIntegration res://GettingStarted/QuickStart.tscn -- --quickstart-smoke
```

Check that opening, confirmation, dismissal, and shutdown finish. Next, [run the full game](../tutorials/godot-complete-project.md).
