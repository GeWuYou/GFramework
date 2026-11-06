---
name: gframework-doc-refresh
description: Refresh GFramework module documentation from source and tests, resolve module aliases, and verify package adoption paths. Use for stale module pages, examples, API references or consumer wiring.
---

# GFramework Documentation Refresh

Follow [repository rules](../../../AGENTS.md). Input is a source module or docs-section alias; output is the smallest
documentation update that the evidence supports. Do not create pages merely to fill a template.

## Resolve and inspect

From the repository root:

```bash
python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py <module> --json
```

The [module map](../_shared/module-map.json) owns supported names, aliases and paths. For ambiguous or unknown input,
report candidates and resolve the intended module before drafting. See [module selection](references/module-selection.md)
when normalization needs explanation.

Inspect the reported source, project file, tests/snapshots, module README and affected docs. The scan reports path
presence, not semantic accuracy. Follow AGENTS.md's Documentation Source Of Truth rules; old pages are editable outputs.
Consult [consumer evidence](references/evidence-and-ai-libs.md) only when real adoption wiring is unclear.

## Update the necessary pages

Prioritize the module README, landing page and adoption path, then stale topics; add API references or tutorials only
when they help adoption. See [output strategy](references/output-strategy.md) for page-specific decisions.
Load only the relevant template: [landing](templates/module-landing.md), [topic](templates/topic-refresh.md),
[API](templates/api-reference.md) or [tutorial](templates/tutorial.md).

Follow AGENTS.md's reader-facing documentation boundaries and the [Markdown standards](../_shared/DOCUMENTATION_STANDARDS.md).
Keep examples traceable to current code/tests; consumer references cannot expand the framework contract.

## Validate and report

For changed public docs, use the focused validators and site build as appropriate:

```bash
bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh <file-or-directory>
cd docs && bun run build
```

Report the evidence used, changed pages, observed checks and remaining gaps. A path scan or Markdown validator does not
prove that an example compiles or an adoption workflow works. A review-only request remains read-only.
