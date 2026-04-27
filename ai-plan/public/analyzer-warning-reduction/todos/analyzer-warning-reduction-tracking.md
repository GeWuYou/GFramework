# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-086`
- 当前阶段：`Phase 86`
- 当前焦点：
  - `2026-04-27` 已按 `$gframework-batch-boot 100` 连续执行多波 `MA0048` 小切片，当前以 `GFramework.Core.Tests` 的测试辅助类型拆分为主
  - `2026-04-27` 已按 `$gframework-pr-review` 收敛 `PR #298` 的有效 nitpick，修复测试辅助类型的只读暴露、线程安全、空安全与文档一致性问题
  - 本轮已完成 `ArchitectureContextTests`、`AsyncQueryExecutorTests`、`CommandExecutorTests`、`StateTests`、`StateMachineTests`、`StateMachineSystemTests`、`ArchitectureModulesBehaviorTests`、`ArchitectureAdditionalCqrsHandlersTests`、`QueryCoroutineExtensionsTests`、`ObjectPoolTests`、`AbstractContextUtilityTests` 等低风险单文件切片
  - 当前仓库根权威基线已从 `353 Warning(s)` / `279` 个唯一位点下降到 `288 Warning(s)` / `214` 个唯一位点
  - 当前分支下一波更适合转向 `GameContextTests.cs`、`ArchitectureServicesTests.cs`、`RegistryInitializationHookBaseTests.cs` 这类仍在 `GFramework.Core.Tests` 内、但已混入 `CS8766` / `MA0016` 的小型混合切片

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `7cfdd2c`（`2026-04-27T16:59:57+08:00`）。
- 当前直接验证结果：
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 最新结果：成功；`28 Warning(s)`、`0 Error(s)`；当前 warning 来自 `GameContextTests.cs`、`ArchitectureServicesTests.cs`、`RegistryInitializationHookBaseTests.cs` 等既有热点
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build`
    - 最新结果：成功；`1610` 通过、`0` 失败
  - `dotnet clean`
    - 最新结果：成功；已刷新仓库根 non-incremental 基线
  - `dotnet build`
    - 最新结果：成功；`288 Warning(s)`、`0 Error(s)`，唯一位点 `214`
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 最新结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests.ReadYamlAsync_Should_Preserve_OperationCanceledException_When_Cancellation_Is_Requested"`
    - 最新结果：成功；`1` 通过、`0` 失败
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainerTests.GetAllByPriority_Should_Sort_By_Priority_Ascending"`
    - 最新结果：成功；`1` 通过、`0` 失败
  - `dotnet format GFramework.sln --verify-no-changes --include GFramework.Game/Config/YamlConfigLoader.cs GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs GFramework.Core.Tests/Ioc/IMixedService.cs GFramework.Core.Tests/Ioc/IPrioritizedService.cs GFramework.Core.Tests/Ioc/PrioritizedService.cs GFramework.Core.Tests/Query/TestAsyncQueryWithExceptionV4.cs`
    - 最新结果：成功；本次 PR follow-up 改动文件无需额外格式化
- 当前批次摘要：
  - 本轮通过多批并行 worker 共完成 `20+` 个 `GFramework.Core.Tests` 文件的测试辅助类型拆分，集中消化纯 `MA0048` warning 热点
  - 本轮停止时共享工作树共有 `61` 个变更条目，仍低于 `$gframework-batch-boot 100` 的文件停止线
  - 本轮仓库根权威 warning 已从开始时的 `353` 下降到 `288`，且 `GFramework.Core.Tests` 受影响项目的 Release 构建已恢复到 `0 Warning(s)` / `0 Error(s)`
- 当前建议保留到下一波次的候选：
  - `GFramework.Core.Tests/Architectures/GameContextTests.cs` 的 `4` 个 `CS8766` 与 `2` 个 `MA0048`
  - `GFramework.Core.Tests/Architectures/ArchitectureServicesTests.cs` 的 `4` 个 `CS8766` 与 `1` 个 `MA0048`
  - `GFramework.Core.Tests/Architectures/RegistryInitializationHookBaseTests.cs` 的 `1` 个 `MA0016` 与 `5` 个 `MA0048`
  - `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 与 `YamlConfigSchemaValidator.ObjectKeywords.cs` 的高耦合 warning 热点

## 当前风险

- `GFramework.Cqrs.Tests/Mediator/*` 仍有 `47` / `44` / `34` 个唯一 warning 位点，属于高 changed-file 风险的 `MA0048` 大波次。
  - 缓解措施：优先继续处理 `6-7` 个 warning 的小文件切片，避免一次性推高文件数。
- `GameContextTests.cs`、`ArchitectureServicesTests.cs` 这类混合 `CS8766` / `MA0048` 文件不再适合继续用“纯拆分”模式批量下发。
  - 缓解措施：下一波由主线程先局部修正可空签名，再决定是否继续并行拆分。
- `YamlConfigSchemaValidator*` 仍然聚集多类高耦合 warning。
  - 缓解措施：继续把它们留在独立波次，不与测试项目的低风险拆分混提。

## 活跃文档

- 当前轮次归档：
  - [analyzer-warning-reduction-history-rp074-rp078.md](../archive/todos/analyzer-warning-reduction-history-rp074-rp078.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/todos/analyzer-warning-reduction-history-rp042-rp048.md)
- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp073-rp078.md](../archive/traces/analyzer-warning-reduction-history-rp073-rp078.md)
  - [analyzer-warning-reduction-history-rp062-rp071.md](../archive/traces/analyzer-warning-reduction-history-rp062-rp071.md)
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)

## 验证说明

- 权威验证结果统一维护在“当前活跃事实”。
- `GFramework.Core.Tests` 项目级 Release 构建已在本轮清零，但仓库根 non-incremental 构建仍保留大量既有 warning。
- warning reduction 的仓库级真值只以同轮 `dotnet clean` 后的 `dotnet build` 为准。

## 下一步建议

1. 提交本轮多批 `MA0048` warning reduction 与 `ai-plan` 同步。
2. 下一波由主线程先处理 `GameContextTests.cs` / `ArchitectureServicesTests.cs` 的 `CS8766`，再决定是否继续拆分剩余 `MA0048`。
3. 继续将 `YamlConfigSchemaValidator*` 与 `GFramework.Cqrs.Tests/Mediator/*` 作为独立高风险波次处理。
