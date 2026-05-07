# CQRS 重写迁移追踪

## 2026-05-07

### 阶段：PR #334 latest-head helper 异常边界收口（CQRS-REWRITE-RP-098）

- 再次使用 `$gframework-pr-review` 抓取 `feat/cqrs-optimization` 对应的 `PR #334` latest-head review，并重新核对 `/tmp/current-pr-review.json` 中最新 open thread：
  - 当前公开 PR 仍为 `PR #334`
  - `CodeRabbit` 最新 review 在 `2026-05-07T12:20:24Z` 为 `APPROVED`
  - latest-head 当前显示 `CodeRabbit 10` / `Greptile 5` 个 open thread
- 本轮逐条回到本地代码后，确认大多数 open thread 仍是 stale 状态；唯一继续成立的问题集中在 `LegacyCqrsDispatchHelper.TryResolveDispatchContext(...)`：
  - 该 helper 之前会把 `IContextAware.GetContext()` 抛出的任意 `InvalidOperationException` 都吞掉并回退到 legacy 直执行
  - 这会把真实运行时故障误判为“上下文未就绪”，导致 bridge 路径悄悄绕过统一 runtime，退化为难以诊断的行为差异
- 本轮主线程决策：
  - 将异常过滤收窄为只接受两类缺上下文信号：`Architecture context has not been set...` 与 `No active architecture context is currently bound.`
  - 其他 `InvalidOperationException` 一律继续向上传播，避免掩盖容器、生命周期或自定义 `GetContext()` 内的真实错误
  - 在 `CommandExecutorTests` 中新增两条回归：一条验证缺上下文时仍会 fallback 到 legacy 直执行；一条验证意外 `InvalidOperationException` 不会被 bridge 逻辑静默吞掉
  - 同步刷新 `cqrs-rewrite` active tracking，把本轮修复记录为新的恢复锚点 `RP-098`
- 本轮权威验证：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json`
    - 结果：通过
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
    - 结果：通过，`0 warning / 0 error`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：通过，`0 warning / 0 error`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~CommandExecutorTests|FullyQualifiedName~QueryExecutorTests"`
    - 结果：通过，`25/25` passed
  - `python3 scripts/license-header.py --check`
    - 结果：通过
  - `git diff --check`
    - 结果：通过

### 阶段：PR #334 nitpick 测试收尾（CQRS-REWRITE-RP-097）

- 继续处理 `PR #334` latest-head review 中仍值得本地吸收的轻量 nitpick，范围限定在 legacy bridge 测试可观察性与测试替身诊断质量：
  - `AsyncQueryExecutorTests.SendAsync_Should_Bridge_Through_Runtime_And_Preserve_Context` 标题声明“保留上下文”，但此前只断言了返回值与 bridge request 类型
  - `CommandExecutorTests.Send_WithResult_Should_Bridge_Through_Runtime_And_Preserve_Context` 同样缺少可观察的上下文注入断言
  - `RecordingCqrsRuntime` 直接强转响应对象，若测试工厂回错类型，失败信息不够聚焦
- 本轮主线程决策：
  - 为两个 “Preserve_Context” 用例补齐 `ObservedContext` 与 `expectedContext` 的同一实例断言，使测试标题、注释与断言对象保持一致
  - 让 `RecordingCqrsRuntime` 通过私有 helper 显式执行响应类型还原；当工厂返回 `null` 或错误装箱类型时，抛出包含 request 类型与期望/实际响应类型的 `InvalidOperationException`
  - 同步刷新 `cqrs-rewrite` active tracking，把本轮 nitpick 收敛与验证结果记录为新的恢复锚点 `RP-097`
- 本轮权威验证：
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：通过，`0 warning / 0 error`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CommandExecutorTests|FullyQualifiedName~AsyncQueryExecutorTests"`
    - 结果：通过，`19/19` passed
  - `python3 scripts/license-header.py --check`
    - 结果：通过
  - `git diff --check`
    - 结果：通过

### 阶段：PR #334 latest-head review 复核（CQRS-REWRITE-RP-096）

- 再次使用 `$gframework-pr-review` 抓取 `feat/cqrs-optimization` 对应的 `PR #334` latest-head review，并读取 `/tmp/current-pr-review.json` 中的 `review_agents`、`latest_commit_review`、`megalinter_report` 与 `test_reports`
- 本轮复核结论：
  - 当前公开 PR 为 `PR #334`，head commit 为 `dc3bd3744e2ceaa557ef03bc991fc88daedb460b`
  - `CodeRabbit` latest review 在 `2026-05-07T11:46:42Z` 已是 `APPROVED`，但 latest-head 仍显示 `10` 个 open thread；`Greptile` 仍显示 `3` 个 open thread
  - 逐条回到本地代码后，相关修复已在当前分支落地：`ArchitectureBootstrapper` 已自动扫描 `typeof(ArchitectureContext).Assembly`；`ArchitectureContextTests` / `ArchitectureModulesBehaviorTests` 已标注 `NonParallelizable` 并保证资源释放；`LegacyAsync*DispatchRequestHandler` 已统一补 `ThrowIfCancellationRequested()` + `WaitAsync(cancellationToken)`；`QueryExecutor` / legacy bridge request XML 文档与 `docs/zh-CN/core/command.md` fallback 说明也已齐备
  - 远端 CTRF 最新测试汇总为 `2311/2311 passed`（run `#1079`），`MegaLinter` 仅剩 `dotnet-format` restore failed 的环境噪音，没有新的文件级诊断
- 主线程决策：
  - 不再为这些 stale open thread 追加新的本地代码改动，避免重复修补已吸收的问题
  - 仅更新 `cqrs-rewrite` active tracking/trace，把“当前剩余差异主要是 GitHub thread 状态滞后”记录为最新权威事实
- 本轮权威验证：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
    - 结果：通过
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
    - 结果：通过，`0 warning / 0 error`

### 阶段：PR #334 legacy bridge sync follow-up（CQRS-REWRITE-RP-095）

- 再次使用 `$gframework-pr-review` 抓取 `feat/cqrs-optimization` 对应的 `PR #334` latest-head review，并只保留本地复核后仍成立的问题：
  - `QueryExecutor` / `CommandExecutor` 新增的同步 bridge 仍直接阻塞 `ICqrsRuntime.SendAsync(...)`，在调用方存在 `SynchronizationContext` 时容易放大 sync-over-async 死锁面
  - `QueryExecutor` / `CommandExecutor` / `AsyncQueryExecutor` 各自保留一份相同的 dispatch-context 解析逻辑，仍有漂移风险
  - `ArchitectureContextTests` 的 bridge fixture 依然共享静态 tracker 且未显式声明非并行；冻结容器所有权也未交还给调用方释放
  - `LegacyAsyncCommandDispatchRequestHandler` 仍未沿用另两个 async bridge handler 的取消可见性模式
