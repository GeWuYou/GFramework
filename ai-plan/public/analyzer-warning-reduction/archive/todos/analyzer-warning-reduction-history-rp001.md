# Analyzer Warning Reduction History

> 该文档于 `2026-04-19` 从旧 `local-plan/todos/analyzer-warning-reduction-tracking.md` 迁入，作为
> `ANALYZER-WARNING-REDUCTION-RP-001` 的历史跟踪归档保留。

## Legacy Tracking Content

## Goal

Reduce the currently surfaced Meziantou analyzer warnings from the repository build by prioritizing low-risk,
behavior-preserving fixes first, then reassessing whether larger refactors are justified for remaining long-method and
file-structure warnings.

## Current Recovery Point

- Recovery point: `ANALYZER-WARNING-REDUCTION-RP-001`
- Current phase: `Phase 1`
- Active focus:
  - capture the current warning clusters with `dotnet build ... -t:Rebuild -clp:Summary;WarningsOnly`
  - fix low-risk runtime and generator warnings that do not require architectural rewrites
  - keep this tracking document and the paired trace synchronized with subagent scope, accepted fixes, and validation
  - keep the current checkpoint test-green for the targeted regressions introduced by warning-reduction edits

## Planned Work

- [x] Read `AGENTS.md` and `.ai/environment/tools.ai.yaml` before choosing commands.
- [x] Confirm the current warning set still reproduces in a clean rebuild-oriented build invocation.
- [x] Group warnings into independent work slices before delegating.
- [x] Record delegated ownership boundaries for parallel subagent work.
- [x] Reduce low-risk warnings in `GFramework.Core` / `GFramework.Cqrs`.
- [x] Reduce low-risk warnings in `GFramework.Godot`.
- [x] Reduce low-risk warnings in source generator projects without broad generator rewrites.
- [x] Rebuild the affected projects and record the remaining warning hotspots.
- [x] Repair the targeted test regressions introduced by the current warning-reduction checkpoint.
- [x] Apply the current CodeRabbit follow-up fixes for module-install ordering, logging factory null-safety/disposal, and
  Godot YAML directory enumeration contracts.
- [x] Apply the latest CodeRabbit follow-up fixes for failure-path module tracking, logger-name validation, and
  brittle test replacement.
- [x] Apply the latest CodeRabbit follow-up fixes for null appender-entry validation and async doc-sample correctness.
- [ ] Decide whether any remaining `MA0051` / file-structure warnings are safe to tackle in the same round.

## Validation

Planned validation commands:

```bash
dotnet build GFramework.sln -c Release -t:Rebuild -nologo -clp:Summary;WarningsOnly
dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild -nologo -clp:Summary;WarningsOnly
dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release -t:Rebuild -nologo -clp:Summary;WarningsOnly
dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release -t:Rebuild -nologo -clp:Summary;WarningsOnly
```

Results:

- `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release --no-restore`
  - passed with `0 Warning(s)` and `0 Error(s)`.
- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release -t:Rebuild --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -nologo -clp:Summary;WarningsOnly`
  - passed with `1 Warning(s)` and `0 Error(s)`.
  - remaining warning:
    - `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` `MA0051` long-method warning.
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -nologo -clp:Summary;WarningsOnly`
  - first focused rebuild after the initial runtime-fix pass: `49 Warning(s)`, `0 Error(s)`.
  - latest focused rebuild after the follow-up structural cleanup: `31 Warning(s)`, `0 Error(s)`.
  - the remaining `net8.0` warnings are now concentrated in:
    - generic/non-generic file-name collisions that cannot be fixed cheaply without API renames
    - delegate-shape rules around specific callbacks
    - a smaller set of long-method warnings
    - a few collection-abstraction warnings on public configuration types
- `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false`
  - worker validation reported a successful build after the Godot-owned fixes.
  - remaining Godot-owned warnings reported by the worker:
    - `GFramework.Godot/Setting/Data/LocalizationMap.cs` `MA0016` on the two public `Dictionary`-typed properties.
  - broader direct `GFramework.Godot` builds still surface many `GFramework.Game` dependency warnings, so those results
    should not be interpreted as unresolved Godot-owned work.
- `dotnet test GFramework.Core.Tests -c Release --filter "FullyQualifiedName~GetString_WithUnknownCompactFormatterArgs_ShouldIgnoreUnknownOptions|FullyQualifiedName~GetString_WithVariable_ShouldFormatCorrectly|FullyQualifiedName~GetString_WithMultipleVariables_ShouldFormatCorrectly|FullyQualifiedName~GetString_WithInvalidCompactFormatterArgs_ShouldFallbackToDefaultFormatting|FullyQualifiedName~GetString_WithCompactFormatterArgs_ShouldApplyOptions|FullyQualifiedName~GetString_WithCompactFormatter_ShouldFormatCorrectly|FullyQualifiedName~Ensure_Should_Create_ArgumentException_With_Message|FullyQualifiedName~RegisterCqrsHandlersFromAssemblies_WithNullAssemblyItem_Should_ThrowArgumentNullException"`
  - passed with `0 Failed`, `8 Passed`, `0 Skipped`.
  - regressions fixed in this validation step:
    - `LocalizationString` placeholder formatting stopped replacing values after the regex switched to `RegexOptions.ExplicitCapture` while still reading unnamed capture groups.
    - `ResultExtensions.Ensure` changed the observable `ArgumentException.Message` by adding a parameter name.
    - `MicrosoftDiContainer.RegisterCqrsHandlersFromAssemblies` changed the null-item contract from `ArgumentNullException` to `ArgumentException`.
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NumericExtensionsTests|FullyQualifiedName~ResultTests|FullyQualifiedName~ResultExtensionsTests|FullyQualifiedName~LoggingConfigurationTests"`
  - passed with `0 Failed`, `101 Passed`, `0 Skipped`.
  - covered the new review-driven fixes for:
    - `NumericExtensions.Between` null contract enforcement
    - `Result` default-value safety in `Equals` / `GetHashCode` / `ToString`
    - `ResultExtensions.Ensure` parameter-name contract
    - `ConfigurableLoggerFactory` longest-prefix logger-level selection
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~SettingsSystemTests|FullyQualifiedName~GodotLocalizationSettingsTests"`
  - passed with `0 Failed`, `7 Passed`, `0 Skipped`.
  - revalidated the `IApplyAbleSettings.ApplyAsync` rename through system and Godot localization callers.
