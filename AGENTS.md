# AGENTS.md

This document is the single source of truth for coding behavior in this repository.

All AI agents and contributors must follow these rules when writing, reviewing, or modifying code in `GFramework`.

## Environment Capability Inventory

- Before choosing runtimes or CLI tools, read `@.ai/environment/tools.ai.yaml`.
- Use `@.ai/environment/tools.raw.yaml` only when you need the full collected facts behind the AI-facing hints.
- Prefer the project-relevant tools listed there instead of assuming every installed system tool is fair game.
- If the real environment differs from the inventory, use the project-relevant installed tool and report the mismatch.
- When working in WSL against this repository's Windows-backed worktree, first prefer Linux `git` with an explicit
  `--git-dir=<repo>/.git/worktrees/<worktree-name>` and `--work-tree=<worktree-root>` binding for every repository
  command. Treat that explicit binding as higher priority than `git.exe`, because it avoids WSL worktree path
  translation mistakes and still works in sessions where Windows `.exe` execution is unavailable.
- If a plain Linux `git` command in WSL fails with a worktree-style “not a git repository” path translation error,
  rerun it with the explicit `--git-dir` / `--work-tree` binding before trying `git.exe`.
- Only prefer Windows Git from WSL (for example `git.exe`) when that executable is both resolvable and executable in the
  current session, and when the explicit Linux `git` binding is unavailable or has already failed.
- If the shell resolves `git.exe` but the current WSL session cannot execute it cleanly (for example `Exec format
  error`), keep using the explicit Linux `git` binding for the rest of the task instead of retrying Windows Git.
- If the shell does not currently resolve `git.exe` to the host Windows Git installation and you still need Windows Git
  as a fallback, prepend that installation's command directory to `PATH` and reset shell command hashing for the
  current session before continuing.
- After resolving either strategy, prefer a session-local binding or command wrapper for subsequent Git commands so the
  shell does not silently fall back to the wrong repository context later in the same WSL session.

## Git Workflow Rules

- Every completed task MUST pass at least one build validation before it is considered done.
- If the task changes multiple projects or shared abstractions, prefer a solution-level or affected-project
  `dotnet build ... -c Release`; otherwise use the smallest build command that still proves the result compiles.
- If the required build passes and there are task-related staged or unstaged changes, contributors MUST create a Git
  commit automatically instead of leaving the task uncommitted, unless the user explicitly says not to commit.
- Commit messages MUST use Conventional Commits format: `<type>(<scope>): <summary>`.
- The commit `summary` MUST use simplified Chinese and briefly describe the main change.
- The commit `body` MUST use unordered list items, and each item MUST start with a verb such as `新增`、`修复`、`优化`、
  `更新`、`补充`、`重构`.
- Each commit body bullet MUST describe one independent change point; avoid repeated or redundant descriptions.
- Keep technical terms in English when they are established project terms, such as `API`、`Model`、`System`.
- When composing a multi-line commit body from shell commands, contributors MUST NOT rely on Bash `$"..."` quoting for
  newline escapes, because it passes literal `\n` sequences to Git. Use multiple `-m` flags or ANSI-C `$'...'`
  quoting so the commit body contains real line breaks.
- If a new task starts while the current branch is `main`, contributors MUST first try to update local `main` from the
  remote, then create and switch to a dedicated branch before making substantive changes.
- The branch naming rule for a new task branch is `<type>/<topic-or-scope>`, where `<type>` should match the intended
  Conventional Commit category as closely as practical.

## Repository Boot Skill

- The repository-maintained Codex boot skill lives at `.codex/skills/gframework-boot/`.
- Prefer invoking `$gframework-boot` when the user uses short startup prompts such as `boot`、`continue`、`next step`、
  `按 boot 开始`、`先看 AGENTS`、`继续当前任务`.
- The boot skill is a startup convenience layer, not a replacement for this document. If the skill and `AGENTS.md`
  diverge, follow `AGENTS.md` first and update the skill in the same change.
