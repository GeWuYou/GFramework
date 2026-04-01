# GFramework Config Tools

Minimal VS Code extension scaffold for the GFramework AI-First config workflow.

## Current MVP

- Browse config files from the workspace `config/` directory
- Open raw YAML files
- Open matching schema files from `schemas/`
- Run lightweight schema validation for nested required fields, unknown nested fields, scalar types, scalar arrays, and
  arrays of objects
- Open a lightweight form preview for nested object fields, top-level scalar fields, and scalar arrays
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

Object-array editing should still be reviewed in raw YAML.

## Local Testing

```bash
cd tools/vscode-config-extension
node --test ./test/*.test.js
```

## Current Constraints

- Multi-root workspaces use the first workspace folder
- Validation only covers a minimal subset of JSON Schema
- Form preview supports nested objects and scalar arrays, but object arrays remain raw-YAML-only for edits
- Batch editing remains limited to top-level scalar fields and top-level scalar arrays

## Workspace Settings

- `gframeworkConfig.configPath`
- `gframeworkConfig.schemasPath`
