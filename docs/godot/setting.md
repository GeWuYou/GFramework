---
title: "Apply Godot settings"
description: "Apply audio, graphics, and language preferences on the engine thread."
---

# Apply Godot settings {#overview}

Create a [settings model](../game/setting.md) and a location provider. Use the host dispatcher when creating the model and `SettingsSystem`. Register `GodotAudioSettings`, `GodotGraphicsSettings`, or `GodotLocalizationSettings` as needed.

Wait for the model to load before applying values to the engine. Update the data from settings UI and save when the player confirms. Verify the audio buses, window configuration, and localization service used by the project.

See the [pause and settings example](../tutorials/godot-complete-project.md#pause-settings).