- The boot skill MUST read `AGENTS.md`、`.ai/environment/tools.ai.yaml`、`ai-plan/public/README.md` and the relevant
  active-topic `ai-plan/` artifacts before substantive execution.

## Subagent Usage Rules

- Use subagents only when the task is complex, the context is likely to grow too large, or the work can be split into
  independent parallel subtasks.
- The main agent MUST identify the critical path first. Do not delegate the immediate blocking task if the next local
  step depends on that result.
- Use `explorer` subagents for read-only discovery, comparison, tracing, and narrow codebase questions.
- Use `worker` subagents only for bounded implementation tasks with an explicit file or module ownership boundary.
- Every delegation MUST specify:
    - the concrete objective
    - the expected output format
    - the files or subsystem the subagent owns
    - any constraints about tests, diagnostics, or compatibility
- Subagents are not allowed to revert or overwrite unrelated changes from the user or other agents. They must adapt to
  concurrent work instead of assuming exclusive ownership of the repository.
- Prefer lightweight models such as `gpt-5.1-codex-mini` for narrow exploration, indexing, and comparison tasks.
- Prefer stronger models such as `gpt-5.4` for cross-module design work, non-trivial refactors, and tasks that require
  higher confidence reasoning.
- The main agent remains responsible for reviewing and integrating subagent output. Unreviewed subagent conclusions do
  not count as final results.

## Commenting Rules (MUST)

All generated or modified code MUST include clear and meaningful comments where required by the rules below.

### XML Documentation (Required)

- All public, protected, and internal types and members MUST include XML documentation comments (`///`).
- Use `<summary>`, `<param>`, `<returns>`, `<exception>`, and `<remarks>` where applicable.
- Comments must explain intent, contract, and usage constraints instead of restating syntax.
- If a member participates in lifecycle, threading, registration, or disposal behavior, document that behavior
  explicitly.

### Inline Comments

- Add inline comments for:
  - Non-trivial logic
  - Concurrency or threading behavior
  - Performance-sensitive paths
  - Workarounds, compatibility constraints, or edge cases
  - Registration order, lifecycle sequencing, or generated code assumptions
- Avoid obvious comments such as `// increment i`.

### Architecture-Level Comments

- Core framework components such as Architecture, Module, System, Context, Registry, Service Module, and Lifecycle types
  MUST include high-level explanations of:
  - Responsibilities
  - Lifecycle
  - Interaction with other components
  - Why the abstraction exists
  - When to use it instead of alternatives

### Source Generator Comments

- Generated logic and generator pipelines MUST explain:
  - What is generated
  - Why it is generated
  - The semantic assumptions the generator relies on
  - Any diagnostics or fallback behavior

### Complex Logic Requirement

- Methods with non-trivial logic MUST document:
  - The core idea
  - Key decisions
  - Edge case handling, if any

### Quality Rules

- Comments MUST NOT be trivial, redundant, or misleading.
- Prefer explaining `why` and `when`, not just `what`.
- Code should remain understandable without requiring external context.
- Prefer slightly more explanation over too little for framework code.

### Enforcement

- Missing required documentation is a coding standards violation.
- Code that does not meet the documentation rules is considered incomplete.

## Code Style

### Language and Project Settings

- Follow the repository defaults:
  - `ImplicitUsings` disabled
  - `Nullable` enabled
  - `GenerateDocumentationFile` enabled for shipped libraries
  - `LangVersion` is generally `preview` in the main libraries and abstractions
- Do not rely on implicit imports. Declare every required `using` explicitly.
- Write null-safe code that respects nullable annotations instead of suppressing warnings by default.

### Naming and Structure

- Use the namespace pattern `GFramework.{Module}.{Feature}` with PascalCase segments.
- Follow standard C# naming:
  - Types, methods, properties, events, and constants: PascalCase
  - Interfaces: `I` prefix
  - Parameters and locals: camelCase
  - Private fields: `_camelCase`
- Keep abstractions projects free of implementation details and engine-specific dependencies.
- Preserve existing module boundaries. Do not introduce new cross-module dependencies without clear architectural need.

### Formatting