- 本轮主线程决策：
  - 新增 `GFramework.Core/Cqrs/LegacyCqrsDispatchHelper.cs`，统一收口 legacy bridge 的 dispatch-context 解析，以及同步 bridge 对 `ICqrsRuntime.SendAsync(...)` 的线程池隔离等待
  - 将 `QueryExecutor`、`CommandExecutor`、`AsyncQueryExecutor` 的重复 helper 改为复用共享 helper，并把 `ArchitectureContext` 的同步 CQRS 包装入口一并切换到同一阻塞策略，避免留下半修状态
  - 为 `ICqrsRuntime.SendAsync(...)` 补充 `<remarks>`，显式说明 legacy 同步入口会在后台线程上等待该异步契约，处理链路不应依赖调用方 `SynchronizationContext`
  - 把 `ArchitectureContextTests`、`ArchitectureModulesBehaviorTests` 标记为 `NonParallelizable`，并让 `CreateFrozenBridgeContext(...)` 把冻结容器通过 `out` 参数返还给每个测试在 `finally` 中释放
  - 为 `LegacyAsyncCommandDispatchRequestHandler` 增补 `ThrowIfCancellationRequested()` + `WaitAsync(cancellationToken)`，与另外两个 async bridge handler 保持一致
  - 新增回归测试覆盖同步 bridge 的 `SynchronizationContext` 隔离、legacy async command handler 的取消语义，以及 async/sync bridge request 的 request-type 命中
- 本轮权威验证：
  - `env GIT_DIR=... GIT_WORK_TREE=... python3 scripts/license-header.py --check`
    - 结果：通过
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
    - 结果：通过，`0 warning / 0 error`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureContextTests|FullyQualifiedName~ArchitectureModulesBehaviorTests|FullyQualifiedName~CommandExecutorTests|FullyQualifiedName~QueryExecutorTests|FullyQualifiedName~AsyncQueryExecutorTests|FullyQualifiedName~LegacyAsyncCommandDispatchRequestHandlerTests"`
    - 结果：通过，`54/54` passed

### 阶段：PR #334 legacy bridge / 文档 review 收尾（CQRS-REWRITE-RP-094）

- 使用 `$gframework-pr-review` 抓取当前分支公开 PR，确认 `feat/cqrs-optimization` 当前对应 `PR #334`
- latest-head open AI review 复核后，主线程接受并执行的修复集中在六类：
  - `GFramework.Core.Tests/Architectures/ArchitectureContextTests.cs` 通过字符串字面量反射实例化内部 bridge handler，维护成本高且不利于 rename-safe 重构
  - `ArchitectureModulesBehaviorTests` 在断言失败路径下未保证 `DestroyAsync()` 执行，且 `TearDown` 未重置 `LegacyBridgePipelineTracker`
  - `LegacyBridgePipelineTracker` 以静态共享计数器记录 bridge pipeline 命中，但未文档化线程安全语义，且用字符串匹配类型名识别 bridge request
  - `LegacyAsyncQueryDispatchRequestHandler` / `LegacyAsyncCommandResultDispatchRequestHandler` 丢弃了 runtime 传入的 `CancellationToken`
  - `CommandExecutorModule` / `QueryExecutorModule` / `AsyncQueryExecutorModule` 依赖 `container.Get<ICqrsRuntime>()` 的隐式注册顺序，但此前既未显式失败，也未写进 API 契约
  - 多个 legacy bridge request / docs 页面仍缺 XML 文档或回退边界说明
- 本轮主线程决策：
  - 为 `GFramework.Core` 新增 `Properties/AssemblyInfo.cs`，用 `InternalsVisibleTo("GFramework.Core.Tests")` 让测试直接实例化内部 handler
  - 把 `ArchitectureContextTests.RegisterLegacyBridgeHandlers` 改成显式构造 6 个 handler，移除字符串反射装配
  - 为 bridge 相关测试补 `TearDown` 清理和 `try/finally` 销毁，减少失败路径资源泄露
  - 为 `LegacyBridgePipelineTracker` 增补 `<remarks>`，并改用 `typeof(LegacyCqrsDispatchRequestBase).IsAssignableFrom(requestType)` 识别 bridge request
  - 为 `LegacyAsyncQueryDispatchRequestHandler` / `LegacyAsyncCommandResultDispatchRequestHandler` 加入预取消检查与 `WaitAsync(cancellationToken)`
  - 将三个 executor module 改为 `GetRequired<ICqrsRuntime>()`，同时在 XML 文档中显式声明 `CqrsRuntimeModule` 的前置注册约束
  - 为 `CommandExecutor` / `QueryExecutor` / `AsyncQueryExecutor` 的 dispatch-context helper 增加 `[MemberNotNullWhen]`，收敛重复 `_runtime is not null` 判空与 null-forgiving
  - 补齐 legacy bridge request / handler 的 XML 文档，以及 `docs/zh-CN/core/command.md`、`context.md` 的 fallback 边界说明
- 本轮没有跟进的 thread：
  - `GFramework.Cqrs.Benchmarks/Messaging/RequestLifetimeBenchmarks.cs` 的 `sealed` 建议属于低价值性能/风格提示，不影响 `PR #334` 的行为正确性
  - 若 review 在 GitHub 重新索引前仍显示旧 thread，下一轮以最新 head commit 再次抓取为准，不在本地重复造改动
- 本轮权威验证：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
    - 结果：通过，`0 warning / 0 error`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureContextTests|FullyQualifiedName~ArchitectureModulesBehaviorTests|FullyQualifiedName~CommandExecutorTests|FullyQualifiedName~QueryExecutorTests|FullyQualifiedName~AsyncQueryExecutorTests"`
    - 结果：通过，`48/48` passed
  - `env GIT_DIR=... GIT_WORK_TREE=... python3 scripts/license-header.py --check`
    - 结果：通过
  - `git diff --check`
    - 结果：通过

### 阶段：legacy Core CQRS -> GFramework.Cqrs bridge（CQRS-REWRITE-RP-093）

- 延续 `$gframework-batch-boot 50`，本轮明确不只盯 benchmark，而是同时处理两个目标：
  - 复核 `ai-libs/Mediator` 还有哪些能力尚未被 `GFramework.Cqrs` 吸收
  - 验证 `GFramework.Core` 的简单 `Command` / `Query` 兼容入口能否在不改外部用法的前提下，底层统一改走 `GFramework.Cqrs`
- 主线程先完成 `GFramework.Core` bridge 实现收尾与测试修正：
  - `ArchitectureContext` 的 legacy `SendCommand(...)` / `SendQuery(...)` / `SendQueryAsync(...)` 现在会创建内部 bridge request，并直接通过统一 `ICqrsRuntime` 分发
  - `CommandExecutor`、`QueryExecutor`、`AsyncQueryExecutor` 在解析到 runtime 且目标对象可提供架构上下文时，也会复用同一条 bridge/runtime 路径
  - 为避免破坏不依赖容器的旧测试，执行器仍保留“未接入 runtime 时直接执行”的回退语义
- 新增 `GFramework.Core/Cqrs/Legacy*DispatchRequest*.cs` 与对应 handler，把 legacy 命令/查询包装成内部 request：
  - bridge handler 在执行前会显式把当前 `IArchitectureContext` 注入给 `IContextAware` 目标
  - 这让旧调用链在不改 public API 的情况下，也能复用统一 pipeline 与 handler dispatch 语义
- 生产接线结论已经本地复核：
  - `CqrsRuntimeModule` 只注册 runtime / registrar / registration service，本身不直接手工注册 bridge handler
  - 默认生产路径依赖 `ArchitectureBootstrapper.ConfigureServices(...)` 自动调用 `RegisterCqrsHandlersFromAssemblies([architectureType.Assembly, typeof(ArchitectureContext).Assembly])`
  - 因此 `GFramework.Core` 程序集中的 internal bridge handler 会在标准架构初始化阶段自动被扫描和注册，不需要业务侧手工补注册
