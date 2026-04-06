# AGENTS.md

This document is the single source of truth for coding behavior in this repository.

All AI agents and contributors must follow these rules when writing, reviewing, or modifying code in `GFramework`.

## Environment Capability Inventory

- Before choosing runtimes or CLI tools, read `@.ai/environment/tools.ai.yaml`.
- Use `@.ai/environment/tools.raw.yaml` only when you need the full collected facts behind the AI-facing hints.
- Prefer the project-relevant tools listed there instead of assuming every installed system tool is fair game.
- If the real environment differs from the inventory, use the project-relevant installed tool and report the mismatch.

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

### Task Tracking

- When working from a tracked implementation plan, contributors MUST update the corresponding tracking document under
  `local-plan/todos/` in the same change.
- Tracking updates MUST reflect completed work, newly discovered issues, validation results, and the next recommended
  recovery point.
- Completing code changes without updating the active tracking document is considered incomplete work.

### Repository Documentation

- Update the relevant `README.md` or `docs/` page when behavior, setup steps, architecture guidance, or user-facing
  examples change.
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
