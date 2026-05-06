# MicrosoftDiContainer Disposal Trace

## 2026-05-06

### MDC-DISPOSE-RP-001 Issue #327 disposal repair

- Trigger:
  - issue `#327` reports that `MicrosoftDiContainer` holds a `ReaderWriterLockSlim` and a frozen `IServiceProvider`
    but never releases either resource explicitly
  - CQRS benchmark types keep `MicrosoftDiContainer` fields alive across runs and currently only dispose the MediatR
    `ServiceProvider` side
  - `RequestStartupBenchmarks` also creates temporary GFramework runtimes whose backing containers are never surfaced
    for cleanup
- Decisions:
  - treat the fix as a container lifetime contract update, not only a benchmark workaround
  - add the disposal contract at the `IIocContainer` abstraction so callers holding interface references can release the
    container explicitly
  - keep runtime ownership unchanged; benchmarks that create containers remain responsible for disposing them
- Implementation notes:
  - `MicrosoftDiContainer` now releases its frozen root `IServiceProvider`, clears registration state, disposes the
    internal `ReaderWriterLockSlim`, and rejects all later operations with `ObjectDisposedException`
  - `RequestStartupBenchmarks` was rewritten so the steady-state runtime keeps an explicit container field and the
    cold-start benchmark disposes its temporary container in the same measured invocation
  - other benchmark classes that own `MicrosoftDiContainer` fields now dispose them during `GlobalCleanup`
- Validation milestone:
  - `python3 scripts/license-header.py --check` passed
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainer|FullyQualifiedName~IocContainerLifetimeTests"` passed (`55/55`)
  - `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release` passed with `0 warnings / 0 errors`
  - `dotnet build GFramework.sln -c Release` passed with `0 warnings / 0 errors`
- Immediate next step:
  - archive the topic and push the branch
