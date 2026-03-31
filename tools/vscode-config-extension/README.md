# GFramework Config Tools

Minimal VS Code extension scaffold for the GFramework AI-First config workflow.

## Current MVP

- Browse config files from the workspace `config/` directory
- Open raw YAML files
- Open matching schema files from `schemas/`
- Run lightweight schema validation for required fields, unknown top-level fields, scalar types, and scalar array items
- Open a lightweight form preview for top-level scalar fields and top-level scalar arrays

## Validation Coverage

The extension currently validates the repository's minimal config-schema subset:

- required top-level properties
- unknown top-level properties
- scalar compatibility for `integer`, `number`, `boolean`, and `string`
- top-level scalar arrays with scalar item type checks

Nested objects and complex arrays should still be reviewed in raw YAML.

## Local Testing

```bash
cd tools/vscode-config-extension
node --test ./test/*.test.js
```

## Current Constraints

- Multi-root workspaces use the first workspace folder
- Validation only covers a minimal subset of JSON Schema
- Form editing currently supports top-level scalar fields and top-level scalar arrays
- Nested objects and complex arrays should still be edited in raw YAML

## Workspace Settings

- `gframeworkConfig.configPath`
- `gframeworkConfig.schemasPath`