- Use 4 spaces for indentation. Do not use tabs.
- Use Allman braces.
- Keep `using` directives at the top of the file and sort them consistently.
- Separate logical blocks with blank lines when it improves readability.
- Prefer one primary type per file unless the surrounding project already uses a different local pattern.
- Unless there is a clear and documented reason to keep a file large, keep a single source file under roughly 800-1000
  lines.
- If a file grows beyond that range, contributors MUST stop and check whether responsibilities should be split before
  continuing; treating oversized files as the default is considered a design smell.
- Keep line length readable. Around 120 characters is the preferred upper bound.

### C# Conventions

- Prefer explicit, readable code over clever shorthand in framework internals.
- Match existing async patterns and naming conventions (`Async` suffix for asynchronous methods).
- Avoid hidden side effects in property getters, constructors, and registration helpers.
- Preserve deterministic behavior in registries, lifecycle orchestration, and generated outputs.
- When adding analyzers or suppressions, keep them minimal and justify them in code comments if the reason is not
  obvious.

### Analyzer and Validation Expectations

- The repository uses `Meziantou.Analyzer`; treat analyzer feedback as part of the coding standard.
- Treat SonarQube maintainability rules as part of the coding standard as well, especially cognitive complexity and
  oversized parameter list findings.
- When a method approaches analyzer complexity limits, prefer extracting named helper methods by semantic phase
  (parsing, normalization, validation, diagnostics) instead of silencing the warning or doing cosmetic reshuffles.
- When a constructor or method exceeds parameter count limits, choose the refactor that matches the shape of the API:
  use domain-specific value objects or parameter objects for naturally grouped data, and prefer named factory methods
  when the call site is really selecting between different creation modes.
- Do not add suppressions for complexity or parameter-count findings unless the constraint is externally imposed and the
  reason is documented in code comments.
- Naming must remain compatible with `scripts/validate-csharp-naming.sh`.

## Testing Requirements

### Required Coverage

- Every non-trivial feature, bug fix, or behavior change MUST include tests or an explicit justification for why a test
  is not practical.
- Public API changes must be covered by unit or integration tests.
- When a public API defines multiple contract branches, tests MUST cover the meaningful variants, including null,
  empty, default, and filtered inputs when those branches change behavior.
- Regression fixes should include a test that fails before the fix and passes after it.

### Test Organization

- Mirror the source structure in test projects whenever practical.
- Reuse existing architecture test infrastructure when relevant:
  - `ArchitectureTestsBase<T>`
  - `SyncTestArchitecture`
  - `AsyncTestArchitecture`
- Keep tests focused on observable behavior, not implementation trivia.

### Source Generator Tests

- Source generator changes MUST be covered by generator tests.
- Preserve snapshot-based verification patterns already used in the repository.
- When generator behavior changes intentionally, update snapshots together with the implementation.

### Validation Commands

Use the smallest command set that proves the change, then expand if the change is cross-cutting.

```bash
# Build the full solution
dotnet build GFramework.sln -c Release

# Run all tests
dotnet test GFramework.sln -c Release

# Run a single test project
dotnet test GFramework.Core.Tests -c Release
dotnet test GFramework.Game.Tests -c Release
dotnet test GFramework.SourceGenerators.Tests -c Release
dotnet test GFramework.Ecs.Arch.Tests -c Release

# Run a single NUnit test or test group
dotnet test GFramework.Core.Tests -c Release --filter "FullyQualifiedName~CommandExecutorTests.Execute"

# Validate naming rules used by CI
bash scripts/validate-csharp-naming.sh
```

### Test Execution Expectations

- Run targeted tests for the code you changed whenever possible.
- Run broader solution-level validation for changes that touch shared abstractions, lifecycle behavior, source
  generators, or dependency wiring.
- Do not claim completion if required tests were skipped; state what was not run and why.

## Security Rules

- Validate external or user-controlled input before it reaches file system, serialization, reflection, code generation,
  or process boundaries.
- Do not build command strings, file paths, type names, or generated code from untrusted input without strict validation
  or allow-listing.
