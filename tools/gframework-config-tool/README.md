# GFramework Config Tool

VS Code extension for the GFramework AI-First config workflow.

## Purpose

This extension is the editor-side companion for the `GFramework.Game` config pipeline:

- `config/**/*.yaml`
- `schemas/**/*.schema.json`
- source-generated config types and tables

It is intended to speed up browsing, validation, lightweight form editing, and domain-level maintenance inside VS Code.
It is not a replacement for the runtime or generator packages, and it does not attempt to be a full JSON Schema editor.

## Recommended Workspace Layout

By default, the extension expects:

```text
GameProject/
├─ config/
│  ├─ monster/
│  │  ├─ slime.yaml
│  │  └─ goblin.yaml
│  └─ item/
│     └─ potion.yaml
└─ schemas/
   ├─ monster.schema.json
   └─ item.schema.json
```

## What It Adds

### Explorer View

- Browse config files from the workspace `config/` directory
- Group files by config domain
- Open matching schema files from `schemas/`

### File-Level Actions

- Open raw YAML
- Open the matching schema
- Open a lightweight form preview

### Domain-Level Actions

- Batch edit one config domain across multiple files for top-level scalar and scalar-array fields
- Run validation across the current workspace config surface

### Form / Validation Support

- Localize extension UI text in English and Simplified Chinese, including the form preview, prompts, and notifications
- Render existing YAML comments in the form preview and edit per-field YAML comments directly from the form
- Jump from reference fields to the referenced schema, config domain, or direct config file when a reference value is
  present
- Initialize empty config files from schema-derived example YAML
- Surface schema metadata such as `title`, `description`, `default`, `enum`, and `x-gframework-ref-table` in the
  lightweight editors

## Validation Coverage

The extension currently validates the repository's current schema subset:

- required properties in nested objects
- unknown properties in nested objects
- scalar compatibility for `integer`, `number`, `boolean`, and `string`
- scalar arrays with scalar item type checks
- arrays of objects whose items use the same supported subset recursively
- scalar `enum` constraints and scalar-array item `enum` constraints

## Workspace Settings

```json
{
  "gframeworkConfig.configPath": "config",
  "gframeworkConfig.schemasPath": "schemas"
}
```

## Quick Start

1. Install the extension in VS Code and open the workspace that contains your `config/` and `schemas/` directories.
2. Keep the default workspace layout, or set `gframeworkConfig.configPath` and `gframeworkConfig.schemasPath` to your
   project-specific paths.
3. Open the `GFramework Config` explorer view and select a config file or domain.
4. Run validation first to confirm the current YAML files still match the supported schema subset.
5. Open the lightweight form preview or domain batch editing actions, then fall back to raw YAML for deeper nested edits
   when needed.

## Documentation

- Chinese adoption guide: [Game 配置工具](../../docs/zh-CN/game/config-tool.md)
- Related config runtime guide: [Game 配置系统](../../docs/zh-CN/game/config-system.md)

## Current Constraints

- Multi-root workspaces use the first workspace folder
- Validation only covers the repository's current schema subset
- Form preview supports object-array editing, but nested object arrays inside array items still fall back to raw YAML
- Batch editing remains limited to top-level scalar fields and top-level scalar arrays

## Local Testing

```bash
cd tools/gframework-config-tool
bun install
bun run test
```

## Packaging And Publishing

```bash
cd tools/gframework-config-tool
bun install
bun run package:vsix
VSCE_PAT=your_marketplace_pat bun run publish:marketplace
```
