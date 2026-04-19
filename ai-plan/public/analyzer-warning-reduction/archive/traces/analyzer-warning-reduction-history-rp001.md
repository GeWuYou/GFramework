# Analyzer Warning Reduction History Trace

> 该文档于 `2026-04-19` 从旧 `local-plan/traces/analyzer-warning-reduction-trace.md` 迁入，作为
> `ANALYZER-WARNING-REDUCTION-RP-001` 的历史执行 trace 归档保留。

## Legacy Trace Content

## 2026-04-18

### Stage: Discovery

- Read `AGENTS.md` and `.ai/environment/tools.ai.yaml` before choosing commands.
- Confirmed the repository currently surfaces a large batch of Meziantou analyzer diagnostics during
  `dotnet build GFramework.sln -c Release -nologo -clp:Summary;WarningsOnly`.
- Confirmed that `dotnet build ... -t:Rebuild` is required to force the compiler/analyzers to emit the warnings
  reliably during project-level validation.
- Identified three independent work slices suitable for parallel handling:
  - `GFramework.Core` plus closely related `GFramework.Cqrs` runtime warnings
  - `GFramework.Godot` runtime warnings
  - source generator warnings under `GFramework.Core.SourceGenerators`, `GFramework.Cqrs.SourceGenerators`, and
    `GFramework.Godot.SourceGenerators`

### Current Recovery Point

- `ANALYZER-WARNING-REDUCTION-RP-001`

### Stage: Delegation

- Delegated `GFramework.Godot/**` low-risk warning reduction to worker `Newton`.
  Scope:
  - runtime-focused analyzer fixes only
  - no edits outside `GFramework.Godot/**` except `GFramework.Godot.Tests/**` if validation strictly needs it
  - avoid broad refactors and preserve behavior
- Delegated source generator low-risk warning reduction to worker `Aristotle`.
  Scope:
  - only `GFramework.Core.SourceGenerators/**`, `GFramework.Cqrs.SourceGenerators/**`, and
    `GFramework.Godot.SourceGenerators/**`
  - explicitly exclude `GFramework.Game.SourceGenerators/**`
  - prioritize string-comparison, parameter-name, file/type-name, and localized method-splitting fixes

### Stage: Main-Thread Focus

- Reserved `GFramework.Core/**` and `GFramework.Cqrs/**` for the main thread because they dominate the current warning
  surface and form the primary validation path.
- Confirmed the current worktree already contains an unrelated git status entry:
  - `AD GFramework.Core/Directory.Build.props`
  This file is not part of the current warning-reduction edit scope and must not be reverted.

### Stage: Implementation

- Applied low-risk runtime fixes across `GFramework.Core/**` and `GFramework.Cqrs/**`, including:
  - `ConfigureAwait(false)` on context-independent asynchronous hot paths
  - `StringComparer.Ordinal` / `StringComparison.Ordinal` for string-keyed collections and comparisons
  - invariant-formatting fixes for logging/statistics paths
  - targeted `ArgumentException` / `ArgumentNullException` overload cleanup
  - localized file-structure cleanup for cache/config helper types
- Split a few file/type mismatches that were cheap to resolve without API churn:
  - `GFramework.Cqrs/Internal/WeakTypePairCache.cs`
  - `GFramework.Core/Logging/AppenderConfiguration.cs`
  - `GFramework.Core/Logging/FilterConfiguration.cs`
  - `GFramework.Core/Logging/ConfigurableLoggerFactory.cs`
  - `GFramework.Core/Resource/ResourceCacheEntry.cs`

### Stage: Worker Results

- Worker `Aristotle` completed the source-generator slice and landed:
  - `MA0006` string-comparison cleanup
  - `MA0048` fix by renaming `LoggerDiagnostic.cs` to `LoggerDiagnostics.cs`
  - localized `MA0015` cleanup
  - small `MA0051` splits in `AutoRegisterModuleGenerator.cs` and `EnumExtensionsGenerator.cs`
