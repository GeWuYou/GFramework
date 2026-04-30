# CQRS 重写迁移追踪

## 2026-04-30

### 阶段：generated request invoker provider 最小落地（CQRS-REWRITE-RP-067）

- 继续按 `gframework-batch-boot 50` 执行，基线仍为本地现有 `origin/main`
- 在 `RP-066` 提交后复算 branch diff，相对 `origin/main` 增长到 `22 files`，仍明显低于 `50 files` stop condition，因此继续下一批
- 本轮 critical path 保持在主线程，本地完成 `dispatch/invoker` 生成前移的最小 request 切片；尝试委派 source-generator 测试给 worker 时因 subagent 名额已满失败，因此主线程直接接管该测试修改
- 本轮关键设计调整：
  - 不按 `requestType.Assembly` 做 provider 发现，避免“请求定义在 A、handler 与 generated registry 在 B”时漏掉 generated invoker
  - generated registry 若实现 `ICqrsRequestInvokerProvider`，registrar 会在激活 registry 后把 provider 注册进容器，并通过 `IEnumeratesCqrsRequestInvokerDescriptors` 把描述符写入 dispatcher 的进程级弱缓存
  - dispatcher 首次创建 request dispatch binding 时只按 `requestType + responseType` 读取静态弱缓存，不依赖具体容器实例；未命中时仍走既有反射创建路径
- 已完成实现：
  - `GFramework.Cqrs` 新增 `ICqrsRequestInvokerProvider`、`IEnumeratesCqrsRequestInvokerDescriptors`、
    `CqrsRequestInvokerDescriptor` 与 `CqrsRequestInvokerDescriptorEntry`
  - `CqrsHandlerRegistrar` 现会识别 generated registry 的 request invoker provider 能力，并登记 provider 与 request invoker 描述符
  - `CqrsDispatcher` 新增 generated request invoker 弱缓存，并在 request binding 创建时优先消费该元数据
  - `CqrsHandlerRegistryGenerator` 在 runtime 合同可用时，会让 generated registry 额外实现 request invoker provider 相关接口，并发射 descriptor 列表、`TryGetDescriptor(...)`、`GetDescriptors()` 与 request invoker 静态方法
- 已补充测试：
  - `CqrsGeneratedRequestInvokerProviderTests` 锁定 registrar 会注册 generated request invoker provider，且 dispatcher 走 generated invoker 后会返回 `generated:` 前缀结果
  - `CqrsHandlerRegistryGeneratorTests` 锁定 generated source 会包含 request invoker provider 接口、descriptor 条目与 `InvokeRequestHandler0(...)` 方法

### 验证（RP-067）

- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests|FullyQualifiedName~CqrsHandlerRegistrarTests|FullyQualifiedName~CqrsDispatcherCacheTests"`
  - 结果：通过，`22/22` passed
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过，`1/1` passed
- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

### 当前下一步（RP-067）

1. 评估 notification / stream invoker 是否值得沿同一 provider 模式继续前移，或先补 request provider 的公开说明与诊断语义
2. 继续在保持 branch diff 低于阈值的前提下推进下一批；当前相对 `origin/main` 的 branch diff 为 `22 files`

### 阶段：LegacyICqrsRuntime compatibility slice 收口（CQRS-REWRITE-RP-066）

- 继续按 `gframework-batch-boot 50` 执行，基线仍为本地现有 `origin/main`
- 在 `RP-065` 之后复算 branch diff，相对 `origin/main` 仍为 `19 files`，明显低于 `50 files` stop condition，因此继续下一批
- 本轮按“关键路径本地、非冲突文档委派”的方式拆成两个切片：
  - worker：`GFramework.Core.Abstractions/README.md`、`docs/zh-CN/abstractions/core-abstractions.md`、`docs/zh-CN/core/cqrs.md`
  - 主线程：`GFramework.Core/Services/Modules/CqrsRuntimeModule.cs`、`GFramework.Tests.Common/CqrsTestRuntime.cs`、`GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs`
- 接受只读 subagent 结论后，将 `LegacyICqrsRuntime` 定位为“容器兼容层”，明确本轮不删除别名、不改 dispatcher 主体、不与旧 `Command` / `Query` API 清理混做
- 主线程已完成：
  - `CqrsRuntimeModule` 把 legacy alias 注册收敛到 `RegisterLegacyRuntimeAlias(...)` helper，并在 XML 文档里明确新旧服务类型解析到同一 runtime 实例
  - `CqrsTestRuntime.RegisterInfrastructure(...)` 现也通过同名 helper 补齐 legacy alias；当容器只预注册正式 `ICqrsRuntime` seam 时，会在幂等接线时回填旧命名空间 alias
  - `MicrosoftDiContainerTests` 新增 `RegisterInfrastructure_Should_Backfill_Legacy_Cqrs_Runtime_Alias_With_The_Same_Instance`，锁定“只存在正式 seam 时也会补旧 alias，且两者仍指向同一实例”的兼容合同
- worker 已完成文档收口：
  - `GFramework.Core.Abstractions/README.md`
  - `docs/zh-CN/abstractions/core-abstractions.md`
  - `docs/zh-CN/core/cqrs.md`
  - 三处文档都已明确：`GFramework.Core.Abstractions.Cqrs.ICqrsRuntime` 只是旧命名空间下保留的 compatibility alias，新代码应依赖 `GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime`

### 验证（RP-066）

- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainerTests"`
  - 结果：通过，`42/42` passed
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

### 当前下一步（RP-066）

1. 在保持 branch diff 低于阈值的前提下，回到 `dispatch/invoker` 生成前移主线
2. 优先尝试只覆盖 request 路径的 generated invoker/provider 最小切片，避免一次卷入 notification / stream / pipeline executor
3. 下一次 batch 结束后继续复算 branch diff，确认距 `50 files` stop condition 的剩余 headroom

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

### 验证（RP-065）

- `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureContextTests"`
  - 结果：通过，`22/22` passed

### 当前下一步（RP-065）

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

### 验证（RP-064）

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

### 当前下一步（RP-064）

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

### 验证（RP-063）

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
