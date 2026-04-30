# CQRS 重写迁移追踪

## 2026-04-30

### 阶段：测试命名收口与 ArchitectureContext lazy-resolution 回归（CQRS-REWRITE-RP-065）

- 继续按 `gframework-batch-boot 50` 执行，基线仍为本地现有 `origin/main`
- `22f608eb` 之后复算 branch diff，相对 `origin/main` 已达到 `18 files`，仍明显低于 `50 files` stop condition，因此继续下一批
- 本轮拆成四个互不冲突切片：
  - worker 1：`MediatorAdvancedFeaturesTests.cs`
  - worker 2：`MediatorArchitectureIntegrationTests.cs`
  - worker 3：`MediatorComprehensiveTests.cs`
  - 主线程：`GFramework.Core.Tests/Architectures/ArchitectureContextTests.cs`
- 三个 worker 均只收口单文件命名与注释语义，并把测试文件迁移到 `GFramework.Cqrs.Tests/Cqrs/`
- 主线程新增 `ArchitectureContextTests` 并发 lazy-resolution 回归，锁定：
  - `PublishAsync(...)` 在并发首次访问时只解析一次 `ICqrsRuntime`
  - `CreateStream(...)` 在并发首次访问时只解析一次 `ICqrsRuntime`
- 集成后已确认三份测试文件中不再残留 `GFramework.Cqrs.Tests.Mediator` 命名空间或 `Mediator` 语义命名

### 验证

- `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureContextTests"`
  - 结果：通过，`22/22` passed

### 当前下一步

1. 继续 `Phase 8` 主线，回到 `dispatch/invoker` 生成前移或 `LegacyICqrsRuntime` 收口的下一个低风险切片
2. 在下一次 batch 结束后复算 branch diff，确认距 `50 files` stop condition 的剩余 headroom

### 阶段：notification publisher seam 最小落地（CQRS-REWRITE-RP-064）

- 本轮按 `gframework-batch-boot 50` 继续 `cqrs-rewrite`，基线使用本地现有 `origin/main`
- 当前 branch diff 相对 `origin/main` 开始时仅 `3 files / 164 lines`，远低于 `50 files` stop condition，因此继续推进真实代码切片
- 主线程锁定 `notification publisher seam` 为本轮最低风险高收益切片，并保持关键路径在本地实现
- 接受两条只读 subagent 结论：
  - 对照 `ai-libs/Mediator` 后，只吸收 notification publisher 策略接缝，不在本轮引入并行 publisher、异常聚合或公开配置面
  - 现有仓库测试需要锁定的兼容语义是：零处理器静默完成、顺序执行、首错即停、上下文逐次注入
- 已完成实现：
  - `GFramework.Cqrs` 新增 `INotificationPublisher`、`NotificationPublishContext<TNotification>`、
    `DelegatingNotificationPublishContext<TNotification, TState>` 与默认 `SequentialNotificationPublisher`
  - `CqrsDispatcher.PublishAsync(...)` 改为解析 handlers 后构造发布上下文，并委托给 publisher seam 执行
  - `CqrsRuntimeFactory`、`CqrsRuntimeModule` 与 `GFramework.Tests.Common.CqrsTestRuntime` 现会在 runtime 创建前复用容器里已注册的 `INotificationPublisher`
  - `GFramework.Cqrs.Tests` 新增 `CqrsNotificationPublisherTests`，覆盖自定义 publisher、上下文注入、零处理器、首错即停与默认接线复用
  - `GFramework.Cqrs/README.md` 与 `docs/zh-CN/core/cqrs.md` 已同步说明默认通知语义与可替换 seam
- 中途验证曾因并行 .NET 构建产生输出文件锁噪音；已改为串行重跑并获取干净结果

### 验证

- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsNotificationPublisherTests"`
  - 结果：通过，`5/5` passed
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainerTests"`
  - 结果：通过，`41/41` passed
- `GIT_DIR=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework/.git/worktrees/GFramework-cqrs GIT_WORK_TREE=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework-WorkTree/GFramework-cqrs bash scripts/validate-csharp-naming.sh`
  - 结果：通过

### 当前下一步

1. 评估 notification publisher seam 的第二阶段是否需要公开配置面、并行 publisher 或 telemetry decorator
2. 把 `dispatch/invoker` 生成前移重新拉回 `Phase 8` 主线，作为下一个实现切片

### 阶段：CQRS vs Mediator 评估归档（CQRS-REWRITE-RP-063）

- 本轮按用户要求使用 `gframework-boot` 启动上下文后，先完成 `cqrs-rewrite` 现状核对，再并行对照
  `GFramework.Cqrs` 与 `ai-libs/Mediator`
- 只读评估结论已归档到 `ai-plan/public/cqrs-rewrite/archive/todos/cqrs-vs-mediator-assessment-rp063.md`
- 本轮关键判断：
  - `GFramework.Cqrs` 已完成对外部 `Mediator` 作为生产 runtime 依赖的替代
  - 当前尚未完成的是仓库内部旧 `Command` / `Query` API、兼容 seam、fallback 旧语义与测试命名的收口
  - 当前已吸收 `Mediator` 的统一消息模型、generator 优先注册与热路径缓存思路
  - 当前仍未完整吸收 publisher 策略抽象、细粒度 pipeline、telemetry / diagnostics / benchmark 体系与 runtime 主体生成
- 本轮把默认下一步从“继续盯 PR thread”调整为“围绕 publisher seam 与 dispatch/invoker 生成前移做下一轮设计收敛”

### 验证

- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

## 活跃事实

- 当前主题仍处于 `Phase 8`
- 当前主题的主问题已从“是否完成外部依赖替代”转为“内部兼容层收口顺序与下一轮能力深化优先级”
- 已完成阶段的详细执行历史不再留在 active trace；默认恢复入口只保留当前恢复点、活跃事实、风险与下一步

## 当前风险

- 当前 `dotnet build GFramework.sln -c Release` 在 WSL 环境仍会受顶层 `GFramework.csproj` 的 Windows NuGet fallback 配置影响
- 若不把“生产替代完成”与“仓库内部收口完成”分开记录，后续很容易重复争论当前 CQRS 迁移是否已经完成

## Archive Context

- 当前评估归档：
  - `ai-plan/public/cqrs-rewrite/archive/todos/cqrs-vs-mediator-assessment-rp063.md`
- 历史 trace 归档：
  - `ai-plan/public/cqrs-rewrite/archive/traces/cqrs-rewrite-history-through-rp043.md`
  - `ai-plan/public/cqrs-rewrite/archive/traces/cqrs-rewrite-history-rp046-through-rp061.md`

## 当前下一步

1. 补一轮最小 Release 构建验证，确认本次 `ai-plan` 与评估文档更新未引入仓库级异常
2. 以 `notification publisher seam` 与 `dispatch/invoker` 生成前移为优先对象，形成下一轮可执行设计
