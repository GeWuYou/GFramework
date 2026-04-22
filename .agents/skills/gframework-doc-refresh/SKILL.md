---
name: gframework-doc-refresh
description: "Refresh or reassess GFramework documentation for a source module such as Core, Game, Godot, Cqrs, Ecs.Arch, or their generator/abstraction packages. Use this when the user asks to update module docs, re-evaluate landing pages, fix outdated topic pages, refresh API reference coverage, verify adoption paths against source/tests/README, or compare current docs with ai-libs consumer wiring. Recommended command: /gframework-doc-refresh <module>."
---

# Purpose

Use this skill to refresh GFramework documentation from source-first evidence.

The public entry is module-driven, not doc-type-driven:

- Input: a source module or a resolvable docs section alias
- Output: the minimal documentation update set needed for that module
- Evidence: code, tests, README, current docs, then `ai-libs/`

Do not start by deciding “this is an API doc task” or “this is a tutorial task”.
Decide that only after the module scan.

# Triggers

Use this skill when the user asks things like:

- `refresh docs for Core`
- `update Game module docs`
- `根据 Godot 模块源码刷新文档`
- `重新评估 Cqrs 模块文档并更新`
- `核对 Godot.SourceGenerators 的文档状态`
- `看看 source-generators 栏目哪些页面已经失真`

Recommended command form:

```bash
/gframework-doc-refresh <module>
```

# Supported Modules

Canonical module names:

- `Core`
- `Core.Abstractions`
- `Core.SourceGenerators`
- `Core.SourceGenerators.Abstractions`
- `Game`
- `Game.Abstractions`
- `Game.SourceGenerators`
- `Godot`
- `Godot.SourceGenerators`
- `Godot.SourceGenerators.Abstractions`
- `Cqrs`
- `Cqrs.Abstractions`
- `Cqrs.SourceGenerators`
- `Ecs.Arch`
- `Ecs.Arch.Abstractions`
- `SourceGenerators.Common`

The canonical mapping lives in `.agents/skills/_shared/module-map.json`.

If the user supplies a docs section name:

- resolve it back to a source module first
- if it maps to multiple modules, stop at normalization guidance and do not draft docs yet

# Workflow

## 1. Normalize the input

Run:

```bash
python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py <module>
```

The script normalizes aliases, reports ambiguity, and prints the module's evidence surface.

If the result is ambiguous:

- return the candidate modules
- ask the user to pick the intended source module
- do not continue into document generation

## 2. Scan the evidence surface

For the resolved module, inspect:

- source directories
- `*.csproj`
- relevant test projects
- sibling `README.md`
- mapped `docs/zh-CN` landing pages and topic pages
- optional `ai-libs/` consumer evidence when needed

Always confirm the actual files in the repository.
Do not assume the mapping is enough on its own.

## 3. Decide whether `ai-libs/` is needed

Use `ai-libs/` when:

- adoption path is unclear from source and README alone
- extension points need a real consumer wiring example
- current docs have concepts but lack an end-to-end integration path

Do not rely on `ai-libs/` for:

- public API contract definitions
- generator diagnostics or semantic guarantees
- claims about what the current version officially supports

If `ai-libs/` conflicts with current source or tests, keep source/tests as the contract and document the migration boundary.

## 4. Judge the documentation state

Classify the module into one or more of these states:

- missing landing page
- stale landing page
- stale topic page
- missing or stale API reference coverage
- stale tutorial/example
- validation-only

Base this on evidence, not on the previous docs shape.

## 5. Choose the output set

Always prioritize:

1. README / landing page / adoption path
2. topic pages
3. API reference
4. tutorials

If the module only needs validation or relinking, do not generate extra pages.

## 6. Draft or update docs

Load only the template that matches the output you selected:

- `templates/module-landing.md`
- `templates/topic-refresh.md`
- `templates/api-reference.md`
- `templates/tutorial.md`

Keep examples minimal, current, and traceable to source or tests.

## 7. Validate

Run the internal validators as needed:

```bash
bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh <file-or-directory>
```

For site-level confirmation after doc edits:

```bash
cd docs && bun run build
```

# Evidence Order

Use this exact priority:

1. source code, XML docs, `*.csproj`
2. tests and snapshots
3. module `README.md`
4. current `docs/zh-CN` pages
5. verified `ai-libs/` consumers
6. archived docs only as fallback context

# Output Rules

- Prefer correcting the adoption path over expanding page count.
- Do not copy wording from outdated docs just to keep page volume.
- Escape generics outside code blocks.
- Keep internal links real and current.
- Mark code blocks with explicit languages.
- Use the smallest example that demonstrates the current contract.
- Consumer examples may align with `ai-libs/`, but must not exceed the current module contract.

# Validation

Use the shared standards in `.agents/skills/_shared/DOCUMENTATION_STANDARDS.md`.

When this skill changes public docs, prefer:

1. focused validator on touched pages
2. `cd docs && bun run build`

When this skill changes the skill system itself:

1. validate `SKILL.md` frontmatter exists
2. run the module scan script for representative modules
3. confirm obsolete `vitepress-*` public entries are gone

# References

Read these only when needed:

- `.agents/skills/_shared/DOCUMENTATION_STANDARDS.md`
- `.agents/skills/_shared/module-map.json`
- `references/module-selection.md`
- `references/evidence-and-ai-libs.md`
- `references/output-strategy.md`
