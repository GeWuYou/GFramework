---
title: "VS Code configuration tool"
description: "Browse, edit, and validate GFramework game configuration in VS Code."
---

# VS Code configuration tool {#overview}

1. Install [GFramework Config Tool](https://marketplace.visualstudio.com/items?itemName=GeWuYou.gframework-config-tool) and open the project root containing `config/` and `schemas/` in VS Code.
2. Open a configuration domain from the tree. Browse its records or open the schema to check field rules.
3. Use the form for fields it presents, or open YAML directly for nested values. Use batch editing for top-level scalars or scalar arrays within one domain.
4. Save, check diagnostics for fields and references, and run the project's load flow to validate the result.

See [typed configuration](../game/config-system.md) for the runtime layout and schema.