- Worker `Newton` completed the Godot slice and landed:
  - `ConfigureAwait(false)` cleanup where context capture was unnecessary
  - string-comparison / comparer fixes
  - invariant formatting and argument-overload cleanup
  - file split for `GodotYamlConfigEnvironment` and `GodotYamlConfigDirectoryEntry`
  - explicit observation of an async result in `AbstractArchitecture`
- Accepted residual worker findings:
  - `GFramework.Core.SourceGenerators/Rule/ContextAwareGenerator.cs` still has a broader `MA0051` refactor remaining.
  - `GFramework.Godot/Setting/Data/LocalizationMap.cs` still has two `MA0016` warnings on public `Dictionary`-typed
    properties that were intentionally left unchanged to avoid public-surface / serialization impact.

### Stage: Validation

- Ran:
  - `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release --no-restore`
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release -t:Rebuild --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -nologo -clp:Summary;WarningsOnly`
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -nologo -clp:Summary;WarningsOnly`
  - `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false`
- Results:
  - `GFramework.Core.SourceGenerators`: `0 Warning(s)`, `0 Error(s)`
  - `GFramework.Cqrs` (`net8.0` rebuild): `1 Warning(s)`, `0 Error(s)`
  - `GFramework.Core` (`net8.0` rebuild):
    - intermediate focused rebuild: `49 Warning(s)`, `0 Error(s)`
    - latest focused rebuild: `31 Warning(s)`, `0 Error(s)`
  - `GFramework.Godot`: worker-reported success after the Godot-owned fixes; broader direct builds still include a
    large inherited `GFramework.Game` warning set from dependencies

### Stage: Regression Repair

- After the checkpoint commit, reproduced the user-reported test regressions from the current branch instead of
  resuming warning cleanup blindly.
- Isolated the failures to three behavior changes:
  - `LocalizationString` enabled `RegexOptions.ExplicitCapture` but still read unnamed capture groups, so every
    localization placeholder fell back to the original token text.
  - `ResultExtensions.Ensure` started constructing `ArgumentException` with a parameter name, which changed the
    externally observed message contract covered by tests.
  - `MicrosoftDiContainer.RegisterCqrsHandlersFromAssemblies` rejected null assembly items with `ArgumentException`
    instead of the previously expected `ArgumentNullException`.
- Repaired the regressions by:
  - switching the localization placeholder regex to named capture groups and reading those names explicitly
  - restoring the message-only `ArgumentException` construction in `Ensure`
  - restoring `ArgumentNullException` for null assembly entries in CQRS registration
- Revalidated with:
  - `dotnet test GFramework.Core.Tests -c Release --filter "FullyQualifiedName~GetString_WithUnknownCompactFormatterArgs_ShouldIgnoreUnknownOptions|FullyQualifiedName~GetString_WithVariable_ShouldFormatCorrectly|FullyQualifiedName~GetString_WithMultipleVariables_ShouldFormatCorrectly|FullyQualifiedName~GetString_WithInvalidCompactFormatterArgs_ShouldFallbackToDefaultFormatting|FullyQualifiedName~GetString_WithCompactFormatterArgs_ShouldApplyOptions|FullyQualifiedName~GetString_WithCompactFormatter_ShouldFormatCorrectly|FullyQualifiedName~Ensure_Should_Create_ArgumentException_With_Message|FullyQualifiedName~RegisterCqrsHandlersFromAssemblies_WithNullAssemblyItem_Should_ThrowArgumentNullException"`
- Result:
  - `8 Passed`, `0 Failed`, `0 Skipped`

### Stage: Review Follow-Up

- Applied a focused batch of review-driven fixes across `GFramework.Core`, `GFramework.Cqrs`, `GFramework.Godot`,
  `GFramework.Game`, and the paired docs:
  - completed missing XML exception contracts for public APIs such as `UiPageBehaviorFactory.Create<T>` and
    `ResourceManager.LoadAsync<T>` / `PreloadAsync<T>`
  - aligned `NumericExtensions.Between` with its documented null contract
  - removed duplicate XML documentation on `WaitForTask`
  - made `Result` safe for `default(Result)` in equality, hashing, and string formatting
  - restored Godot main-thread affinity by removing `ConfigureAwait(false)` from scene/module paths that touch node APIs
  - observed async destroy failures in `AbstractArchitecture` instead of discarding them silently
  - made `ConfigurableLoggerFactory.Dispose` idempotent under concurrent calls and switched logger-level lookup to
    longest-prefix matching
  - corrected CQRS weak-cache XML exception docs and weak-type-pair cache remarks
  - changed `IApplyAbleSettings.Apply` to `ApplyAsync` and updated implementations, callers, tests, and settings docs
  - fixed `GodotYamlConfigEnvironment` so Godot-path byte reads now throw on engine-reported read/open errors instead of
    silently returning an empty byte array

