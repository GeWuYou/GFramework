---
title: "Configuration generator reference"
description: "Find generator inputs, outputs, and shared schema constraints."
---

# Configuration generator reference {#overview}

## Inputs and outputs {#inputs}

Install `GeWuYou.GFramework.Game.SourceGenerators` at the same version as Game in the consuming project. Include `schemas/**/*.schema.json` in the build inputs. Each schema produces typed configuration, a table wrapper, and registration helpers. `GeneratedConfigCatalog` lists the project's generated tables. Use `RegisterAllGeneratedConfigTables` before the initial load.

## Schema keywords {#keywords}

| Data | Example supported constraints |
| --- | --- |
| Value | `enum`, `const`, `not` |
| Number | `minimum`, `maximum`, `exclusiveMinimum`, `exclusiveMaximum`, `multipleOf` |
| String | `minLength`, `maxLength`, `pattern`, `format` |
| Array | One object-valued `items` schema, `minItems`, `maxItems`, `uniqueItems`, `contains` |
| Object | `additionalProperties: false`, `required`, `minProperties`, `maxProperties`, `dependentRequired`, `dependentSchemas` |
| Object composition | `allOf`, object conditions with `if` / `then` / `else` |

Constrain dates, times, durations, email addresses, URIs, and UUIDs using their supported `format` values. Use explicit object properties and conditions for variants so the generated CLR type has a defined shape. When the build reports a diagnostic, address the schema path it identifies and rebuild.

See [typed game configuration](../game/config-system.md) for the directory layout and YAML example.