- 为防止以后有人改坏默认扫描范围，本轮额外补了一条更接近真实启动路径的回归：
  - `ArchitectureModulesBehaviorTests.InitializeAsync_Should_AutoRegister_LegacyBridgeHandlers_For_Default_Core_Assemblies`
  - 该用例通过 `Architecture.Configurator` 注册 open-generic pipeline behavior，然后直接走 `Architecture.InitializeAsync()`，验证旧 `SendCommand` / `SendQuery` 兼容入口能命中统一 pipeline
- 只读 subagent 同步完成 `Mediator` 差距复核，接受的结论是六类未完全吸收能力：
  - `IMediator` / `ISender` / `IPublisher` 风格 facade
  - telemetry / tracing / metrics
  - stream pipeline
  - notification publisher 策略
  - 生成器配置与诊断公开面
  - 生命周期 / 缓存公开配置面
- 文档与恢复入口同步更新：
  - `docs/zh-CN/core/context.md`、`command.md`、`query.md`、`cqrs.md`
  - `GFramework.Core/README.md`
  - active tracking / trace 升级到 `RP-093`

### 验证（RP-093）

- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureContextTests|FullyQualifiedName~CommandExecutorTests|FullyQualifiedName~QueryExecutorTests|FullyQualifiedName~AsyncQueryExecutorTests"`
  - 结果：通过，`45/45` passed
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过，`1644/1644` passed
- `env GIT_DIR=... GIT_WORK_TREE=... python3 scripts/license-header.py --check`
  - 结果：通过
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-093）

1. 若继续沿用 `$gframework-batch-boot 50`，优先从 `stream pipeline` 或 `notification publisher` 策略中切一块对齐 `Mediator`
2. 若要继续收敛 public seam，下一批优先设计 facade，而不是继续扩大 `ArchitectureContext` 的兼容职责

### 阶段：request handler 生命周期矩阵 benchmark（CQRS-REWRITE-RP-092）

- 使用 `$gframework-batch-boot 50` 启动本轮批次，并按技能要求先复核 `origin/main` 基线与 branch diff：
  - `origin/main` = `2c58d8b6`，提交时间 `2026-05-07 13:24:46 +0800`
  - 本地 `main` = `c2d22285`，已落后于 remote-tracking ref，因此不作为本轮 batch baseline
  - 当前 `feat/cqrs-optimization` 相对 `origin/main` 的累计 branch diff 在开工前为 `0 files / 0 lines`
- 本轮批次目标：继续推进 `GFramework.Cqrs.Benchmarks`，补一个独立、低风险、可单项目 Release 验证的 request 生命周期对照切片
- 主线程先复核现有 benchmark 宿主与 runtime 解析路径后确认：
  - `RequestBenchmarks` 与 `StreamingBenchmarks` 当前都固定使用单根容器宿主
  - `MicrosoftDiContainer` 虽支持 `RegisterScoped` / `CreateScope()`，但当前 `CqrsDispatcher` 的 steady-state benchmark 路径直接从根容器解析 handler
  - 因此若直接把 `Scoped` 注册加入现有 benchmark，会把“根作用域下的 scoped 解析”误当成公平对照，语义不成立
- 本轮决策：
  - 新增 `Messaging/RequestLifetimeBenchmarks.cs`
  - 生命周期矩阵只覆盖 `Singleton / Transient`
  - 在 XML 文档与 README 中显式注明：`Scoped` 需要等未来具备真实显式作用域边界的 benchmark host 后再比较
- 已修改：
  - `GFramework.Cqrs.Benchmarks/Messaging/RequestLifetimeBenchmarks.cs`
  - `GFramework.Cqrs.Benchmarks/README.md`
  - `ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md`
  - `ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md`
- 预期结果：
  - `GFramework.Cqrs.Benchmarks` 不再只覆盖“有无 generated provider / startup / pipeline”的维度，也开始覆盖 request steady-state 下的 handler 生命周期成本差异
  - benchmark 设计继续保持“只加入语义公平的矩阵”，避免把作用域模型不对称的结论写进基线

### 验证（RP-092）

- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet run --project GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release --no-build -- --filter "*RequestLifetimeBenchmarks*" --job short --warmupCount 1 --iterationCount 1 --launchCount 1`
  - 结果：通过（沙箱外权威结果）
  - 备注：当前 agent 沙箱内执行同一 benchmark 会在 BenchmarkDotNet 自动生成 bootstrap 阶段失败；切换到沙箱外后，`restore/build` 自举与 6 个 benchmark case 全部通过
  - 备注：`Singleton` 下 baseline / MediatR / GFramework 分别约 `5.633 ns / 58.687 ns / 301.731 ns`
  - 备注：`Transient` 下 baseline / MediatR / GFramework 分别约 `5.044 ns / 52.274 ns / 287.863 ns`
- `env GIT_DIR=... GIT_WORK_TREE=... python3 scripts/license-header.py --check`
  - 结果：通过
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-092）

1. 若 branch diff 仍明显低于 `$gframework-batch-boot 50` 阈值，下一批优先补 `stream handler` 生命周期矩阵，保持 request / stream benchmark 维度对称
2. 若准备扩到 `Scoped` 生命周期，先为 benchmark host 设计真实显式作用域基线，再进入运行时对照

## 2026-05-06

### 阶段：PR #331 review 收尾补丁（CQRS-REWRITE-RP-091）

- 使用 `$gframework-pr-review` 拉取当前分支 `fix/package-validation-guard` 对应的 `PR #331` latest-head review 后，主线程只保留本地复核仍成立的问题：
  - `.github/workflows/ci.yml` 的 `dotnet pack` 步骤缺少 `--no-build`，会在已完成 solution `Build` 后重复编译整仓库
  - `scripts/validate-packed-modules.sh` 使用 GNU `find -printf`，在 macOS / BSD `find` 下无法运行
  - `ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md` 的 active PR 锚点仍写成 `待创建`，与当前公开 PR 状态不一致
- 本轮决策：
  - `ci.yml` 的 pack 步骤显式补上 `--no-build`，使其与前置 `Build` 步骤形成单次编译链路
  - 共享包校验脚本改为使用 `find ... -exec basename {} \;`，避免依赖 GNU-only 选项
  - active tracking 同步到 `PR #331`，并把这轮 PR review 的剩余问题描述更新为当前已核验的真实范围
- 预期结果：
  - PR workflow 的 pack 阶段不再对同一 solution 重复编译
  - `validate-packed-modules.sh` 可在 GNU / BSD `find` 环境下保持相同行为
  - `cqrs-rewrite` active 恢复入口继续与当前公开 PR 保持一致

### 阶段：benchmark 发布面隔离与包清单校验前移（CQRS-REWRITE-RP-091）

- 针对 tag 发布中出现的 `GFramework.Cqrs.Benchmarks` 异常包名单，本轮先复核 benchmark 项目与 solution pack 的本地事实：
  - `GFramework.Cqrs.Benchmarks.csproj` 已包含 `IsPackable=false` 与 `GeneratePackageOnBuild=false`
  - 本地执行 `dotnet pack GFramework.sln -c Release --no-restore -o /tmp/gframework-sln-pack-probe -p:IncludeSymbols=false` 时，产物仅包含 14 个预期发布包
  - 因此本轮不把 benchmark 包加入发布白名单，而是把“benchmark 永不发布”与“PR 前置完整包名单校验”同时固化
