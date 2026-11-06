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
- When the goal is to inspect or reduce warnings printed during project build, contributors MUST establish the warning
  baseline from a non-incremental repository-root build by running `dotnet clean` and then `dotnet build`.
- Contributors MUST NOT treat a repeated incremental `dotnet build` result as authoritative for warning inspection when
  a clean baseline has not been captured in the same round.
- If a direct `dotnet clean`, `dotnet build`, or `dotnet test` command fails inside the agent sandbox with missing
  diagnostics, `Permission denied`, MSBuild pipe/socket errors, or other environment-only noise that does not match a
  normal shell invocation, contributors MUST request permission and rerun the same direct command outside the sandbox
  before concluding that the repository or toolchain is broken.
- For repository truth, contributors MUST prefer the result of the original direct command executed outside the sandbox
  over sandbox-only failures, workaround-heavy variants, or speculative environment flags unless the user explicitly
  asks for a non-default command shape.
- If the task changes multiple projects or shared abstractions, prefer a solution-level or affected-project
  `dotnet build ... -c Release`; otherwise use the smallest build command that still proves the result compiles.
- When a task adds a feature or modifies code, contributors MUST run a Release build for every directly affected
  module/project instead of relying on an unrelated project or solution slice that does not actually compile the touched
  code.
- Warnings reported by those affected-module builds are part of the task scope. Contributors MUST resolve the touched
  module's build warnings in the same change, or stop and explicitly report the exact warning IDs and blocker instead of
  deferring them to a separate long-lived cleanup branch by default.
- If the required build passes and there are task-related staged or unstaged changes, contributors MUST create a Git
  commit automatically instead of leaving the task uncommitted, unless the user explicitly says not to commit.
- Commit messages MUST use Conventional Commits format: `<type>(<scope>): <summary>`.
- The commit `summary` MUST use simplified Chinese and briefly describe the main change.
- The commit `body` MUST use unordered list items, and each item MUST start with a verb such as `新增`、`修复`、`优化`、
  `更新`、`补充`、`重构`.
- Each commit body bullet MUST describe one independent change point; avoid repeated or redundant descriptions.
- Commit `type` MUST reflect release semantics instead of author intent:
  - Use `feat` only for user-facing or consumer-facing capability additions that should raise the next released version's
    `minor` segment.
  - Use `fix` for behavior corrections, `perf` for observable performance improvements, and `refactor` only for
    non-feature code restructuring; these should raise the next released version's `patch` segment.
  - Use `deps` for dependency version updates, dependency lockfile refreshes, and package maintenance that should raise
    the next released version's `patch` segment.
  - Use `security` for vulnerability fixes, dependency security mitigations, and security configuration corrections
    that should raise the next released version's `patch` segment.
  - Use `docs`、`test`、`chore`、`build`、`ci`、`style` for their literal categories; do not encode these changes as
    `feat` just because they feel important. These categories MUST NOT trigger a release.
  - Use `BREAKING CHANGE` in the commit footer or `!` after the type / scope header (for example `feat!:` or
    `feat(core)!:`) when the change should raise the next released version's `major` segment.
- Documentation-only changes MUST NOT use `feat`, including new guides, refreshed examples, navigation updates, and
  adoption notes for existing capabilities. If a commit changes both product behavior and related docs, either split the
  commit or use `feat` only when the code/package behavior is the primary released change.
- Contributors MUST avoid ambiguous scopes such as `feat(docs)` for documentation work. If the change only affects docs,
  prefer `docs(<module-or-area>)`; if it adds a real capability in a docs-related toolchain, use the scope of that
  actual subsystem instead of `docs`.
- Keep technical terms in English when they are established project terms, such as `API`、`Model`、`System`.
- When composing a multi-line commit body from shell commands, contributors MUST NOT rely on Bash `$"..."` quoting for
  newline escapes, because it passes literal `\n` sequences to Git. Use multiple `-m` flags or ANSI-C `$'...'`
  quoting so the commit body contains real line breaks.
- If a new task starts while the current branch is `main`, contributors MUST first try to update local `main` from the
  remote, then create and switch to a dedicated branch before making substantive changes.
- The branch naming rule for a new task branch is `<type>/<topic-or-scope>`, where `<type>` should match the intended
  Conventional Commit category as closely as practical.

## License Header Rules

- Repository-maintained source and configuration files that are supported by `scripts/license-header.py` MUST include an
  Apache-2.0 file header before the task is considered complete.
- When creating or modifying supported files, contributors MUST preserve an existing compliant header or add the SPDX
  header generated by `python3 scripts/license-header.py --fix`.
- Before committing changes that add or modify supported source/configuration files, contributors MUST run
  `python3 scripts/license-header.py --check` and resolve any missing or misplaced headers.
- For files with shebang lines, keep the shebang as the first line and place the license header immediately after it.
- For XML/MSBuild files with an XML declaration, keep the XML declaration as the first node and place the license header
  immediately after it.
- Do not add project license headers to excluded or third-party areas such as `.agents/**`, `ai-libs/**`,
  `third-party-licenses/**`, generated snapshots, binary assets, lock files, and generated build output. Treat
  `scripts/license-header.py` as the authoritative include/exclude policy for this check.
- If CI reports a license-header failure, either fix it locally with `python3 scripts/license-header.py --fix` or, for
  maintainer-owned cleanup, use the manual `License Header Fix` GitHub Actions workflow to create a reviewed repair PR.

## Task Startup and Recovery