### Stage: Review Follow-Up Validation

- Ran:
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NumericExtensionsTests|FullyQualifiedName~ResultTests|FullyQualifiedName~ResultExtensionsTests|FullyQualifiedName~LoggingConfigurationTests"`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~SettingsSystemTests|FullyQualifiedName~GodotLocalizationSettingsTests"`
  - `dotnet build GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release`
  - `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~LoadAsync_Should_Use_Globalized_Res_Directory_Directly_When_Running_In_Editor"`
- Results:
  - `GFramework.Core.Tests`: `101 Passed`, `0 Failed`
  - `GFramework.Game.Tests`: `7 Passed`, `0 Failed`
  - `GFramework.Godot.Tests` build: succeeded
  - targeted `GFramework.Godot.Tests` smoke test: `1 Passed`, `0 Failed`
- Validation caveat:
  - parallel `dotnet test` invocations against the Windows-backed worktree caused transient file-lock failures in
    shared `obj/bin` outputs, so the final validation was rerun serially.
  - a dedicated test for `GodotYamlConfigEnvironment.Default.ReadAllBytes("res://missing")` was attempted but removed
    because the native Godot file API crashes the test host in this environment before managed assertions can observe
    the failure.

### Current Remaining Hotspots

- `MA0051` long methods in:
  - `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs`
  - `GFramework.Core/Architectures/ArchitectureLifecycle.cs`
  - `GFramework.Core/Coroutine/CoroutineScheduler.cs`
  - `GFramework.Core/Pause/PauseStackManager.cs`
  - `GFramework.Core/StateManagement/Store.cs`
- `MA0048` file/type mismatches in generic/non-generic families and multi-type files such as:
  - `AbstractCommand*`
  - `AbstractAsyncCommand*`
  - `AbstractQuery*`
  - `EasyEventGeneric.cs`
- `MA0046` delegate-shape warnings in architecture/coroutine/logging callbacks.
- `MA0016` collection-abstraction warnings on some public logging configuration properties.

### Immediate Next Step

1. Stop at `ANALYZER-WARNING-REDUCTION-RP-001` for this batch unless the next round explicitly wants to take on the
   remaining structural refactors.
2. If the next round resumes from this point, keep the restored localization / `Ensure` / CQRS registration test
   contracts intact while tackling the remaining analyzer hotspots.

### Stage: Review Follow-Up Batch 2

- Applied the current CodeRabbit follow-up fixes across `GFramework.Godot`, `GFramework.Core`, `GFramework.Cqrs`, and
  `GFramework.Game.Tests`:
  - moved `AbstractArchitecture.InstallGodotModule` anchor validation ahead of `module.Install(this)` so a missing
    `SceneTree` anchor fails before any module-side effects occur
  - normalized `ConfigurableLoggerFactory` configuration collections when JSON deserialization yields `null`
  - merged `GetLogger(..., minLevel)` with `_config.MinLevel` using the stricter level and updated the XML contract
  - added a compatibility disposal path for `AsyncLogAppender`
  - made `GodotYamlConfigEnvironment` return `null` for inaccessible non-Godot directories and always call
    `ListDirEnd()` for Godot directory iteration
  - completed the missing XML exception contract on `WeakTypePairCache.GetValueOrDefaultForTesting`
  - added XML summaries to the modified public `GodotLocalizationSettingsTests` methods
- Added focused regression tests for:
  - `ConfigurableLoggerFactory` null-collection normalization
  - caller `minLevel` lower-bound behavior
  - factory disposal of `AsyncLogAppender`