- 本轮决策：
  - 为 `GFramework.Cqrs.Benchmarks` 补充注释，明确其 benchmark-only 的发布边界
  - 新增 `scripts/validate-packed-modules.sh`，集中维护预期包集合与实际 `.nupkg` diff 逻辑
  - `publish.yml` 改为调用共享脚本，避免发布工作流与 PR 工作流各自维护一份包名单
  - `ci.yml` 新增 solution `dotnet pack` 与 packed modules 校验，把异常发布包从 tag 发布前移到普通 PR 阶段
- 预期结果：
  - benchmark / example / tooling 一类新项目若意外进入发布面，会先在 PR 失败，而不是等到 tag 发布
  - 发布与 PR 使用同一份包名单规则，减少后续名单漂移
  - `GFramework.Cqrs.Benchmarks` 继续只服务于 benchmark workflow，不进入 NuGet / GitHub Packages

### 阶段：benchmark 对照宿主收敛与 startup cold-start 恢复（CQRS-REWRITE-RP-090）

- 使用 `$gframework-pr-review` 拉取 `PR #326` latest-head review 后，主线程确认仍有效的 benchmark 反馈集中在三类问题：
  - `RequestBenchmarks` 的 GFramework / MediatR handler 生命周期不对齐
  - `RequestStartupBenchmarks` 把容器构建、程序集扫描范围和缓存清理阶段混在一起，导致 cold-start 对照不公平
  - benchmark 工程里的 `MicrosoftDiContainer` 多处以 `ImplementationType` 方式注册 handler，但未在 runtime 分发前 `Freeze()`，首次真实解析路径存在隐藏失败风险
- 本轮本地复核的关键根因：
  - `MicrosoftDiContainer.Get(Type)` 在未冻结时只读取 `ImplementationInstance`，不会实例化 `ImplementationType`
  - `ColdStart_GFrameworkCqrs` 清空 dispatcher 静态缓存后，首次发送必须走真实 handler 解析，因此会稳定触发 `No CQRS request handler registered`
  - 多个 benchmark 同时采用“手工 MediatR 注册 + `RegisterServicesFromAssembly(...)` 全程序集扫描”，容易把无关 handler / behavior 一并纳入对照，且存在重复注册漂移
- 本轮决策：
  - 新增 `Messaging/BenchmarkHostFactory.cs`，统一 benchmark 最小宿主构建规则
  - GFramework benchmark 宿主统一先注册再 `Freeze()`，保证 steady-state 与 cold-start 都走真实可解析容器
  - MediatR benchmark 宿主统一通过 `TypeEvaluator` 限制到当前场景所需 handler / behavior 类型，保留正常 `AddMediatR` 组装路径，同时移除全程序集扫描噪音
  - `RequestStartupBenchmarks` 采用专用 `ColdStart` job，设置 `InvocationCount=1` 与 `WithUnrollFactor(1)`，并把 dispatcher cache reset 放到 `IterationSetup`
- 已修改的 benchmark 范围：
  - `RequestBenchmarks`
  - `RequestPipelineBenchmarks`
  - `RequestStartupBenchmarks`
  - `StreamingBenchmarks`
  - `NotificationBenchmarks`
  - `RequestInvokerBenchmarks`
  - `StreamInvokerBenchmarks`
- 结果：
  - `ColdStart_GFrameworkCqrs` 已恢复出有效结果，不再出现 `No CQRS request handler registered`
  - `RequestBenchmarks`、`RequestStartupBenchmarks` 在本地均可实际运行
  - `RequestStartupBenchmarks` 目前仍会收到 BenchmarkDotNet 对单次 cold-start 场景的 `MinIterationTime` 提示；这是测量形状带来的工具提示，不再是运行级失败

### 验证（RP-090）

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过
  - 备注：确认当前分支对应 `PR #326`，仍有效的 open AI feedback 集中在 benchmark 对照语义与 active 文档收敛
- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet run --project GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release -- --filter "*RequestStartupBenchmarks*" --job short --warmupCount 1 --iterationCount 1 --launchCount 1`
  - 结果：通过
  - 备注：`ColdStart_GFrameworkCqrs` 已恢复，最新本地输出约 `220-292 us`，`ColdStart_MediatR` 约 `575-616 us`
- `dotnet run --project GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release -- --filter "*RequestBenchmarks*" --job short --warmupCount 1 --iterationCount 1 --launchCount 1`
  - 结果：通过
  - 备注：steady-state request 对照可正常运行，未再触发 MediatR 重复注册或 GFramework 首次解析失败

### 阶段：PR #326 review 收尾补丁（CQRS-REWRITE-RP-090）

- 再次使用 `$gframework-pr-review` 复核 `PR #326` latest-head open threads 后，主线程确认本轮仍成立且适合在当前 PR 内收敛的问题集中在四类：
  - `.github/workflows/benchmark.yml` 的 `benchmark_filter` 直接插值到 shell，存在 workflow_dispatch 输入注入风险
  - `RequestInvokerBenchmarks` 与 `StreamInvokerBenchmarks` 的 MediatR handler 生命周期仍为 `Singleton`，与 GFramework 反射 / generated 路径的 transient 语义不一致
  - `RequestPipelineBenchmarks` 未在场景切换前后清理 dispatcher 缓存，且四个空 pipeline behavior 类型仍使用非法的分号类声明
  - `ai-plan/public/cqrs-rewrite` active 文档仍保留旧失败结论与重复日期标题，和“active 入口只保留最新权威恢复点”的约束不一致
- 本轮刻意未扩展处理的 review：
  - `MicrosoftDiContainer` 的释放契约建议会扩大到核心 Ioc 接口与全仓库生命周期语义，不适合作为 benchmark review 顺手改动
  - `RequestStartupBenchmarks` 的“手工单点注册 vs 受限程序集扫描”差异目前属于有意保留的最小宿主模型，代码注释已明确该设计边界
- 已修改：
  - `.github/workflows/benchmark.yml`
  - `GFramework.Cqrs.Benchmarks/Messaging/RequestInvokerBenchmarks.cs`
  - `GFramework.Cqrs.Benchmarks/Messaging/RequestPipelineBenchmarks.cs`
  - `GFramework.Cqrs.Benchmarks/Messaging/StreamInvokerBenchmarks.cs`
  - `ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md`
  - `ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md`
- 预期结果：
  - 手动 benchmark workflow 的过滤器输入不再直接参与 shell 解析
  - request / stream invoker 三路对照的 handler 生命周期重新回到同一基线
  - request pipeline benchmark 在 `0 / 1 / 4` 场景切换时不再复用旧 dispatcher cache
  - active tracking / trace 更符合 boot 恢复入口所要求的“只保留最新权威结论”形状

## 2026-04-30

### 阶段：历史 PR #307 active 入口收敛（CQRS-REWRITE-RP-076）

- 继续沿用 `$gframework-pr-review` 对 `PR #307` 做 latest-head triage，本轮只处理仍成立的 `ai-plan` 恢复入口问题
- 主线程确认当前远端权威信号：
  - 当时分支对应 `PR #307`，状态为 `OPEN`
  - 远端 `CTRF` 最新汇总为 `2247/2247` passed
  - `MegaLinter` 仅剩 `dotnet-format` 的 `Restore operation failed` 环境噪音
  - 仍未闭环的 review 重点集中在 `cqrs-rewrite` active tracking / trace 仍保留过多历史锚点，而非新的运行时代码缺陷
