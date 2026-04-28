# Analyzer Warning Reduction 追踪

## 2026-04-28 — RP-088

### 阶段：收敛 PR #300 的 open review threads 与 failed-test follow-up

- 触发背景：
  - 用户执行 `$gframework-pr-review`，要求以当前分支 PR 真值为准核对 AI review / failed-test / linter 信号
  - `fetch_current_pr_review.py --json-output /tmp/current-pr-review.json` 返回 `PR #300`，latest head 上仍有 `8` 条 CodeRabbit open threads、`1` 个失败测试 `RegistryInitializationHookBaseTests.OnPhase_Should_Not_Throw_When_Registry_Not_Found`，以及 `dotnet-format` restore failed 的 CI 噪音
- 主线程实施：
  - 核对 `TestArchitectureContext` / `TestArchitectureContextV3` 后，修复共享事件总线与旧版命令/查询入口的静默成功问题，统一改为显式 `NotSupportedException` 或 faulted task
  - 校正 `TestArchitectureWithRegistry` / `TestArchitectureWithoutRegistry` 的显式接口 `RegisterLifecycleHook`，使接口视角与公开 no-op 语义一致
  - 修复 `RegistryInitializationHookBase` 在注册表缺失场景下的 no-op 行为，并保持有注册表路径继续使用单实例 `GetUtility<TRegistry>()`
  - 收敛 `TestResourceLoader`、`TestLogger`、`CapturingLoggerFactoryProvider`、`DeterministicNotificationHandlerState`、`PartialGeneratedNotificationHandlerRegistry` 的判空、XML 文档、快照访问与并发语义说明
  - 新增 `TestArchitectureContextBehaviorTests.cs`，覆盖共享事件总线、旧入口失败契约与 `RegisterLifecycleHook` 接口行为
