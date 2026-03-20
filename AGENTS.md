# Repository Guidelines

## Project Structure & Module Organization

`GFramework.sln` is the entry point for the full .NET solution. Runtime code lives in `GFramework.Core/`,
`GFramework.Game/`, `GFramework.Godot/`, and `GFramework.Ecs.Arch/`. Interface-only contracts stay in the paired
`*.Abstractions/` projects. Roslyn generators are split across `GFramework.SourceGenerators/`,
`GFramework.Godot.SourceGenerators/`, and `GFramework.SourceGenerators.Common/`. Tests mirror the runtime modules in
`GFramework.Core.Tests/`, `GFramework.Game.Tests/`, `GFramework.Ecs.Arch.Tests/`, and
`GFramework.SourceGenerators.Tests/`. Documentation is under `docs/`, Godot templates under `Godot/script_templates/`,
and repository utilities under `scripts/` and `refactor-scripts/`.

## Build, Test, and Development Commands

- `dotnet build GFramework.sln` builds the full solution from the repo root.
- `dotnet test GFramework.sln --no-build` runs all NUnit test projects after a build.
- `dotnet test GFramework.Core.Tests --filter "FullyQualifiedName~CommandExecutorTests.Execute"` runs a focused NUnit
  test.
- `bash scripts/validate-csharp-naming.sh` checks PascalCase namespace and directory rules used by CI.
- `cd docs && bun install && bun run dev` starts the VitePress docs site locally.

## Coding Style & Naming Conventions

Use standard C# formatting with 4-space indentation and one public type per file. The repository keeps `ImplicitUsings`
disabled and `Nullable` enabled, so write explicit `using` directives and annotate nullability carefully. Follow
`PascalCase` for types, methods, namespaces, directories, and constants; use `_camelCase` for private fields and
`camelCase` for locals and parameters. Keep namespaces aligned with folders, for example
`GFramework.Core.Architectures`.

## Testing Guidelines

Tests use NUnit 4 with `Microsoft.NET.Test.Sdk`; some suites also use Moq. Place tests in the matching module test
project and name files `*Tests.cs`. Prefer directory parity with production code, for example `GFramework.Core/Logging/`
and `GFramework.Core.Tests/Logging/`. Add or update tests for every behavior change, especially public APIs, source
generators, and integration paths.

## Commit & Pull Request Guidelines

Recent history follows Conventional Commits style such as `feat(events): ...`, `refactor(localization): ...`,
`docs(guide): ...`, and `test(localization): ...`. Keep commits scoped and imperative. PRs should explain the
motivation, implementation, and validation commands run; link related issues; and include screenshots when docs, UI, or
Godot-facing behavior changes.