- 本轮决策：
  - 将 active tracking 收敛为单一恢复入口，只保留 `RP-076`、`PR #307`、活跃风险、最近权威验证与下一推荐步骤
  - 将 active trace 收敛为当前阶段的关键事实与决策，不再在默认恢复入口中保留 `RP-062` 之后的长阶段流水账
  - 新增 `archive/traces/cqrs-rewrite-history-rp062-through-rp076.md` 承接 `RP-062` 至 `RP-076` 的详细 trace 历史，保持旧阶段仍可追溯

### 验证（RP-076）

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output <temporary-json-output>`
  - 结果：通过
  - 备注：确认 `PR #307` 的当前 review 重点已收敛到 `ai-plan` 文档收尾
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Provider_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Enumerator|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Entry_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过，`5/5` passed

### 当前下一步（RP-076）

1. 当时继续按 `PR #307` 的 latest-head review 收尾，优先保持 active tracking 与 active trace 的单一锚点一致
2. 若继续推进代码切片，先复核 request 侧是否仍存在与 stream invoker gate 对称的生成合同遗漏
3. 进入下一批前继续使用最小 Release build 或 targeted test 作为权威验证，避免把环境噪音误判为代码问题

## 2026-05-04

### 阶段：request invoker provider gate 对称回归（CQRS-REWRITE-RP-077）

- 使用 `$gframework-batch-boot 25` 继续 `feat/cqrs-optimization` 的 CQRS 收口批次
- 批次目标：在 branch diff 相对 `origin/main` 接近 `25` 个文件前，补齐低风险的 generator 合同回归切片
- 本轮先确认当前 worktree 已无 `local-plan` 遗留恢复入口，随后转入 `cqrs-rewrite` 的 request / stream invoker provider gate 对称性复核
- 结论：
  - 生产代码已经同时检查 request provider、enumerator、descriptor 与 descriptor entry 四项 runtime 合同
  - request 侧测试只覆盖缺少 provider / enumerator，缺少 descriptor / descriptor entry 的回归覆盖落后于 stream 侧
- 已补齐：
  - `Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Type`
  - `Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Entry_Type`
  - source emission XML 文档同步说明 provider gate 依赖完整 descriptor / descriptor entry 合同

### 验证（RP-077）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Entry_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Provider_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Enumerator"`
  - 结果：通过，`4/4` passed
- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行，避免脚本内部 plain `git ls-files` 误判仓库上下文
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-077）

1. 继续使用 `origin/main` 作为 `$gframework-batch-boot 25` 的基线，复算 branch diff 后决定是否还能接下一批
2. 若继续推进代码切片，优先查找 request / stream invoker provider runtime 合同之外的同类对称测试缺口

### 阶段：mixed fallback attribute usage 回归（CQRS-REWRITE-RP-078）

- 继续沿用 `$gframework-batch-boot 25`，当前 branch diff 仍低于阈值
- 复核 fallback metadata runtime contract 后确认：
  - mixed fallback 在 runtime 允许多个 fallback attribute 实例时已有直接 `Type` + 字符串拆分回归
  - runtime 同时支持 `params Type[]` / `params string[]` 但不允许多个 fallback attribute 实例时，缺少锁定“整体回退到单个字符串 attribute”的回归
- 已补齐：
  - `Emits_String_Fallback_Metadata_For_Mixed_Fallback_When_Runtime_Disallows_Multiple_Fallback_Attributes`
  - `ReplaceAttributeUsageForType` 测试辅助方法，用于构造 runtime attribute usage 变体而不复制大型 source fixture

### 验证（RP-078）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_String_Fallback_Metadata_For_Mixed_Fallback_When_Runtime_Disallows_Multiple_Fallback_Attributes"`
  - 结果：通过，`1/1` passed
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-078）

1. 继续复算 branch diff vs `origin/main`，若仍低于 `25` 个文件可继续下一批
2. 下一批优先查看 fallback metadata 与 generated invoker provider 之外是否还有同类 runtime contract gate 回归缺口

### 阶段：基础 generated registry contract gate 回归（CQRS-REWRITE-RP-079）

- 继续沿用 `$gframework-batch-boot 25`，当前 branch diff 仍低于阈值
- 复核 generator 基础启用条件后确认：缺少 `ICqrsHandlerRegistry` 时，runtime 不具备承载 generated registry 的基础接口合同，应整体跳过发射
- 已补齐：
  - `Does_Not_Generate_Registry_When_Runtime_Lacks_Handler_Registry_Interface`

### 验证（RP-079）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Generate_Registry_When_Runtime_Lacks_Handler_Registry_Interface"`
  - 结果：通过，`1/1` passed
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-079）

1. 继续复算 branch diff vs `origin/main`，若仍低于 `25` 个文件可继续下一批
2. 下一批优先复核基础 generation gate 中其他必需 runtime contracts 是否也需要同类回归覆盖

### 阶段：基础 generated registry contract gate 扩展回归（CQRS-REWRITE-RP-080）

- 将 `RP-079` 的单一 handler registry interface 缺失回归扩展为基础 generation gate 参数化测试
- 已补齐缺失分支：
  - `ICqrsHandlerRegistry`
  - `INotificationHandler<TNotification>`
  - `IStreamRequestHandler<TRequest, TResponse>`
  - `CqrsHandlerRegistryAttribute`
- stream handler interface 变体采用类型重命名构造 runtime metadata miss，避免删除命名空间尾部单行接口时引入输入编译错误

### 验证（RP-080）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Generate_Registry_When_Runtime_Lacks_Required_Generation_Contract"`
  - 结果：通过，`4/4` passed
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-080）

1. 继续复算 branch diff vs `origin/main`，若仍低于 `25` 个文件可继续下一批
2. 下一批优先复核基础 generation gate 中 logging / DI 依赖是否已有合适的输入编译安全回归覆盖方式

### 阶段：基础 generated registry external contract gate 回归（CQRS-REWRITE-RP-081）

- 延续 `RP-080` 的参数化基础 generation gate 测试，将外部 logging / DI 依赖也纳入同一组静默跳过回归
- 已补齐缺失分支：
  - `GFramework.Core.Abstractions.Logging.ILogger`
  - `Microsoft.Extensions.DependencyInjection.IServiceCollection`
- 两个变体均通过类型重命名构造 runtime metadata miss，保持输入源码可编译，避免把依赖缺失测试误写成编译失败测试

### 验证（RP-081）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Generate_Registry_When_Runtime_Lacks_Required_Generation_Contract"`
  - 结果：通过，`6/6` passed
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-081）

1. 继续复算 branch diff vs `origin/main`，若仍低于 `25` 个文件可继续下一批
2. 下一批优先复核基础 generation gate 中 request handler contract 与 handler registry attribute 以外是否还有可安全构造的缺失分支

### 阶段：基础 generated registry request handler gate 回归（CQRS-REWRITE-RP-082）

- 延续 `RP-081` 的基础 generation gate 参数化测试，补齐 `IRequestHandler<TRequest,TResponse>` 缺失分支
- 该变体同样通过类型重命名构造 runtime metadata miss，保持输入源码可编译
- 至此基础 generation gate 中可安全构造的缺失分支已覆盖：
  - request handler interface
  - notification handler interface
  - stream handler interface
  - handler registry interface
  - handler registry attribute
  - logging interface
  - DI service collection interface

