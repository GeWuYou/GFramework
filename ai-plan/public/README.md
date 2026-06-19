# AI-Plan Public Index

`ai-plan/public/README.md` is the shared startup index for `boot`. It should stay short, list only active topics, and
help the current worktree land on the right recovery documents without scanning every public artifact.

## Boot Rules

1. Read this file before scanning `ai-plan/public/<topic>/`.
2. If the current branch or worktree appears in the map below, read the listed topics in priority order.
3. If there is no match, fall back to scanning active topic directories.
4. Ignore `ai-plan/public/archive/**` by default unless the user explicitly asks for historical context.

## Active Topics

- `ai-first-config-system`
  - Purpose: continue the AI-First config runtime, generator, and consumer DX work for `GFramework.Game`.
  - Tracking: `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-tracking.md`
  - Trace: `ai-plan/public/ai-first-config-system/traces/ai-first-config-system-trace.md`
- `documentation-full-coverage-governance`
  - Purpose: govern full-coverage documentation inventory, module-wave remediation, and the README / docs / XML /
    API-reference alignment baseline.
  - Tracking: `ai-plan/public/documentation-full-coverage-governance/todos/documentation-full-coverage-governance-tracking.md`
  - Trace: `ai-plan/public/documentation-full-coverage-governance/traces/documentation-full-coverage-governance-trace.md`
- `ai-plan-governance`
  - Purpose: govern repository AI workflow rules, recovery entrypoints, and multi-agent coordination conventions.
  - Tracking: `ai-plan/public/ai-plan-governance/todos/ai-plan-governance-tracking.md`
  - Trace: `ai-plan/public/ai-plan-governance/traces/ai-plan-governance-trace.md`
- `coroutine-optimization`
  - Purpose: continue the coroutine semantics, host integration, observability, regression coverage, and migration-doc
    follow-up work.
  - Tracking: `ai-plan/public/coroutine-optimization/todos/coroutine-optimization-tracking.md`
  - Trace: `ai-plan/public/coroutine-optimization/traces/coroutine-optimization-trace.md`
- `cqrs-rewrite`
  - Purpose: continue the CQRS migration, registry hardening, and related PR follow-up.
  - Tracking: `ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md`
  - Trace: `ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md`
- `data-repository-persistence`
  - Purpose: continue the data repository persistence hardening plus the settings / serialization follow-up backlog.
  - Tracking: `ai-plan/public/data-repository-persistence/todos/data-repository-persistence-tracking.md`
  - Trace: `ai-plan/public/data-repository-persistence/traces/data-repository-persistence-trace.md`
- `input-system-godot-integration`
  - Purpose: establish the shared input abstraction, default binding runtime, and Godot InputMap integration path.
  - Tracking: `ai-plan/public/input-system-godot-integration/todos/input-system-godot-integration-tracking.md`
  - Trace: `ai-plan/public/input-system-godot-integration/traces/input-system-godot-integration-trace.md`
- `godot-cqrs-main-thread-guard`
  - Purpose: triage the active PR review for Godot main-thread CQRS guard work and keep the minimal recovery entrypoint for documentation, guard-message, and regression-test fixes.
  - Tracking: `ai-plan/public/godot-cqrs-main-thread-guard/todos/godot-cqrs-main-thread-guard-tracking.md`
  - Trace: `ai-plan/public/godot-cqrs-main-thread-guard/traces/godot-cqrs-main-thread-guard-trace.md`

## Worktree To Active Topic Map

- Branch: `feat/ai-first-config`
  - Worktree hint: `GFramework-Ai-First-Config`
  - Priority 1: `ai-first-config-system`
- Branch: `feat/cqrs-optimization`
  - Worktree hint: `GFramework-cqrs`
  - Priority 1: `ai-plan-governance`
  - Priority 2: `cqrs-rewrite`
- Branch: `feat/coroutine-optimization`
  - Worktree hint: `GFramework-coroutine-optimization`
  - Priority 1: `coroutine-optimization`
  - Priority 2: `ai-plan-governance`
- Branch: `feat/data-repository-persistence`
  - Worktree hint: `GFramework-data-repository-persistence`
  - Priority 1: `data-repository-persistence`
- Branch: `feat/input-system-godot-integration`
  - Worktree hint: `GFramework-input-system-godot-integration`
  - Priority 1: `input-system-godot-integration`
- Branch: `fix/godot-cqrs-main-thread-guard`
  - Worktree hint: `GFramework`
  - Priority 1: `godot-cqrs-main-thread-guard`
- Branch: `docs/sdk-update-documentation`
  - Worktree hint: `GFramework-update-documentation`
  - Priority 1: `documentation-full-coverage-governance`
