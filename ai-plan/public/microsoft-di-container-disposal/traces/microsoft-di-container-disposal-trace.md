# Microsoft DI Container Disposal 追踪

## 2026-05-06

### 阶段：PR #330 review triage 与修复面收敛（MICROSOFT-DI-DISPOSAL-RP-001）

- 使用 `$gframework-pr-review` 抓取当前分支对应的 `PR #330` latest-head review 后，主线程确认仍有效的 open AI 反馈集中在四类：
  - `IIocContainer` 缺少显式的释放生命周期文档
  - `MicrosoftDiContainer.Clear()` 在 `_frozen == false` 路径下仍保留不可达的 `_provider.Dispose()` 调用
  - `MicrosoftDiContainer.Dispose()` 会让等待中的读写线程泄露 `ReaderWriterLockSlim` 的 `ObjectDisposedException`
  - 多个 `GFramework.Cqrs.Benchmarks` cleanup 顺序释放资源但缺乏异常隔离，前一个 `Dispose()` 失败会阻断后续资源回收
- 本轮决策：
  - 先补 `ai-plan/public/microsoft-di-container-disposal` 的 tracking / trace，保证该跨模块 PR follow-up 有明确恢复入口
  - 通过 `EnterReadLockOrThrowDisposed` / `EnterWriteLockOrThrowDisposed` 收口 `MicrosoftDiContainer` 的等待中竞态，而不是零散修补个别 API
  - 通过共享 `BenchmarkCleanupHelper` 一次性收敛 benchmark 宿主 cleanup 的同类风险
- 实现补充：
  - `IIocContainer` 现已补充释放契约文档，明确 `Dispose()` 幂等性、根 `IServiceProvider` 与同步资源归属，以及释放后的统一异常语义
  - `MicrosoftDiContainer.Clear()` 已移除未冻结路径下不可达的 `_provider.Dispose()` 调用
  - `MicrosoftDiContainer.Dispose()` 现先发布 `_disposed`，再等待遗留 waiter 退场后释放底层锁；若锁在有限自旋内未静默，则记录 warning 并跳过锁销毁，避免 `Dispose()` 无限阻塞
  - `GFramework.Cqrs.Benchmarks` 新增 `BenchmarkCleanupHelper`，并统一接入 7 个 `GlobalCleanup` 入口
- 回归验证：
  - `Dispose_Should_Translate_Waiting_Readers_To_Container_ObjectDisposedException`
  - `Dispose_Should_Be_Idempotent_When_Called_Concurrently`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~IocContainerLifetimeTests|FullyQualifiedName~MicrosoftDiContainerTests"` 通过，`57/57` passed
  - `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release` 通过，`0 warning / 0 error`

### 当前下一步

1. 推送当前分支后重新运行 `$gframework-pr-review`，确认 latest-head open threads 是否已与本地修复对齐

### 阶段：收敛剩余并发 Dispose 双重锁销毁竞态（MICROSOFT-DI-DISPOSAL-RP-001）

- 根据用户补充的 `greptile` P1，重新核对 `MicrosoftDiContainer.Dispose()` 的尾部流程后确认还存在一个更窄的窗口：
  - 线程 A 与线程 B 都可能通过最外层 `_disposed` 快速路径
  - 线程 A 完成主释放并退出写锁后，线程 B 仍可能拿到写锁、因为 `_disposed == true` 直接返回，但 `finally` 仍会调用 `DisposeLockWhenQuiescent()`
  - 这样两个线程都可能执行 `_lock.Dispose()`；第二次调用会抛出 `ObjectDisposedException`
- 本轮修复决策：
  - 在 `DisposeLockWhenQuiescent()` 入口增加 `Interlocked.CompareExchange` 守卫，把底层锁销毁流程收敛为单次执行
  - 保持现有“先发布 `_disposed`、再等待 waiter 退场”的语义不变，只修复重复销毁底层锁的尾部竞态
  - 在 `IocContainerLifetimeTests` 增加更直接的回归断言，验证并发 `Dispose()` 后锁销毁启动标记只会变为 `1`