### 验证（RP-082）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Generate_Registry_When_Runtime_Lacks_Required_Generation_Contract"`
  - 结果：通过，`7/7` passed
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-082）

1. 继续复算 branch diff vs `origin/main`，若仍低于 `25` 个文件可继续下一批
2. 下一批优先复核基础 generation gate 之外的 runtime contract 或 fallback selection 分支；基础 gate 的可安全构造缺失分支已覆盖

### 阶段：PR #323 review 锚点收敛（CQRS-REWRITE-RP-082）

- 使用 `$gframework-pr-review` 重新拉取当前分支 PR review payload，确认当前分支对应 `PR #323`，状态为 `OPEN`
- 本轮 latest-head open AI thread 仅指出 active tracking 中仍保留 `PR #307` 作为当前 PR 锚点；本地复核后确认该反馈仍成立
- 已将 active tracking 的当前 PR 锚点、活跃事实、最近 PR review 备注和下一推荐步骤统一到 `PR #323`
- `PR #307` 仅保留为历史 PR 说明和较早 trace 段落，不再作为 active 恢复入口

### 验证（PR #323 review）

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output <temporary-json-output>`
  - 结果：通过
  - 备注：确认 `PR #323` 只有 1 个 CodeRabbit open thread，指向 active tracking 的 PR 锚点漂移
- 远端 `CTRF` 最新汇总为 `2274/2274` passed
- `MegaLinter` 当前仅报告 `dotnet-format` 的 `Restore operation failed` 环境噪音，未提供本地仍成立的文件级格式诊断
- `git diff --check`
  - 结果：通过
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

### 当前下一步（PR #323 review）

1. 若 review 重新触发后仍有 latest-head open thread，继续以 `PR #323` 为当前唯一 PR 恢复锚点复核
2. 后续若继续推进代码切片，优先复核基础 generation gate 之外的 runtime contract 或 fallback selection 分支

## 2026-05-06（RP-083 ~ RP-089）

### 阶段：mixed invoker provider 排序回归（CQRS-REWRITE-RP-083）

- 使用 `$gframework-batch-boot 50` 继续 `feat/cqrs-optimization` 的 CQRS 收口批次
- 批次目标：在 branch diff 相对 `origin/main` 接近 `50` 个文件前，继续补齐低风险的 generator runtime contract / emission 回归
- 本轮基线选择：
  - `origin/main a8c6c11e`，committer date `2026-05-05 13:14:24 +0800`
  - `main a8c6c11e`，committer date `2026-05-05 13:14:24 +0800`
  - 当前分支 `feat/cqrs-optimization a8c6c11e`，committer date `2026-05-05 13:14:24 +0800`
- 启动时 branch diff vs `origin/main` 为 `0` files / `0` lines，因此继续选择低风险测试回归切片
- 本轮复核 `CreateGeneratedRegistrySourceShape` 与 invoker emission 路径后确认：
  - 现有测试已覆盖 request / stream provider 的单一 direct 场景、单一 reflected-implementation 场景、precise reflected 跳过边界，以及各项 runtime contract 缺失分支
  - 尚未锁定“同一 registry 同时包含 direct registration 与 reflected-implementation registration”时的 descriptor 顺序与 `Invoke*HandlerN` 编号稳定性
- 已补齐：
  - `Emits_Request_Invoker_Provider_Metadata_In_Stable_Order_For_Mixed_Direct_And_Reflected_Implementations`
  - `Emits_Stream_Invoker_Provider_Metadata_In_Stable_Order_For_Mixed_Direct_And_Reflected_Implementations`
  - 两组 source fixture：`MixedRequestInvokerProviderSource`、`MixedStreamInvokerProviderSource`
- 通过新增回归，显式锁定以下约束：
  - provider descriptor 条目按稳定实现排序输出
  - `InvokeRequestHandler0/1` 与 `InvokeStreamHandler0/1` 的方法编号随 emission 顺序连续增长
  - 隐藏实现类型不会破坏 direct registration 与 reflected-implementation registration 的混合发射

### 验证（RP-083）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_In_Stable_Order_For_Mixed_Direct_And_Reflected_Implementations|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_In_Stable_Order_For_Mixed_Direct_And_Reflected_Implementations"`
  - 结果：通过，`2/2` passed

### 当前 stop-condition 度量（RP-083）

- primary metric：branch diff files vs `origin/main`
- 当前说明：active batch 尚未提交时，基于 `HEAD` 的 branch diff 仍显示 `0` files / `0` lines；提交本批后再以新 `HEAD` 复算累计 branch diff

### 当前下一步（RP-083）

1. 提交本轮 mixed invoker provider 排序回归后，复算 branch diff vs `origin/main`，确认 `50` 文件阈值仍有充足余量
2. 若继续推进代码切片，优先复核 invoker provider 之外的 runtime contract 或 fallback selection 分支

### 阶段：benchmark 基础设施引入（CQRS-REWRITE-RP-084）

- 用户明确将当前长期分支目标上提为：系统性吸收 `ai-libs/Mediator` 的实现思路与设计哲学，并将可取部分纳入 `GFramework.Cqrs`
- 本轮据此调整批次目标，不再把关注点收缩到单个 generator 回归，而是建立能持续比较和吸收设计差异的 benchmark 基础设施
- 参考 `ai-libs/Mediator` 的 benchmark 设计后，本轮采纳的核心结构包括：
  - 独立 benchmark 项目壳，而非扩展现有 NUnit 测试项目
  - 共享 `Fixture` 输出并校验场景配置
  - `Request` / `Notification` 两个 messaging 场景作为首批最小落点
  - 自定义列 `CustomColumn`，为后续矩阵扩展保留可读结果标签
- 本轮新增：
  - `GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj`
  - `GFramework.Cqrs.Benchmarks/Program.cs`
  - `GFramework.Cqrs.Benchmarks/CustomColumn.cs`
  - `GFramework.Cqrs.Benchmarks/Messaging/Fixture.cs`
  - `GFramework.Cqrs.Benchmarks/Messaging/BenchmarkContext.cs`
  - `GFramework.Cqrs.Benchmarks/Messaging/RequestBenchmarks.cs`
  - `GFramework.Cqrs.Benchmarks/Messaging/NotificationBenchmarks.cs`
  - `GFramework.Cqrs.Benchmarks/README.md`
- 设计取舍：
  - 使用最小 `ICqrsContext` marker，避免把完整 `ArchitectureContext` 初始化成本混入 steady-state dispatch
  - 直接复用 `GFramework.Cqrs.CqrsRuntimeFactory` 与 `MicrosoftDiContainer`，让基准聚焦于 runtime dispatch / publish
  - 外部对照组先接入 `MediatR`，保持与 `Mediator` benchmark 的对照哲学一致；但本轮仍只做最小 request / notification 场景
  - 暂不把 source generator benchmark、cold-start 独立工程或完整 pipeline / stream 矩阵一起引入，避免首批 scope 失控
- 兼容性修正：
  - 在根 `GFramework.csproj` 中显式排除 `GFramework.Cqrs.Benchmarks/**`，避免 meta-package 意外编译 benchmark 源码
  - 将 benchmark 项目加入 `GFramework.sln`，保持仓库级工作流完整

