# Evidence And `ai-libs`

The evidence order is fixed:

1. source code, XML docs, `*.csproj`
2. tests and snapshots
3. module README
4. current `docs/zh-CN`
5. `ai-libs/`
6. archived docs

## When To Use `ai-libs`

Use `ai-libs/` to answer questions like:

- How is this extension point wired in a real project?
- What does the minimal project layout look like?
- Which project-side files need to exist for this module to work end to end?

## When Not To Use `ai-libs`

Do not use `ai-libs/` as the primary source for:

- public API semantics
- exact generator output guarantees
- supported package matrix
- diagnostics behavior

## Conflict Rule

If `ai-libs/` drifts from the current repo:

- trust source and tests
- mention the drift as a compatibility or migration note
- do not document old consumer behavior as if it were still the contract