- `dotnet build GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release`
  - passed with `0 Error(s)`.
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~LoadAsync_Should_Use_Globalized_Res_Directory_Directly_When_Running_In_Editor"`
  - passed with `0 Failed`, `1 Passed`, `0 Skipped`.
  - used as the stable Godot-side smoke test after the review-driven changes to `SceneBehaviorBase`,
    `AbstractArchitecture`, and `GodotYamlConfigEnvironment`.
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ConfigurableLoggerFactoryTests|FullyQualifiedName~LoggingConfigurationTests"`
  - narrowed to `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ConfigurableLoggerFactoryTests" -m:1`
  - passed with `0 Failed`, `3 Passed`, `0 Skipped`.
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~LoadAsync_Should_Use_Globalized_Res_Directory_Directly_When_Running_In_Editor"`
  - passed with `0 Failed`, `1 Passed`, `0 Skipped`.
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GodotLocalizationSettingsTests"`
  - passed with `0 Failed`, `3 Passed`, `0 Skipped`.
- `dotnet build GFramework.sln -c Release`
  - rerun serially as `dotnet build GFramework.sln -c Release -m:1`
  - passed with `698 Warning(s)` and `0 Error(s)`.
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ConfigurableLoggerFactoryTests" -m:1`
  - latest follow-up rerun passed with `0 Failed`, `5 Passed`, `0 Skipped`.
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~AbstractArchitectureModuleInstallationTests" -m:1`
  - passed with `0 Failed`, `1 Passed`, `0 Skipped`.
  - now directly covers the contract that `InstallGodotModule` throws before `module.Install(...)` when `_anchor` is unavailable.
- `dotnet build GFramework.sln -c Release -m:1`
  - latest follow-up rerun passed with `847 Warning(s)` and `0 Error(s)`.
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ConfigurableLoggerFactoryTests" -m:1`
  - latest null-appender validation rerun passed with `0 Failed`, `6 Passed`, `0 Skipped`.
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -m:1`
  - latest focused release build passed with `79 Warning(s)` and `0 Error(s)`.

## Known Risks

- Many warnings are duplicated across target frameworks, so a small source edit can remove multiple reported instances;
  warning counts must be interpreted by unique source location, not only raw line count.
- Some warning families such as `MA0158` (`System.Threading.Lock`) are not obviously safe in the current multi-targeted
  `net8.0;net9.0;net10.0` runtime modules and may require conditional compilation or a broader policy decision.
- Several `MA0051` findings sit in very large files such as `SchemaConfigGenerator.cs` and `YamlConfigLoaderTests.cs`;
  these may require more refactoring than is appropriate for a warning-reduction pass.
- A subset of the remaining `MA0048` warnings come from generic/non-generic type families that share the same base type
  name, so the analyzer cannot be satisfied by a trivial file move alone.
- The current checkpoint has only been revalidated against the eight known regression tests above; a broader test rerun
  is still pending if the next batch expands the edit surface again.
- The `GodotYamlConfigEnvironment` default-directory enumeration fix depends on host file-system and Godot native API
  behavior; if a deterministic unit test cannot be kept stable in this WSL test host, retain build plus loader-smoke
  validation and document the gap explicitly.
- The `AbstractArchitecture.InstallGodotModule` ordering fix can be asserted in pure managed code, but attempting to
  exercise `AbstractArchitecture.Initialize()` directly in the current Godot test host crashes the native test
  process. Keep this fix covered by project build plus adjacent Godot smoke tests unless a more stable harness is
  introduced.
- The install-ordering regression is now covered by a stable direct-call Godot test. The destroy-observation path still
  lacks a dedicated automated assertion because intercepting `GD.PushError` reliably in the current .NET test host is
  not yet practical.
- Parallel `dotnet test` / `dotnet build` executions against the Windows-backed worktree still trigger transient
  `obj/bin` file-lock failures; keep validation serial with `-m:1` in this environment.
- A direct Godot test that calls `GodotYamlConfigEnvironment.Default.ReadAllBytes("res://missing")` currently crashes the
  .NET test host in this environment, so the missing-file contract fix for the default Godot file API path is covered
  by code review plus project build and adjacent loader tests rather than a dedicated failing-path unit test.

## Recommended Resume Step

1. Continue from `ANALYZER-WARNING-REDUCTION-RP-001` only if the next batch is willing to take on structural fixes such
   as long-method splits, delegate-shape rewrites, or public configuration-surface refactors.
2. Preserve the review-follow-up contracts now covered by tests:
   `NumericExtensions.Between` null checking, `Result` default-value safety, longest-prefix logger-level selection, and
   the `IApplyAbleSettings.ApplyAsync` rename.
3. If the next batch wants stronger Godot validation for missing-path reads, investigate a harness that can safely
   exercise `FileAccess.GetFileAsBytes` failure paths without crashing the test host before adding a dedicated test.
