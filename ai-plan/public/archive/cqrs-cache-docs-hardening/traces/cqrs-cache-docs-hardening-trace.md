# CQRS Cache And Docs Hardening Trace

## 2026-04-17

### Stage: Discovery

- Read `AGENTS.md` and `.ai/environment/tools.ai.yaml` before selecting repository tools.
- Confirmed `README.md` quick-install guidance omits `GeWuYou.GFramework.Cqrs` and
  `GeWuYou.GFramework.Cqrs.Abstractions` even though the module overview recommends CQRS as a first-class module.
- Confirmed `docs/zh-CN/source-generators/index.md` lists the split CQRS generator package but does not provide a
  dedicated CQRS handler-registry section with package wiring, minimal usage, or compatibility notes.
- Confirmed `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` has a cross-container metadata-cache test that
  only asserts reflection/attribute call counts and does not prove the second container still receives registrations.
- Confirmed `GFramework.Cqrs/Internal/CqrsDispatcher.cs` and
  `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` currently use process-wide `ConcurrentDictionary<Type, ...>` /
  `ConcurrentDictionary<Assembly, ...>` caches that strongly retain collectible types and assemblies.
- Reviewed `docs/zh-CN/getting-started/installation.md` as a related setup entry page; it also omits the CQRS runtime
  packages from the installation snippets.

### Current Recovery Point

- `CQRS-CACHE-DOCS-HARDENING-RP-001`

### Immediate Next Step

1. Update the tracking document and then patch the docs, runtime caches, and tests in one pass.

### Stage: Implementation

- Updated `README.md` quick-install guidance to include:
  - `GeWuYou.GFramework.Cqrs`
  - `GeWuYou.GFramework.Cqrs.Abstractions`
- Updated `docs/zh-CN/getting-started/installation.md` to add CQRS runtime package rows and installation snippets, and
  corrected the installation verification sample to the current `GFramework.Core.Architectures` /
  `OnInitialize()` / `OnInit()` API shape.
- Updated `docs/zh-CN/core/cqrs.md` to add explicit CQRS package wiring and document the current namespace split
  between message base types (`GFramework.Cqrs.*`) and handler base types (`GFramework.Cqrs.Cqrs.*`).
- Updated `docs/zh-CN/source-generators/index.md` to add a dedicated `CQRS Handler Registry 生成器` section covering:
  - required runtime and generator packages
  - a minimal working architecture example
  - compatibility and migration notes for reflection fallback vs generated registries
- Added `GFramework.Cqrs/Internal/WeakKeyCache.cs` with unload-aware weak-key cache helpers built on
  `ConditionalWeakTable`.
- Replaced the CQRS runtime's process-wide strong-reference type/assembly caches with weak-key caches in:
  - `GFramework.Cqrs/Internal/CqrsDispatcher.cs`
  - `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs`
- Expanded `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` so the cross-container metadata-cache test now
  proves both containers still resolve the expected handlers after the cache is reused.
- Updated `GFramework.Cqrs.Tests/Cqrs/CqrsDispatcherCacheTests.cs` to assert cache reuse through observable cached
  entries instead of assuming `ConcurrentDictionary`-specific count semantics.
- Evaluated the suggestion to change handler doc examples from `using GFramework.Cqrs.Cqrs.Command;` to
  `using GFramework.Cqrs.Command;`. The repository's actual handler base types still live under
  `GFramework.Cqrs.Cqrs.*`, so the implementation kept the real API and clarified the namespace split in the docs
  instead of documenting a non-existent namespace.

### Stage: Validation

- Ran:
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
- Result:
  - passed, `62` tests passed
- The first validation run surfaced one nullable warning in `GFramework.Cqrs/Internal/WeakKeyCache.cs` because the
  nested weak cache lookup did not prove the secondary cache was non-null to the compiler.

### Stage: Validation Follow-up

- Fixed `WeakTypePairCache<TValue>.TryGetValue(...)` to guard the nested weak cache reference explicitly.
- Re-ran:
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore`
- Result:
  - passed, `62` tests passed

### Immediate Next Step

1. Stop at `CQRS-CACHE-DOCS-HARDENING-RP-001` unless broader solution-level validation is requested.

### Stage: Minor Follow-up

- Updated `docs/zh-CN/core/cqrs.md` again so the standalone handler snippet now imports both:
  - `GFramework.Cqrs.Command`
  - `GFramework.Cqrs.Cqrs.Command`
- Added XML documentation to the private nested binding types in `GFramework.Cqrs/Internal/CqrsDispatcher.cs` so the
  intent of the cached service-type/delegate bundles is explicit to future maintainers.
- Restored the explicit `using System.Runtime.CompilerServices;` directive at the top of
  `GFramework.Cqrs/Internal/WeakKeyCache.cs` so `ConditionalWeakTable<,>` does not rely on ambient imports.
- Added a stateful `WeakKeyCache<TKey, TValue>.GetOrAdd<TState>(...)` overload and updated
  `WeakTypePairCache<TValue>.GetOrAdd(...)` to use it, removing the captured lambda allocation from the secondary-key
  lookup path.
- Updated both `WeakKeyCache<TKey, TValue>.GetOrAdd(...)` XML comments to state the full contract:
  `valueFactory` itself must be non-null, and it must not produce a null cache value.
