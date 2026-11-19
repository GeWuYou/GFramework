---
title: "Domain state"
description: "Keep game state and its rules in the object that owns them."
---

# Domain state {#overview}

Start a run, award points, and end the run through domain methods. Give callers read-only properties or snapshots. Views read presentation state, and save or restore operations map snapshots to save DTOs.

<<< ../../Samples/DodgeTheCreeps.Advanced.Core/Gameplay/GameSession.cs
