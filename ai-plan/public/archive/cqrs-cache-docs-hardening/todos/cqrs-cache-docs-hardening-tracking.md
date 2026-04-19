# CQRS Cache And Docs Hardening Tracking

## Goal

Address the current CQRS follow-up issues across runtime caches, tests, and user-facing setup/integration
documentation so the repository state matches the published onboarding path and the runtime remains safe for
collectible assemblies.

## Current Recovery Point

- Recovery point: `CQRS-CACHE-DOCS-HARDENING-RP-001`
- Current phase: `Phase 2`
- Active focus:
  - validation completed for the targeted CQRS test project
  - the repository can resume from documentation follow-up or broader solution validation if needed

## Planned Work

- [x] Create the matching execution trace under `ai-plan/public/archive/cqrs-cache-docs-hardening/traces/`.
- [x] Update `README.md` quick-install guidance to include CQRS runtime packages.
- [x] Update the related Chinese setup/integration docs with CQRS runtime + source-generator wiring and a minimal
      working CQRS generator example.
- [x] Replace static `ConcurrentDictionary<Assembly, ...>` / `ConcurrentDictionary<Type, ...>` CQRS caches with
      weak-key caches that do not permanently root collectible assemblies or handler/message types.
- [x] Document the weak-cache thread-safety assumptions and recomputation behavior in code comments/XML docs.
- [x] Expand CQRS tests to assert that cached metadata still results in successful registration in every container.
- [x] Adapt dispatcher cache tests to the new unload-aware cache structure.
- [x] Run targeted CQRS test validation and capture the results here and in the trace.

## Validation

Executed:

```bash
dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release
dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore
```

Results:

- `dotnet test ... -c Release`: passed, `62` tests passed.
- `dotnet test ... -c Release --no-restore`: passed, `62` tests passed.
- The first run exposed one nullable warning in `GFramework.Cqrs/Internal/WeakKeyCache.cs`; the follow-up fixed the
  `WeakTypePairCache.TryGetValue(...)` null-flow and reran the full CQRS test project cleanly.
- The CQRS doc update intentionally did not rename handler examples from `GFramework.Cqrs.Cqrs.*` to
  `GFramework.Cqrs.*`, because the current public handler base types still live under the double-`Cqrs` namespace.
  Instead, the docs now explain the split between message namespaces and handler namespaces explicitly.
- A later follow-up added the missing `using GFramework.Cqrs.Command;` to the handler snippet so that the block can be
  copied independently, documented the private dispatch-binding types in `CqrsDispatcher`, restored the explicit
  `System.Runtime.CompilerServices` import in `WeakKeyCache.cs`, and removed the `WeakTypePairCache.GetOrAdd(...)`
  closure allocation by routing the secondary-key factory through a stateful overload.
- A final documentation cleanup aligned the `WeakKeyCache<TKey, TValue>.GetOrAdd(...)` XML comments with the runtime
  contract by stating that `valueFactory` must not return `null`.

## Known Risks

- `ConditionalWeakTable`-based caches trade deterministic retention for unload safety, so the implementation must make
  the recomputation semantics explicit and keep hot-path lookups thread-safe.
- The source-generator docs already cover multiple generator families; adding CQRS guidance must stay aligned with the
  actual runtime APIs and package split.
- The existing CQRS docs mix conceptual snippets and minimal examples; changes should improve correctness without
  silently inventing APIs that the current packages do not expose.

## Recommended Resume Step

1. If broader regression confidence is needed, run solution-level CQRS/Core validation next.
2. If the public CQRS handler namespaces are flattened in a future refactor, update the docs again in the same change.
