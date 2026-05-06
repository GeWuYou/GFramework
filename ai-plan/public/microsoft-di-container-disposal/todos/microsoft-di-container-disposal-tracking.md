# Microsoft DI Container Disposal 跟踪

## 目标

围绕 `PR #330` 收敛 `MicrosoftDiContainer` 的释放契约、并发释放竞态，以及 `GFramework.Cqrs.Benchmarks` 的宿主清理鲁棒性。

## 当前恢复点

- 恢复点编号：`MICROSOFT-DI-DISPOSAL-RP-001`
- 当前阶段：`Phase 1`
- 当前 PR 锚点：`PR #330`
- 当前结论：
  - `$gframework-pr-review` 已确认 latest-head review 仍存在 5 条 open AI thread，其中 `IIocContainer` 文档契约、`MicrosoftDiContainer.Clear()` 的不可达释放逻辑、`Dispose()` 并发竞态，以及 benchmark `Cleanup()` 缺乏异常隔离均已在本地补齐
  - `CodeRabbit` 关于 `GFramework.Cqrs.Benchmarks` 的 cleanup 问题虽然标在单个文件上，但同类模式实际覆盖 `RequestBenchmarks`、`NotificationBenchmarks`、`RequestPipelineBenchmarks`、`RequestStartupBenchmarks`、`StreamingBenchmarks`、`RequestInvokerBenchmarks`、`StreamInvokerBenchmarks`，当前已通过共享 helper 一次性收敛
  - `MicrosoftDiContainer.Dispose()` 现会先对外发布 `_disposed` 状态并释放写锁，让等待线程统一抛出容器级 `ObjectDisposedException`；随后仅在锁静默后才销毁底层 `ReaderWriterLockSlim`

## 当前活跃事实

- 当前分支：`fix/microsoft-di-container-disposal`
- 当前分支对应 `PR #330`，状态为 `OPEN`
- 已决定的最小修复面：
  - `GFramework.Core.Abstractions/Ioc/IIocContainer.cs`
  - `GFramework.Core/Ioc/MicrosoftDiContainer.cs`
  - `GFramework.Core.Tests/Ioc/IocContainerLifetimeTests.cs`
  - `GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs`
  - `GFramework.Cqrs.Benchmarks/Messaging/*.cs` 的 7 个 benchmark cleanup

## 当前风险

- 若极端情况下存在长时间不退出的遗留 waiter，`DisposeLockWhenQuiescent()` 会在有限自旋后跳过底层锁销毁并记录警告，以优先保证 `Dispose()` 不被无限阻塞
- 并发释放回归测试依赖对内部 `_lock` 的反射访问，需要保持断言目标明确，避免把实现细节暴露成对外契约

## 最近权威验证

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过
  - 备注：确认当前分支对应 `PR #330`，open AI review 重点已收敛到释放契约、并发竞态和 benchmark cleanup
- `python3 scripts/license-header.py --check`
  - 结果：通过
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~IocContainerLifetimeTests|FullyQualifiedName~MicrosoftDiContainerTests"`
  - 结果：通过，`57/57` passed
- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

## 下一推荐步骤

1. 再次运行 `$gframework-pr-review` 或检查生成的 JSON，确认当前 latest-head open threads 是否只剩待推送的 GitHub 状态差异
2. 关注 push 后若仍有 review thread 未关闭，优先核对其是否属于 stale comment 还是需要额外文档/测试补充