- Read this document and `.ai/environment/tools.ai.yaml` before substantive work.
- Inspect the current branch, HEAD and working-tree changes; preserve unrelated work.
- Read `engineering/README.md` to locate the relevant architecture, development guidance and roadmap task.
  For continuation, reconstruct current source, diff and task status instead of inheriting historical PR or branch state.
- Ordinary tasks proceed directly under these rules; startup and continued execution do not require a separate skill.
- For multi-step work, update the single task entry described in Project Development Documentation. Record verified
  progress, remaining work and next action at a scope boundary; do not maintain paired execution logs.

## Subagent Usage Rules

- Use one agent by default. Delegate only when the user requests parallel or delegated work.
- Before dispatch, agree on the interfaces and dependencies, concrete objective, disjoint file/subsystem ownership,
  required validation and expected output. Keep the immediate critical path local.
- Workers must preserve other contributors' changes and stay within their assigned scope. They do not stage or commit
  in a shared checkout; the main agent integrates the task-owned changes.
- The main agent owns validation, review of each result, final integration and recovery updates. Worker reports alone
  do not establish acceptance. Stop adding slices when ownership overlaps or the combined change becomes hard to review.
- Use the models and agent capabilities available in the current session; do not require historical model names.

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
- Framework runtime, abstractions, and meta-package projects MUST NOT reference `*.SourceGenerators*` projects or packages,
  and MUST NOT use source-generator attributes such as `GenerateEnumExtensions` or `ContextAware`. Those capabilities are
  reserved for consumer projects, generator projects, examples explicitly meant to demonstrate generator usage, and related tests.

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
If a sandboxed agent run reports environment-specific .NET failures, rerun the same direct command outside the sandbox
and treat that unsandboxed result as authoritative for validation and warning baselines.

```bash
# Check warnings from the default repository build entrypoint
dotnet clean
dotnet build

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
- Public documentation under `README.md` and `docs/**` MUST stay reader-facing. Do not publish governance-only content
  such as inventory tables, coverage baselines, review queues, batch metrics, recovery points, trace summaries, or
  “this still needs a later audit wave” notes in those user-facing pages.
- Public documentation MUST use semantic section titles and link labels. Do not surface raw filenames or paths such as
  `README.md`、`game/index.md`、`../core/cqrs.md` as reader-facing navigation text when a meaningful destination label is
  available.
- Public documentation MUST avoid rhetorical, self-referential, or AI-sounding headings and prompts such as
  “你真正会用到的公开入口”、
  “先理解包关系”、
  “这个栏目应该回答什么” or “想看……转到……”. Prefer neutral labels such as
  “公开入口”、
  “模块与包关系”、
  “栏目覆盖范围” and “相关主题”.
- Public documentation MUST present limitations, suitability, and migration boundaries as adoption guidance for readers.
  Do not publish internal-governance or product-roadmap wording such as “当前阶段的结论”、
  “不建议立即启动”、
  “仓库当前的主要使用者” or similar maintainer-facing decision records in `README.md` or `docs/**`; that material
  belongs in `engineering/**` if it must be tracked.
- Governance-only material such as XML audit snapshots, documentation remediation baselines, backlog status, and
  durable development decisions belong in `engineering/**`; local recovery metadata belongs in ignored `.local/`.
- Treat `ai-libs/` as a read-only third-party source reference area.
- Code under `ai-libs/**` exists for comparison, tracing, design study, and behavior verification; do not modify it
  unless the user explicitly asks to sync or update that third-party snapshot.
- When implementation plans, traces, reviews, or design notes say “reference a third-party project”, prefer the
  repository-local path under `ai-libs/` instead of an unspecified upstream repository.
- If a task depends on observations from `ai-libs/**`, record the referenced path and conclusion in the relevant
  engineering document or roadmap task when the work is multi-step or complex, rather than editing the reference copy.
- The main documentation site lives under `docs/`, with Chinese content under `docs/zh-CN/`.
- Keep code samples, package names, and command examples aligned with the current repository state.
- Prefer documenting behavior and design intent, not only API surface.
- When a public page references XML docs or API coverage, convert that evidence into reader-facing guidance: explain
  which types, namespaces, or entry points readers should inspect and why, instead of exposing audit counts or
  governance terminology.
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

### Project Development Documentation

- `engineering/README.md` is the contributor documentation index; `docs/` remains the user-facing documentation site.
- Keep current architecture and significant decisions in `engineering/architecture/`, development and documentation
  guidance in `engineering/development/`, and implementation sequence plus task status in `engineering/roadmap/`.
- Before substantive multi-step refactoring or migration, create or adopt one roadmap task with scope, status,
  acceptance criteria, evidence and next action. Update that task at meaningful milestones; do not duplicate its status
  in architecture reports, paired tracking/trace files, branch maps or numbered recovery points.
- Mark planned APIs and target architecture explicitly. Source, project files and tests remain authoritative for
  current behavior; archived history is available through Git and is not a current task queue.
- Keep task entries concise. Retain completed acceptance evidence in the task, and use Git for detailed execution history.
- When adding, moving or deleting development documents, update the engineering index and affected incoming links in
  the same change. See `engineering/development/documentation.md` for ownership and validation.
- Put temporary probes, command logs and worktree-private handoff notes in ignored `.local/`. Preserve unrelated
  tasks and untracked private files; do not recursively delete a directory merely because tracked documentation was retired.
- Committed development documents must be safe to publish: no secrets, credentials, machine usernames, absolute
  filesystem paths, hostnames, IP addresses or proprietary URLs. Use repository-relative paths and stable identifiers.

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