### 验证（RP-084）

- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `GIT_DIR=<worktree-git-dir> GIT_WORK_TREE=<worktree-root> python3 scripts/license-header.py --check`
  - 结果：通过
- `git diff --check`
  - 结果：通过

### 当前 stop-condition 度量（RP-084）

- primary metric：branch diff files vs `origin/main`
- 当前说明：本轮仍在 `50` 文件阈值以内，可继续按 benchmark 场景或 CQRS runtime 对照能力分批推进

### 当前下一步（RP-084）

1. 继续扩展 `GFramework.Cqrs.Benchmarks`，优先补齐 pipeline、stream、cold-start 与 generated invoker provider 对照场景
2. 当后续有具体 runtime 优化切片时，用该 benchmark 项目验证是否真正吸收到了 `Mediator` 的低开销 dispatch 设计收益

### 阶段：stream request benchmark 对照（CQRS-REWRITE-RP-085）

- 继续沿用 `$gframework-batch-boot 50`，当前 branch diff 相对 `origin/main` 仍明显低于阈值
- 在 `RP-084` 已建立独立 benchmark 项目后，本轮优先补齐 `ai-libs/Mediator/benchmarks/Mediator.Benchmarks/Messaging/StreamingBenchmarks.cs` 对应的最小 stream 场景
- 选择 stream 作为第二批 benchmark 的原因：
  - 已有独立的 `CreateStream` runtime 路径和单独的 stream invoker provider 元数据契约
  - 与 `Mediator` 的 messaging benchmark 分层直接对应
  - 不需要像 pipeline / cold-start 那样先进一步澄清运行时或宿主边界
- 本轮新增：
  - `GFramework.Cqrs.Benchmarks/Messaging/StreamingBenchmarks.cs`
  - `GFramework.Cqrs.Benchmarks/README.md` 中的 stream 场景说明
- 设计约束：
  - 保持与前一批一致的三路对照：`Baseline`、`GFramework.Cqrs`、`MediatR`
  - 基准测量“完整枚举 3 个元素”的全量消费成本，而不是只测创建异步枚举器
  - 使用最小 `ICqrsContext` marker，继续避免把完整 `ArchitectureContext` 初始化成本混入 steady-state stream dispatch
- 结论：
  - 当前 benchmark 项目已经覆盖 `Request`、`Notification`、`StreamRequest` 三个核心 messaging steady-state 场景
  - 下一批更适合转向 request pipeline 数量矩阵或 cold-start / initialization，而不是继续扩同层次的 messaging 基线

### 验证（RP-085）

- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `GIT_DIR=<worktree-git-dir> GIT_WORK_TREE=<worktree-root> python3 scripts/license-header.py --check`
  - 结果：通过
- `git diff --check`
  - 结果：通过

### 当前 stop-condition 度量（RP-085）

- primary metric：branch diff files vs `origin/main`
- 当前说明：新增 stream benchmark 后仍处于 `50` 文件阈值以内，适合继续下一批 request pipeline 或 cold-start 场景

### 当前下一步（RP-085）

1. 继续扩展 `GFramework.Cqrs.Benchmarks`，优先补齐 request pipeline 数量矩阵，随后再评估 cold-start / initialization
2. 当需要验证 generated invoker provider 的实际收益时，把 request benchmark 扩展为 reflection / generated provider 对照，而不是只停留在框架间对比

### 阶段：request pipeline 数量矩阵（CQRS-REWRITE-RP-086）

- 继续沿用 `$gframework-batch-boot 50`，当前 branch diff 相对 `origin/main` 仍明显低于阈值
- 本轮把 benchmark 关注点从单纯 messaging steady-state 扩展到 request pipeline 编排行为，原因是：
  - `ai-libs/Mediator` 的对照价值已经不只在 request / notification / stream 三个入口本身，还在 pipeline 包装策略与生命周期取舍
  - `GFramework.Cqrs.Internal.CqrsDispatcher` 已按 `behaviorCount` 缓存 `RequestPipelineExecutor<TResponse>` 形状，因此单独量化 `0 / 1 / 4` 个行为的 steady-state 开销有直接信息密度
- 本轮新增：
  - `GFramework.Cqrs.Benchmarks/Messaging/RequestPipelineBenchmarks.cs`
  - `GFramework.Cqrs.Benchmarks/README.md` 中的 request pipeline 场景说明
- 设计取舍：
  - 采用 `0 / 1 / 4` 个 pipeline 行为，而不是立即扩到更大的参数空间，先锁定最有代表性的无行为 / 少量行为 / 常见多行为矩阵
  - 使用最小 no-op 行为族，不引入日志、计时或上下文刷新逻辑，避免把测量结果污染成业务行为成本
  - `GFramework.Cqrs` 与 `MediatR` 侧都只注册当前 benchmark 请求对应的闭合行为类型，确保矩阵反映编排成本而非程序集扫描差异
- 接受的只读 subagent 结论：
  - 下一批 benchmark 继续优先考虑 `cold-start / initialization` 与 `generated provider` 对照，而不是立即照搬 `Mediator` 的 large-project 维度
  - 当前 `GFramework.Cqrs.Benchmarks` 仍未接入 `Mediator` 包和 `GFramework.Cqrs.SourceGenerators`，因此本轮不扩成 `Mediator_IMediator` / generated-provider 对照，避免 scope 失控
- 结论：
  - 当前 benchmark 项目已经覆盖 `Request`、`Notification`、`StreamRequest` 与 `RequestPipeline`
  - 后续若要继续贴近 `Mediator` 的 comparison benchmark，最值得优先补的是 initialization / first-hit 与 generated invoker provider，而不是继续横向堆更多 steady-state messaging 入口

### 验证（RP-086）

- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

### 当前 stop-condition 度量（RP-086）

- primary metric：branch diff files vs `origin/main`
- 当前说明：提交前基于 `HEAD` 的 branch diff 仍为 `14` files，距离 `50` 文件阈值仍有明显余量

### 当前下一步（RP-086）

1. 提交本轮 request pipeline benchmark 后，继续扩展 `GFramework.Cqrs.Benchmarks`，优先补齐 initialization / cold-start 场景
2. 当需要验证 dispatcher 预热与 source generator 收益时，引入 generated invoker provider 对照，并评估是否同时接入 `Mediator` concrete runtime 作为更贴近设计哲学的外部参照

### 阶段：request startup 基线（CQRS-REWRITE-RP-087）

- 继续沿用 `$gframework-batch-boot 50`，当前 branch diff 相对 `origin/main` 仍明显低于阈值
- 本轮目标：把 benchmark 从 steady-state dispatch 再向前推进一层，补齐与 `ai-libs/Mediator/benchmarks/Mediator.Benchmarks/Messaging/Comparison/*` 更接近的 startup 维度
- 本轮新增：
  - `GFramework.Cqrs.Benchmarks/Messaging/RequestStartupBenchmarks.cs`
  - `GFramework.Cqrs.Benchmarks/README.md` 中的 startup 场景说明
- 设计取舍：
  - `Initialization` 只测“从已配置宿主解析/创建 runtime 句柄”的成本，不把完整架构初始化混入 benchmark
  - `ColdStart` 只测新宿主上的首次 request send；`GFramework.Cqrs` 侧在每次 benchmark 前通过反射清空 dispatcher 静态缓存，避免把热缓存误当 first-hit
  - `ColdStart_MediatR` 改为真正 `await` 完任务后再释放 `ServiceProvider`，以满足 `Meziantou.Analyzer` 对资源生命周期的要求，并避免 benchmark 本身含有错误宿主释放语义