- 验证里程碑：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~RegistryInitializationHookBaseTests|FullyQualifiedName~WaitForMultipleEventsTests|FullyQualifiedName~ResourceManagerTests|FullyQualifiedName~LoggerTests|FullyQualifiedName~TestArchitectureContextBehaviorTests"`
    - 结果：成功；`97` 通过、`0` 失败
  - `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
    - 结果：成功；`125 Warning(s)`、`0 Error(s)`；warning 全部来自既有 `Mediator/*` 文件，当前 helper 改动未新增 warning
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~CqrsHandlerRegistrarTests"`
    - 结果：成功；`11` 通过、`0` 失败
- 当前结论：
  - `PR #300` 当前仍然成立的 open review threads 已在本地收敛，且 failed-test 信号已被现有测试回归覆盖
  - `Core` / `Core.Tests` 的本轮受影响项目均保持 `0 Warning(s)`，新增测试也覆盖了此前没有直接回归的测试替身行为
  - `GFramework.Cqrs.Tests` 仍保留 `125` 条既有 `Mediator/*` warning；这属于下一轮 warning reduction 波次，而不是本轮 PR review follow-up 的直接写集
- 下一步：
  1. 提交本轮 PR review follow-up 与 `ai-plan` 同步。
  2. 若继续处理 `Cqrs.Tests` warning，以下一轮单独规划 `Mediator/*` 波次为起点。

## 2026-04-28 — RP-087

### 阶段：按 `$gframework-batch-boot 50` 并行收敛 `Core.Tests` / `Cqrs.Tests` 低风险切片

- 触发背景：
  - 用户再次要求先拿仓库根构建 warning，再把可切分的 warning 批次分派给多个 subagent，以降低主线程上下文压力
  - 当前分支与 `origin/main@6cc87a9` 无提交差异，适合从单文件和小型混合 warning 切片重新起步
- 主线程实施：
  - 先执行仓库规则要求的 non-incremental 基线：`dotnet restore GFramework.sln -p:RestoreFallbackFolders=` 以修复当前 WSL 环境里的旧 fallback 资产，再执行仓库根 `dotnet clean` + `dotnet build`
    - 基线结果：`288 Warning(s)`、唯一位点 `214`
  - 并行下发四个 disjoint worker：
    - `GameContextTests.cs`
    - `ArchitectureServicesTests.cs`
    - `RegistryInitializationHookBaseTests.cs`
    - `CqrsDispatcherCacheTests.cs`
  - 主线程补齐不与 worker 重叠的零散切片：
    - `ResourceManagerTests.cs`
    - `TestEvent.cs`
    - `EventListenerScopeTests.cs`
    - `TestArchitectureBase.cs`
    - `ContextProviderTests.cs`
    - `LoggerTests.cs`
    - `LoggingConfigurationTests.cs`
    - `WaitForMultipleEventsTests.cs`
    - `CommandCoroutineExtensionsTests.cs`
  - 集成 `GameContextTests`、`CqrsDispatcherCacheTests` 与 `CqrsHandlerRegistrarTests` worker 产出的 commit `a7be413` / `9098490` / `98afcbf`，并接受已直接落到共享工作树的 `ArchitectureServicesTests` / `RegistryInitializationHookBaseTests` 切片
  - 重新执行仓库根 `dotnet clean` + `dotnet build`
    - final 结果：`236 Warning(s)`、唯一位点 `162`
- 验证里程碑：
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet clean`
    - 结果：成功
  - `dotnet build`
    - 结果：成功；`236 Warning(s)`、`0 Error(s)`，唯一位点 `162`
  - `git diff --name-only refs/remotes/origin/main...HEAD | wc -l`
    - 结果：`21`
  - 已提交 diff 与当前工作树变更并集文件数
    - 结果：`45 / 50`
- 当前结论：
  - 本轮已经把剩余 `Core.Tests` warning 全部清空，并把 `CqrsDispatcherCacheTests.cs` 与 `CqrsHandlerRegistrarTests.cs` 这两处 `Cqrs.Tests` 单文件 `MA0048` 热点从仓库根中移除
  - 仓库根权威 warning 从 `288` 下降到 `236`，唯一位点从 `214` 下降到 `162`
  - 在 `45 / 50` footprint 下，继续扩批将明显压缩 review 余量；剩余切片也只剩 `Mediator*` 与 `YamlConfigSchemaValidator*` 这两组高风险热点，因此本轮应在这里收口
- 下一步：
  1. 提交本轮已验证的 warning reduction 与 `ai-plan` 同步。
  2. 下一轮在新提交基础上单独规划 `Mediator*` 波次。
  3. 将 `YamlConfigSchemaValidator*` 保持为独立高耦合波次，再决定是否需要新的并行切法。

## 2026-04-27 — RP-086

### 阶段：收敛 PR #298 的 CodeRabbit nitpick follow-up

- 触发背景：
  - 用户再次执行 `$gframework-pr-review` 后，要求按 `PR #298` 的 nitpick comments 收敛仍然适用的问题
  - 复核 PR 真值后确认当前分支无 failed checks、无 open review threads，但仍有一批测试辅助类型的可维护性 nitpick 值得本地落地
- 主线程实施：
  - 校验 `TestStateMachineSystemV5`、`ComplexQuery`、`TrackingPipelineBehavior`、`TestEnvironment`、`TestContextUtilityV1/V2` 等改动后，分别修复可变内部状态暴露、`_context!` 空抑制、静态计数器非原子递增、`new Register(...)` 测试辅助入口和生命周期标记公开 setter 问题
  - 统一更新 `TestQueryV2`、`TestCommandWithResultV2`、`TestAsyncQueryInput`、`TestAsyncQueryResult*` 的 XML 文档，使 `init` 属性语义与文档一致
  - 将三倍结果属性从 `DoubleValue` 更名为 `TripleValue`，同步更新 `TestAsyncComplexQuery*` 与相关断言，避免名称与 `* 3` 的行为不一致
  - 精简 active tracking，移除重复的 `GFramework.Core.Tests` Release build 记录，并把该项目的当前真值修正为 `28 Warning(s)`
- 验证里程碑：
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`28 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build`
    - 结果：成功；`1610` 通过、`0` 失败
- 当前结论：
  - `PR #298` 中仍然适用的低风险 nitpick 已完成收敛，且没有为当前 touched files 引入新的构建 warning 或测试回归
  - `GFramework.Core.Tests` 的剩余 warning 仍集中在 `GameContextTests.cs`、`ArchitectureServicesTests.cs`、`RegistryInitializationHookBaseTests.cs` 等既有热点，不属于本轮 nitpick follow-up 新引入问题
- 下一步：
  1. 提交本轮 `PR #298` nitpick follow-up 与 `ai-plan` 同步。
  2. 回到 `GameContextTests.cs` / `ArchitectureServicesTests.cs` 的 `CS8766` warning reduction 主线。

## 2026-04-27 — RP-085

### 阶段：按 `$gframework-batch-boot 100` 并行消化 `GFramework.Core.Tests` 低风险 `MA0048`

- 触发背景：
  - 用户要求以仓库根 non-incremental 构建 warning 为准，并在上下文可控前提下把小切片分派给多个 subagent 并行处理
  - 本轮开始时，当前分支与 `origin/main@7cfdd2c` 无提交差异，适合从纯 `MA0048` 单文件切片起步
- 主线程实施：
  - 执行权威基线：`dotnet clean` + 仓库根 `dotnet build`
    - 初始结果：`353 Warning(s)`、唯一位点 `279`
  - 分四波次并行下发 `GFramework.Core.Tests` 小切片，累计完成 `20+` 个文件的测试辅助类型拆分
  - 主线程持续复核共享工作树、处理并发编译阻断，并在每一轮后复跑 `GFramework.Core.Tests` Release 构建
  - 在工作树达到 `61` 个变更条目时主动停止扩批，保留对 `100` 文件停止线的充分余量
- 代表性已落地切片：
  - `ArchitectureContextTests.cs`
  - `AsyncQueryExecutorTests.cs`
  - `CommandExecutorTests.cs`
  - `StateTests.cs`
  - `StateMachineTests.cs`
  - `StateMachineSystemTests.cs`
  - `ArchitectureModulesBehaviorTests.cs`
  - `ArchitectureAdditionalCqrsHandlersTests.cs`
  - `QueryCoroutineExtensionsTests.cs`
  - `ObjectPoolTests.cs`
  - `AbstractContextUtilityTests.cs`
  - `EnvironmentTests.cs`
  - `EventBusTests.cs`
  - `ContextAwareTests.cs`
- 验证里程碑：
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet clean`
    - 结果：成功
  - `dotnet build`
    - 结果：成功；`288 Warning(s)`、`0 Error(s)`，唯一位点 `214`
- 当前结论：
  - 本轮主要收益来自 `GFramework.Core.Tests` 内的纯 `MA0048` 大范围收敛
  - 仓库根权威 warning 已从 `353` 降到 `288`，唯一位点从 `279` 降到 `214`
  - 下一波不再适合继续盲目平铺纯拆分，因为剩余 `GFramework.Core.Tests` 热点已开始混入 `CS8766` / `MA0016`
- 下一步：
  1. 提交本轮 warning reduction 与 `ai-plan` 同步。
  2. 下一波优先由主线程处理 `GameContextTests.cs` / `ArchitectureServicesTests.cs` 的混合 warning。
  3. 保持 `YamlConfigSchemaValidator*` 与 `GFramework.Cqrs.Tests/Mediator/*` 为独立高风险波次。

## 2026-04-27 — RP-084

### 阶段：收敛 PR #297 的 CodeRabbit follow-up

- 触发背景：
  - 用户执行 `$gframework-pr-review`，要求以当前分支对应 PR 为准，提取并核对 AI review / check 信号
  - `fetch_current_pr_review.py` 返回 PR `#297` 的最新 head review 中仍有 `3` 个 open threads，另有 `2` 个 folded nitpick 仍然适用
- 主线程实施：
  - 校验 `GFramework.Game/Config/YamlConfigLoader.cs` 后，保留 `ReadYamlAsync` 的原始取消语义，并把 `IntegerTryParseDelegate<T>` 第一个参数改为 `string?`
  - 校验 `GFramework.Core.Tests/Ioc/*` 与 `Query/TestAsyncQueryWithExceptionV4.cs` 后，补齐缺失 XML 文档，让 `IPrioritizedService` 继承 `IMixedService` 复用 `Name` 契约，并补上 `<returns>` 文档
  - 新增 `YamlConfigLoaderTests.ReadYamlAsync_Should_Preserve_OperationCanceledException_When_Cancellation_Is_Requested`，用反射直接命中私有读取路径，稳定回归本次取消语义修复
  - 用 `dotnet format --verify-no-changes --include ...` 清理并验证本次改动文件的格式状态
- 验证里程碑：
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests.ReadYamlAsync_Should_Preserve_OperationCanceledException_When_Cancellation_Is_Requested"`
    - 结果：成功；`1` 通过、`0` 失败
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainerTests.GetAllByPriority_Should_Sort_By_Priority_Ascending"`
    - 结果：成功；`1` 通过、`0` 失败
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet format GFramework.sln --verify-no-changes --include GFramework.Game/Config/YamlConfigLoader.cs GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs GFramework.Core.Tests/Ioc/IMixedService.cs GFramework.Core.Tests/Ioc/IPrioritizedService.cs GFramework.Core.Tests/Ioc/PrioritizedService.cs GFramework.Core.Tests/Query/TestAsyncQueryWithExceptionV4.cs`
    - 结果：成功
- 当前结论：
  - PR `#297` 当前仍然有效的 CodeRabbit open threads 与 folded nitpick 已在本地全部核对并收敛
  - 当前恢复点完成后，分支可以回到 `ArchitectureContextTests.cs` / `AsyncQueryExecutorTests.cs` / `YamlConfigSchemaValidator*` 的 warning reduction 主线
- 下一步：
  1. 提交本轮 PR review follow-up。
  2. 继续执行下一波 `MA0048` 小切片，优先避免一次性进入 `Mediator*` 的高 changed-file 风险波次。

## 2026-04-27 — RP-083

### 阶段：修复 `YamlConfigLoader` 单文件 warning，并拆分 `MicrosoftDiContainerTests` 的辅助类型

- 触发背景：
  - 用户执行 `$gframework-batch-boot 50`，要求先拿仓库根构建 warning，再按 bounded slice 分派给不同 subagent 并持续推进
  - 当前分支在本轮开始时与 `origin/main@b6a9fef` 零提交差异，适合从低风险 warning slice 起步
- 主线程实施：
  - 先执行 non-incremental 仓库根基线：`dotnet clean` + `dotnet build`，得到 `397 Warning(s)` / `316` 个唯一位点
  - 主线程修复 `GFramework.Game/Config/YamlConfigLoader.cs` 的 `MA0051`、`MA0002` 与 `MA0158`
  - 接受一个 worker batch：将 `GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs` 末尾的 `10` 个测试辅助接口/类拆分到 `Ioc/` 同目录独立文件
  - 接受第二波 worker 的已落地结果：将 `GFramework.Core.Tests/Query/AbstractAsyncQueryTests.cs` 末尾的 `7` 个测试辅助类型拆分到 `Query/` 同目录独立文件
  - 启动 `ArchitectureContextTests.cs` 候选 worker，但在共享工作树落地前主动停止，以避免本轮上下文与 review 面积继续膨胀
- 验证里程碑：
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 结果：成功；`111 Warning(s)`、`0 Error(s)`
    - 观察：构建输出未再报告 `GFramework.Game/Config/YamlConfigLoader.cs`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet clean`
    - 结果：成功；刷新最终 non-incremental 仓库根 warning 基线
  - `dotnet build`
    - 结果：成功；`353 Warning(s)`、`0 Error(s)`，唯一位点 `279`
    - 观察：构建输出未再报告 `GFramework.Game/Config/YamlConfigLoader.cs`、`GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs` 与 `GFramework.Core.Tests/Query/AbstractAsyncQueryTests.cs`
- 当前结论：
  - 本轮已完成一个主线程单文件 slice 和两个 worker 拆分 slice；仓库根 non-incremental warning 从 `397` 降到 `353`
  - 当前共享工作树 footprint 为 `22` 个 changed files，仍低于 `$gframework-batch-boot 50` 的停止线
  - 下一波更适合继续处理 `7` 个 `MA0048` 的小文件，而不是立即进入 `Mediator*` 或 `YamlConfigSchemaValidator*` 的高耦合热点

## 活跃风险

- `GFramework.Cqrs.Tests/Mediator/*` 的 `MA0048` 位点密度很高，一次性拆分会迅速推高 changed-file 数。
  - 缓解措施：下一波优先继续拿 `7` warning 级别的小切片。
- `YamlConfigSchemaValidator*` 仍然聚集多类高耦合 warning。
  - 缓解措施：继续维持为独立波次，不与测试项目拆分混提。

## 下一步

1. 完成本轮 `YamlConfigLoader.cs`、`MicrosoftDiContainerTests.cs` 与 `ai-plan` 的提交。
2. 下一波优先从 `ArchitectureContextTests.cs` 或 `AsyncQueryExecutorTests.cs` 继续拆分纯 `MA0048`。

## 历史归档指针

- 最新 trace 归档：
  - [analyzer-warning-reduction-history-rp073-rp078.md](../archive/traces/analyzer-warning-reduction-history-rp073-rp078.md)
  - [analyzer-warning-reduction-history-rp062-rp071.md](../archive/traces/analyzer-warning-reduction-history-rp062-rp071.md)
- 历史 todo 归档：
  - [analyzer-warning-reduction-history-rp074-rp078.md](../archive/todos/analyzer-warning-reduction-history-rp074-rp078.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/todos/analyzer-warning-reduction-history-rp042-rp048.md)
- 早期归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
