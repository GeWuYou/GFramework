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
- Verified current PR review findings locally: the validator regex still missed standalone attributes, while the
  `docs/zh-CN/contributing.md` generator-boundary text should be removed instead of repositioned because it is
  maintainer-facing governance rather than reader-facing contribution guidance.
- Added a Python regression test for standalone, parameterized, fully qualified, and multi-attribute declarations so
  future validator edits cannot silently reintroduce the false negative.
- Added comment-line filtering for the validator after the first regex fix started matching XML documentation examples
  such as `/// [ContextAware]`, which would otherwise create false CI failures for reader-facing code comments.

## Validation Target

- `python3 scripts/validate-runtime-generator-boundaries.py`
- `python3 scripts/test_validate_runtime_generator_boundaries.py`
- `python3 scripts/license-header.py --check`
- `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
- `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release`
- `dotnet pack GFramework.sln -c Release -p:PackageVersion=<local>`

## Latest Validation Result

- `python3 scripts/test_validate_runtime_generator_boundaries.py` passed on 2026-05-05.
- `python3 scripts/validate-runtime-generator-boundaries.py` passed on 2026-05-05.
- `python3 scripts/license-header.py --check` passed on 2026-05-05.
- `dotnet build GFramework.Game/GFramework.Game.csproj -c Release` passed on 2026-05-05 with 0 warnings and 0 errors.

## Next Recommended Resume Step

Run the boundary validator, the new Python regression tests, and the minimal Release build/pack validation; then push
the follow-up commit so the open PR review threads can be resolved against fresh CI.