- 结论：
  - 当前 benchmark 项目已经覆盖 `Request`、`Notification`、`StreamRequest`、`RequestPipeline`、`RequestStartup`
  - 后续若继续贴近 `Mediator` comparison benchmark，下一批最有价值的是 generated invoker provider、registration / service lifetime 与 concrete runtime 外部对照，而不是继续只加同层 steady-state case

### 验证（RP-087）

- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

### 当前 stop-condition 度量（RP-087）

- primary metric：branch diff files vs `origin/main`
- 当前说明：提交前 branch diff 仍远低于 `50` 文件阈值，可继续下一批 benchmark 或低风险 runtime 对照切片

### 当前下一步（RP-087）

1. 提交本轮 request startup benchmark 后，继续扩展 `GFramework.Cqrs.Benchmarks`，优先评估 generated invoker provider 与 registration / service lifetime 矩阵
2. 若要更贴近 `Mediator` 的 comparison benchmark 设计哲学，评估是否在 benchmark 项目中同时接入 `Mediator` concrete runtime 对照，而不只保留 `MediatR`

### 阶段：request invoker reflection / generated 对照（CQRS-REWRITE-RP-088）

- 继续沿用 `$gframework-batch-boot 50`，当前 branch diff 相对 `origin/main` 仍明显低于阈值
- 本轮目标：不再只比较 `GFramework.Cqrs` 与 `MediatR` 的外层框架差异，而是开始直接量化 `GFramework.Cqrs` 内部 reflection request binding 与 generated invoker provider 路径的 steady-state 差异
- 本轮新增：
  - `GFramework.Cqrs.Benchmarks/Messaging/RequestInvokerBenchmarks.cs`
  - `GFramework.Cqrs.Benchmarks/Messaging/GeneratedRequestInvokerBenchmarkRegistry.cs`
  - `GFramework.Cqrs.Benchmarks/README.md` 中的 generated invoker 场景说明
- 设计取舍：
  - 采用 benchmark 内手写的 generated registry/provider“等价物”，而不是当轮就把真实 `GFramework.Cqrs.SourceGenerators` 接到 benchmark 项目中，目的是先走通真实的 registrar -> descriptor 预热 -> dispatcher generated path，同时把写入面控制在低风险范围
  - generated 对照使用程序集级 `CqrsHandlerRegistryAttribute` + `ICqrsRequestInvokerProvider` + `IEnumeratesCqrsRequestInvokerDescriptors`，确保运行时语义与生产路径一致
  - 在 benchmark 生命周期前后清理 dispatcher 静态缓存，避免 generated descriptor 预热状态跨场景泄漏，污染 reflection 对照
- 结论：
  - 当前 benchmark 项目已经能区分 `GFramework.Cqrs` 的 reflection request 路径、generated request 路径与 `MediatR` 外部对照
  - 后续若继续贴近 `Mediator` comparison benchmark，下一批更适合扩到 registration / service lifetime、stream generated provider，或再决定是否接入 `Mediator` concrete runtime

### 验证（RP-088）

- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

### 当前 stop-condition 度量（RP-088）

- primary metric：branch diff files vs `origin/main`
- 当前说明：提交前 branch diff 仍远低于 `50` 文件阈值，可继续推进下一批 benchmark 对照切片

### 当前下一步（RP-088）

1. 提交本轮 request invoker benchmark 后，继续扩展 `GFramework.Cqrs.Benchmarks`，优先评估 registration / service lifetime 或 stream generated provider

### 阶段：stream invoker reflection / generated 对照（CQRS-REWRITE-RP-089）

- 使用 `$gframework-batch-boot 30` 继续 `feat/cqrs-optimization` 的 CQRS 收口批次
- 本轮基线选择：
  - `origin/main c01abac0`，committer date `2026-05-06 09:40:08 +0800`
  - `main a8c6c11e`，committer date `2026-05-05 13:14:24 +0800`
- 启动时 branch diff vs `origin/main` 为 `18` files / `2100` lines，低于 `30` 文件阈值，因此继续选择单模块、低风险 benchmark 切片
- 复核 `GFramework.Cqrs.Benchmarks` 与 `ai-libs/Mediator/benchmarks` 后确认：
  - `RP-088` 已把 generated descriptor 预热收益量化到 request dispatch 路径
  - stream benchmark 仍停留在 direct handler / reflection runtime / `MediatR` 三路对照，尚未量化 generated stream invoker provider 的收益
  - 虽然 `Mediator` 参考基准大量使用 service lifetime 矩阵，但当前 `GFramework.Cqrs.Benchmarks` 尚未建立对称的 scoped host 模式；直接扩 lifetime 会引入超出本批风险预算的宿主语义变化
- 本轮因此优先选择 request 对称切片，而不是 service lifetime 扩展：
  - 新增 `Messaging/StreamInvokerBenchmarks.cs`
  - 新增 `Messaging/GeneratedStreamInvokerBenchmarkRegistry.cs`
  - 更新 `GFramework.Cqrs.Benchmarks/README.md`
- 设计约束：
  - 继续沿用 handwritten generated registry/provider 模式，避免把 benchmark 基础设施与真实 source-generator 输出耦合
  - 复用与 `RP-088` 相同的 dispatcher 缓存清理策略，确保 reflection / generated 路径对照不受静态缓存残留污染
  - 使用统一的异步枚举体工厂，让三组 stream handler 共享同一枚举成本基线，把变量收敛到 invoker/provider 接线路径

### 当前下一步（RP-089）

1. 完成本轮 benchmark 项目 Release build、license header 检查与 diff 校验后，更新 active tracking 的权威验证列表
2. 若 branch diff 仍明显低于 `30` 文件阈值，可继续评估 notification publish strategy 或更贴近 `Mediator` concrete runtime 的单批对照
3. 若要继续贴近 `Mediator` 的 comparison benchmark 设计哲学，评估是否把 `Mediator` concrete runtime 本身接入 benchmark 项目，而不是长期只保留 `MediatR`

### 验证（RP-089）

- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `GIT_DIR=<worktree-git-dir> GIT_WORK_TREE=<worktree-root> python3 scripts/license-header.py --check`
  - 结果：通过
- `git diff --check`
  - 结果：通过
- `dotnet run --project GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release -- --filter "*RequestStartupBenchmarks*" --job short --warmupCount 1 --iterationCount 1 --launchCount 1`
  - 结果：部分通过；`MediatR` startup benchmark 已恢复真实测量，`ColdStart_GFrameworkCqrs` 仍因 `No CQRS request handler registered` 失败

### 阶段：手动 benchmark workflow（CQRS-REWRITE-RP-089）

- 新增 `.github/workflows/benchmark.yml`，提供仅 `workflow_dispatch` 触发的 benchmark 入口
- workflow 默认只执行 `GFramework.Cqrs.Benchmarks` 的 Release build，避免在当前已知 `RequestStartupBenchmarks` 残留未清时默认运行失败
- 只有在手动输入 `benchmark_filter` 时才执行 BenchmarkDotNet，并上传 `BenchmarkDotNet.Artifacts` 供后续比较
