# GFramework Config Tool

VS Code extension for the GFramework AI-First config workflow.

## Current MVP

- Browse config files from the workspace `config/` directory
- Open raw YAML files
- Open matching schema files from `schemas/`
- Localize extension UI text in English and Simplified Chinese, including the form preview, prompts, and notifications
- Run lightweight schema validation for nested required fields, unknown nested fields, scalar types, scalar arrays, and
  arrays of objects
- Open a lightweight form preview for nested object fields, object arrays, top-level scalar fields, and scalar arrays
- Render existing YAML comments in the form preview and edit per-field YAML comments directly from the form
- Jump from reference fields to the referenced schema, config domain, or direct config file when a reference value is
  present
- Initialize empty config files from schema-derived example YAML
- Batch edit one config domain across multiple files for top-level scalar and scalar-array fields
- Surface schema metadata such as `title`, `description`, `default`, `enum`, and `x-gframework-ref-table` in the
  lightweight editors

## Validation Coverage

The extension currently validates the repository's minimal config-schema subset:

- required properties in nested objects
- unknown properties in nested objects
- scalar compatibility for `integer`, `number`, `boolean`, and `string`
- scalar arrays with scalar item type checks
- arrays of objects whose items use the same supported subset recursively
- scalar `enum` constraints and scalar-array item `enum` constraints

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

## Current Constraints

- Multi-root workspaces use the first workspace folder
- Validation only covers a minimal subset of JSON Schema
- Form preview supports object-array editing, but nested object arrays inside array items still fall back to raw YAML
- Batch editing remains limited to top-level scalar fields and top-level scalar arrays

## Workspace Settings

- `gframeworkConfig.configPath`
- `gframeworkConfig.schemasPath`
