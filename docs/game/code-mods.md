---
title: "Code mods and Harmony"
description: "Load trusted code mods into a desktop .NET game."
---

# Code mods and Harmony {#overview}

## Write a mod {#authoring}

Put the game's extension interfaces in a shared API assembly. Reference a compatible version from the mod and declare the DLL, entry type, and API version range in the `code` block of `content-pack.json`. The entry type is a public, concrete `IGameMod` implementation with a parameterless constructor. Register gameplay extensions in lifecycle methods.

## Start a trusted session {#session}

1. Resolve a content pack plan and set `CodeApiVersion`.
2. Tell the player the DLL will run with the game's process permissions. Store approval for the package version and file fingerprint.
3. Call `GameModHost.PrepareAsync` with shared contract assemblies, services, the dispatcher, and a `TrustPolicy`.
4. Pass `modHost.Admission` to `ContentCatalogLoader.LoadAsync`. Prepare resources, then call `StartAsync`.
5. Await disposal of the mod host at the end of the game session. Restart the game after changing enabled mods, versions, or ordering.

To patch methods, install `GeWuYou.GFramework.Game.Harmony` in the host and configure `HarmonyModExtension`. Manage patches and their removal using the mod's own owner. For gameplay the public API exposes, use its interfaces directly.

See the [Dodge the Creeps mod contracts](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps.ModApi).
