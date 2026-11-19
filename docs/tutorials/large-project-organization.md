---
title: "Organize a game project by feature"
description: "Give use cases, domain state, and native views clear composition boundaries."
---

# Organize a game project by feature {#overview}

Organize code by features such as Gameplay, Progress, and Settings. Put rules that need independent tests in a plain C# project. Implement adapters in the Godot project and connect them in the composition root. Use parameters and interfaces to show where data comes from and who receives the result.

See the [advanced Godot example](../tutorials/godot-advanced-project.md) and [dependency organization](../best-practices/architecture-patterns.md).
