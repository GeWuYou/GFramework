# Runtime / Generator Boundary Tracking

## Goal

Keep runtime, abstractions, and meta-package modules free from source-generator project references, source-generator
attributes, and leaked NuGet dependencies.

## Current Recovery Point

- Recovery point: RGB-RP-001
- Phase: remove `GFramework.Game` generator coupling and add repository guardrails
- Focus:
  - delete `GFramework.Game`'s dependency on `GFramework.Core.SourceGenerators.Abstractions`
  - remove unused `[GenerateEnumExtensions]` usage from `GFramework.Game`
  - add static and packed-package validation so runtime packages cannot regress

## Active Risks

- A runtime package can still compile locally if it references a non-packable generator helper project, so regressions are
  easy to miss without an explicit guard.
- A leaked package dependency may only surface when a consumer restores from NuGet, not during normal repository builds.

## Completed In This Stage

- Confirmed `GFramework.Game` was the direct runtime offender and `GeWuYou.GFramework.Game` leaked
  `GFramework.Core.SourceGenerators.Abstractions` into its nuspec dependency graph.
- Confirmed the two `[GenerateEnumExtensions]` usages inside `GFramework.Game` do not need generated output and can be
  removed outright.

## Validation Target

- `python3 scripts/validate-runtime-generator-boundaries.py`
- `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
- `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release`
- `dotnet pack GFramework.sln -c Release -p:PackageVersion=<local>`

## Next Recommended Resume Step

Run the new boundary validator plus minimal Release build and pack validation, then inspect `GeWuYou.GFramework.Game`
and transitive runtime packages to confirm no `SourceGenerators` dependency remains.
