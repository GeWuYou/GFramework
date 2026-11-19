---
title: "Generate Godot project metadata"
description: "Generate C# entry points for Autoload and InputMap from project.godot."
---

# Generate Godot project metadata {#overview}

Install `GeWuYou.GFramework.Godot.SourceGenerators`. Include the project-root `project.godot` as an `AdditionalFiles` input. The NuGet package adds the root file automatically. With a project reference, configure the input explicitly.

`[autoload]` generates `GFramework.Godot.Generated.AutoLoads` and `[input]` generates `GFramework.Godot.Generated.InputActions`. Use generated names in [input bindings](../godot/input.md). Rebuild after adding or renaming Godot actions.
