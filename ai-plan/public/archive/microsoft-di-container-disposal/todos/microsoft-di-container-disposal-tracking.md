# MicrosoftDiContainer Disposal Tracking

## Goal

Fix issue `#327` by making `MicrosoftDiContainer` explicitly disposable, ensuring frozen service providers and lock
state are released deterministically, and updating CQRS benchmarks so every owned container is disposed in cleanup or
cold-start paths.

## Current Recovery Point

- Recovery point: `MDC-DISPOSE-RP-001`
- Phase: completed and ready to archive
- Focus:
  - keep the final validated implementation and archive handoff concise

## Active Risks

- No active implementation blockers remain after validation.

## Completed In This Stage

- Extended `IIocContainer` to inherit `IDisposable` so callers holding the abstraction can release container-owned
  resources explicitly.
- Implemented `MicrosoftDiContainer.Dispose()` with idempotent root-provider release, lock cleanup, state clearing, and
  `ObjectDisposedException` guards for post-disposal access.
- Updated `Clear()` to dispose the currently built root provider before resetting container state.
- Added Core regression tests that verify resolved DI-owned singletons are disposed and that disposed containers reject
  further registration, lookup, and scope creation.
- Fixed CQRS benchmark cleanup so every benchmark-owned `MicrosoftDiContainer` is disposed, including the temporary
  cold-start container path in `RequestStartupBenchmarks`.

## Validation Target

- `python3 scripts/license-header.py --check`
- `dotnet test GFramework.Core.Tests -c Release --filter "FullyQualifiedName~MicrosoftDiContainer|FullyQualifiedName~IocContainerLifetimeTests"`
- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
- `dotnet build GFramework.sln -c Release`

## Latest Validation Result

- `python3 scripts/license-header.py --check` passed on 2026-05-06.
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainer|FullyQualifiedName~IocContainerLifetimeTests"` passed on 2026-05-06 with `55` tests passed.
- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release` passed on 2026-05-06 with `0 warnings` and `0 errors`.
- `dotnet build GFramework.sln -c Release` passed on 2026-05-06 with `0 warnings` and `0 errors`.

## Next Recommended Resume Step

Archive this topic under `ai-plan/public/archive/` and push the fix branch for review.
