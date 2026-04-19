# AI-Plan Public Index

`ai-plan/public/README.md` is the shared startup index for `boot`. It should stay short, list only active topics, and
help the current worktree land on the right recovery documents without scanning every public artifact.

## Boot Rules

1. Read this file before scanning `ai-plan/public/<topic>/`.
2. If the current branch or worktree appears in the map below, read the listed topics in priority order.
3. If there is no match, fall back to scanning active topic directories.
4. Ignore `ai-plan/public/archive/**` by default unless the user explicitly asks for historical context.

## Active Topics

- `analyzer-warning-reduction`
  - Purpose: track the analyzer warning reduction branch, including the current recovery point, remaining warning
    hotspots, and the next safe warning-reduction slice.
  - Tracking: `ai-plan/public/analyzer-warning-reduction/todos/analyzer-warning-reduction-tracking.md`
  - Trace: `ai-plan/public/analyzer-warning-reduction/traces/analyzer-warning-reduction-trace.md`
- `ai-plan-governance`
  - Purpose: govern the `ai-plan/` directory model, startup index, and archive policy.
  - Tracking: `ai-plan/public/ai-plan-governance/todos/ai-plan-governance-tracking.md`
  - Trace: `ai-plan/public/ai-plan-governance/traces/ai-plan-governance-trace.md`
- `ai-first-config-system`
  - Purpose: continue the AI-First config runtime, generator, and consumer DX work for `GFramework.Game`.
  - Tracking: `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-tracking.md`
  - Trace: `ai-plan/public/ai-first-config-system/traces/ai-first-config-system-trace.md`
- `cqrs-rewrite`
  - Purpose: continue the CQRS migration, registry hardening, and related PR follow-up.
  - Tracking: `ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md`
  - Trace: `ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md`

## Worktree To Active Topic Map

- Branch: `fix/analyzer-warning-reduction-batch`
  - Worktree hint: `GFramework-analyzer`
  - Priority 1: `analyzer-warning-reduction`
- Branch: `feat/ai-first-config`
  - Worktree hint: `GFramework-Ai-First-Config`
  - Priority 1: `ai-first-config-system`
- Branch: `feat/cqrs-optimization`
  - Worktree hint: `GFramework-cqrs`
  - Priority 1: `ai-plan-governance`
  - Priority 2: `cqrs-rewrite`

## Archived Topics

- `cqrs-cache-docs-hardening`
  - Archive root: `ai-plan/public/archive/cqrs-cache-docs-hardening/`
  - Note: archived topics stay outside the default `boot` context until a user explicitly requests historical review.