- Avoid logging secrets, tokens, credentials, or machine-specific sensitive data.
- Keep source generators deterministic and free of hidden environment or network dependencies.
- Prefer least-privilege behavior for file, process, and environment access.
- Do not introduce unsafe deserialization, broad reflection-based activation, or dynamic code execution unless it is
  explicitly required and tightly constrained.
- When adding caching, pooling, or shared mutable state, document thread-safety assumptions and failure modes.
- Minimize new package dependencies. Add them only when necessary and keep scope narrow.

## Documentation Rules

### Code Documentation

- Any change to public API, lifecycle semantics, module behavior, or extension points MUST update the related XML docs.
- If a framework abstraction changes meaning or intended usage, update the explanatory comments in code as part of the
  same change.

### Documentation Source Of Truth

- Treat source code, `*.csproj`, tests, generated snapshots, and packaging metadata as the primary evidence for
  documentation updates.
- Treat verified reference implementations under `ai-libs/` as a secondary evidence source for real project adoption
  patterns, directory layouts, and end-to-end usage examples.
- Treat existing `README.md` files and `docs/zh-CN/` pages as editable outputs, not authoritative truth.
- If existing documentation conflicts with code or tests, update the documentation to match the implementation instead
  of preserving outdated wording.
- Do not publish example code, setup steps, or package guidance that cannot be traced back to code, tests, or a
  verified consumer project.

### Module README Requirements

- Every user-facing package or module directory that contains a `*.csproj` intended for direct consumption MUST have a
  sibling `README.md`.
- Use the canonical filename `README.md`. Do not introduce new `ReadMe.md` or other filename variants.
- A module README MUST describe:
  - the module's purpose
  - the relationship to adjacent runtime, abstractions, or generator packages
  - the major subdirectories or subsystems the reader is expected to use
  - the minimum adoption path
  - the corresponding `docs/zh-CN/` entry points
- Adding a new top-level module directory without a `README.md` is considered incomplete work.
- If a module's responsibilities, setup, public API surface, generator inputs, or adoption path change, update that
  module's `README.md` in the same change.

### Repository Documentation

- Update the relevant `README.md` or `docs/` page when behavior, setup steps, architecture guidance, or user-facing
  examples change.
- Treat `ai-libs/` as a read-only third-party source reference area.
- Code under `ai-libs/**` exists for comparison, tracing, design study, and behavior verification; do not modify it
  unless the user explicitly asks to sync or update that third-party snapshot.
- When implementation plans, traces, reviews, or design notes say “reference a third-party project”, prefer the
  repository-local path under `ai-libs/` instead of an unspecified upstream repository.
- If a task depends on observations from `ai-libs/**`, record the referenced path and conclusion in the active plan or
  trace when the work is multi-step or complex, or when an active tracking document already exists, rather than editing
  the third-party reference copy.
- The main documentation site lives under `docs/`, with Chinese content under `docs/zh-CN/`.
- Keep code samples, package names, and command examples aligned with the current repository state.
- Prefer documenting behavior and design intent, not only API surface.
- When a feature is added, removed, renamed, or substantially refactored, contributors MUST update or create the
  corresponding user-facing integration documentation in `docs/zh-CN/` in the same change.
- For integration-oriented features such as the AI-First config system, documentation MUST cover:
    - project directory layout and file conventions
    - required project or package wiring
    - minimal working usage example
    - migration or compatibility notes when behavior changes
- If an existing documentation page no longer reflects the current implementation, fixing the code without fixing the
  documentation is considered incomplete work.
- Do not rely on “the code is self-explanatory” for framework features that consumers need to adopt; write the
  adoption path down so future users do not need to rediscover it from source.
- The repository root `README.md` MUST mirror the current top-level documentation taxonomy used by the docs site.
  Do not maintain a second, differently named navigation system in the root README.
- Prefer linking the root `README.md` to section landing pages such as `index.md` instead of deep-linking to a single
  article when the target is intended to be a documentation category.
- If a docs category appears in VitePress navigation or sidebar, it MUST have a real landing page or be removed from
  navigation in the same change.
