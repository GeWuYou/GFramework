---
title: "Development environment"
description: "Set up the repository SDK and build the source and documentation."
---

# Development environment {#overview}

Use the [repository SDK](../getting-started/installation.md#requirements), Git, Bash, and Python 3. Use Bun to preview the docs and the matching Godot .NET editor to run Godot projects.

```bash
dotnet build GFramework.sln -c Release
dotnet test GFramework.sln -c Release
```

Run documentation commands from the repository's `docs/` directory:

```bash
bun install --frozen-lockfile
bun run build
bun run dev
```

Preview the corresponding English and Simplified Chinese pages, check links in both languages, and verify examples against the source.
