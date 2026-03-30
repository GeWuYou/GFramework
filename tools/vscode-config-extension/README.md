# GFramework Config Tools

Minimal VS Code extension scaffold for the GFramework AI-First config workflow.

## Current MVP

- Browse config files from the workspace `config/` directory
- Open raw YAML files
- Open matching schema files from `schemas/`
- Run lightweight schema validation for required fields and simple scalar types
- Open a lightweight form preview for top-level scalar fields

## Current Constraints

- Multi-root workspaces use the first workspace folder
- Validation only covers a minimal subset of JSON Schema
- Form editing currently supports top-level scalar fields only
- Arrays and nested objects should still be edited in raw YAML

## Workspace Settings

- `gframeworkConfig.configPath`
- `gframeworkConfig.schemasPath`
