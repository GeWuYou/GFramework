---
title: "Organize project dependencies"
description: "Give domain logic, use cases, and host code explicit boundaries."
---

# Organize project dependencies {#overview}

| Layer | Example responsibility |
| --- | --- |
| Domain | Run state, scoring, and invariants |
| Application | Start, resume, save, and exit |
| Host adapters | Scene tree, files, and rendering |
| Composition root | Build dependencies, register services, start, and stop |

Pass dependencies to use cases through their constructors. Define interfaces where the application crosses a real host boundary. Add rules and adapters under the feature they serve. See the [advanced Godot project](../tutorials/godot-advanced-project.md) for a runnable structure.