- When examples are rewritten, preserve only the parts that remain true. Delete or replace speculative examples instead
  of lightly editing them into another inaccurate form.

### Task Tracking

- `ai-plan/` is split by intent:
  - `ai-plan/public/README.md`: the shared startup index that binds worktrees or branches to active topics and resume
    entry points
  - `ai-plan/public/<topic>/todos/`: repository-safe recovery documents for an active topic
  - `ai-plan/public/<topic>/traces/`: repository-safe execution traces for an active topic
  - `ai-plan/public/<topic>/archive/`: archived stage-level artifacts that still belong to an active topic; prefer
    `archive/todos/` and `archive/traces/` when archiving content cut out of the active entry files
  - `ai-plan/public/archive/<topic>/`: completed-topic archives that should not be treated as default boot context
  - `ai-plan/private/`: worktree-private recovery artifacts; keep these untracked and scoped to the current worktree
- Contributors MUST keep committed `ai-plan/public/**` content safe to publish in Git history.
- Never write secrets, tokens, credentials, private keys, machine usernames, home-directory paths, hostnames, IP
  addresses, proprietary URLs, or other sensitive environment details into any `ai-plan/**` file.
- Never record absolute file-system paths in `ai-plan/**`; use repository-relative paths, branch names, PR numbers, or
  stable document identifiers instead.
- Use `ai-plan/public/**` only for durable, handoff-safe task state. Put temporary notes, local experiments, or
  worktree-specific scratch recovery data under `ai-plan/private/`.
- `ai-plan/public/README.md` MUST list only active topics. Do not add `ai-plan/public/archive/**` content to the
  default boot index.
- When a worktree-to-topic mapping changes, or when a topic becomes active/inactive, contributors MUST update
  `ai-plan/public/README.md` in the same change.
- When working from a tracked implementation plan, contributors MUST update the corresponding tracking document under
  `ai-plan/public/<topic>/todos/` in the same change.
- Tracking updates MUST reflect completed work, newly discovered issues, validation results, and the next recommended
  recovery point.
- Active tracking and trace files are recovery entrypoints, not append-only changelogs. They MUST stay concise enough
  for `boot` to locate the current recovery point quickly.
- Completing code changes without updating the active tracking document is considered incomplete work.
- For any multi-step refactor, migration, or cross-module task, contributors MUST create or adopt a dedicated recovery
  document under `ai-plan/public/<topic>/todos/` before making substantive code changes.
- Recovery documents MUST record the current phase, the active recovery point identifier, known risks, and the next
  recommended resume step so another contributor or subagent can continue the work safely.
- Contributors MUST maintain a matching execution trace under `ai-plan/public/<topic>/traces/` for complex work. The
  trace should record the current date, key decisions, validation milestones, and the immediate next step.
- When a stage inside an active topic is fully complete, move the finished artifacts into that topic's `archive/`
  directory instead of leaving every completed step in the default boot path.
- When completed and validated stages begin to accumulate, contributors MUST archive their detailed history out of the
  active `todos/` and `traces/` entry files in the same change. Keep only the current recovery point, active facts,
  active risks, immediate next step, and pointers to the relevant archive files in the default boot path.
- When a topic is fully complete, move the entire topic directory under `ai-plan/public/archive/<topic>/` and remove it
  from `ai-plan/public/README.md` in the same change.
- When a task spans multiple commits or is likely to exceed a single agent context window, update both the recovery
  document and the trace at each meaningful milestone before pausing or handing work off.
- If subagents are used on a complex task, the main agent MUST capture the delegated scope and any accepted findings in
  the active recovery document or trace before continuing implementation.

### Documentation Preview

When documentation changes need local preview, use:

```bash
cd docs && bun install && bun run dev
```

## Review Standard

Before considering work complete, confirm:

- Required comments and XML docs are present
- Code follows repository style and naming rules
- Relevant tests were added or updated
- Sensitive or unsafe behavior was not introduced
- User-facing documentation is updated when needed
- Feature adoption docs under `docs/zh-CN/` were added or updated when functionality was added, removed, or refactored