- Planned validation for this batch:
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ConfigurableLoggerFactoryTests|FullyQualifiedName~LoggingConfigurationTests"`
  - `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~LoadAsync_Should_Use_Globalized_Res_Directory_Directly_When_Running_In_Editor"`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GodotLocalizationSettingsTests"`
  - `dotnet build GFramework.sln -c Release`
- Attempted to add a direct `AbstractArchitecture` regression test for the anchor-ordering change, but the current
  Godot .NET test host crashes when initializing that runtime path. Removed the unstable test and kept the code change
  covered by project build plus existing Godot loader smoke validation instead.

### Stage: Review Follow-Up Batch 2 Validation

- First attempted to run the targeted tests and solution build in parallel, but the Windows-backed worktree reproduced
  the known `obj/bin` file-lock failures (`CS2012`, `MSB3026`, `MSB3883`). Switched all remaining validation to serial
  invocations with `-m:1`.
- Ran:
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ConfigurableLoggerFactoryTests" -m:1`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GodotLocalizationSettingsTests" -m:1`
  - `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~LoadAsync_Should_Use_Globalized_Res_Directory_Directly_When_Running_In_Editor" -m:1`
  - `dotnet build GFramework.sln -c Release -m:1`
- Results:
  - `GFramework.Core.Tests`: `3 Passed`, `0 Failed`
  - `GFramework.Game.Tests`: `3 Passed`, `0 Failed`
  - `GFramework.Godot.Tests` targeted smoke: `1 Passed`, `0 Failed`
  - `GFramework.sln` release build: `698 Warning(s)`, `0 Error(s)`

### Stage: Review Follow-Up Batch 3

- Applied the latest review-driven fixes across `GFramework.Godot`, `GFramework.Core`, `GFramework.Cqrs`, and
  `GFramework.Core.Tests`:
  - recorded Godot modules in `_extensions` immediately after `module.Install(this)` so failed attach paths still
    participate in later teardown
  - tightened `ConfigurableLoggerFactory.GetLogger` with explicit `name` null validation
  - aligned the `minLevel` XML contract with the actual override precedence semantics
  - added inline comments to the two-level weak cache `GetOrAdd` hot path
  - replaced the reflection-based `AsyncLogAppender` disposal test with an observable post-disposal logger behavior
    assertion
- Added a stable Godot-specific regression test that directly invokes `InstallGodotModule(...)` without going through
  `Initialize()`, avoiding the native crash path while still covering the anchor-before-install contract.
- Documented the remaining test gap:
  - the destroy-observation path introduced by `ObserveDestroyAsync` still lacks a dedicated automated assertion because
    capturing `GD.PushError` deterministically in the current Godot .NET test host is not yet practical.

### Stage: Review Follow-Up Batch 3 Validation

- Ran:
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ConfigurableLoggerFactoryTests" -m:1`
  - `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~AbstractArchitectureModuleInstallationTests" -m:1`
  - `dotnet build GFramework.sln -c Release -m:1`
- Results:
  - `GFramework.Core.Tests`: `5 Passed`, `0 Failed`
  - `GFramework.Godot.Tests` targeted install-ordering test: `1 Passed`, `0 Failed`
  - `GFramework.sln` release build: `847 Warning(s)`, `0 Error(s)`

### Stage: Review Follow-Up Batch 4

- Applied the latest review-driven fixes across `GFramework.Core`, `GFramework.Core.Tests`, and the Godot settings docs:
  - made `ConfigurableLoggerFactory` reject `appenders` entries deserialized as `null` with an explicit
    `InvalidOperationException` instead of deferring to an unclear downstream failure
  - extended the constructor XML docs to describe that validation contract explicitly
  - added a focused regression test for `appenders: [null]`
  - fixed the `docs/zh-CN/godot/setting.md` audio examples so both `SetMasterVolume` samples compile while awaiting
    `audioSettings.ApplyAsync()`

### Stage: Review Follow-Up Batch 4 Validation

- Ran:
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ConfigurableLoggerFactoryTests" -m:1`
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -m:1`
- Results:
  - `GFramework.Core.Tests`: `6 Passed`, `0 Failed`
  - `GFramework.Core` release build: `79 Warning(s)`, `0 Error(s)`
