# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-084`
- 当前阶段：`Phase 84`
- 当前焦点：
  - `2026-04-27` 已完成 PR `#297` 的 CodeRabbit follow-up，修复 `YamlConfigLoader` 的取消语义与 `IntegerTryParseDelegate` 可空性问题
  - 已补齐 `GFramework.Core.Tests/Ioc` 与 `GFramework.Core.Tests/Query` 中 review 指向的 XML 文档缺口，并让 `IPrioritizedService` 复用 `IMixedService.Name` 契约
  - 已新增 `YamlConfigLoaderTests` 回归测试，锁定“取消时保留 `OperationCanceledException`”这一行为
  - 当前分支的下一波 warning reduction 仍建议回到 `ArchitectureContextTests.cs`、`AsyncQueryExecutorTests.cs` 或 `YamlConfigSchemaValidator*` 的后续 slice

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `b6a9fef`（`2026-04-27T10:53:34+08:00`）。
- 当前直接验证结果：
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 最新结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 最新结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests.ReadYamlAsync_Should_Preserve_OperationCanceledException_When_Cancellation_Is_Requested"`
    - 最新结果：成功；`1` 通过、`0` 失败
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainerTests.GetAllByPriority_Should_Sort_By_Priority_Ascending"`
    - 最新结果：成功；`1` 通过、`0` 失败
  - `dotnet format GFramework.sln --verify-no-changes --include GFramework.Game/Config/YamlConfigLoader.cs GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs GFramework.Core.Tests/Ioc/IMixedService.cs GFramework.Core.Tests/Ioc/IPrioritizedService.cs GFramework.Core.Tests/Ioc/PrioritizedService.cs GFramework.Core.Tests/Query/TestAsyncQueryWithExceptionV4.cs`
    - 最新结果：成功；本次 PR follow-up 改动文件无需额外格式化
- 当前批次摘要：
  - 本轮完成 PR `#297` 最新 head review 中仍然有效的 `3` 个 open threads 修复：`YamlConfigLoader` 取消语义、`IMixedService.Name` XML 文档、`IPrioritizedService` 相关契约整理
  - 本轮同时吸收 CodeRabbit folded nitpick 中仍然成立的 `2` 个点：`IntegerTryParseDelegate` 可空性对齐、`TestAsyncQueryWithExceptionV4.OnDoAsync` 的 `<returns>` 文档
  - 本轮新增一条精确回归测试，确保底层 YAML 文件读取在取消时继续抛出 `OperationCanceledException` 系列异常，而不是包装成 `ConfigLoadException`
- 当前建议保留到下一波次的候选：
  - `GFramework.Core.Tests/Architectures/ArchitectureContextTests.cs` 的 `7` 个 `MA0048`
  - `GFramework.Core.Tests/Query/AsyncQueryExecutorTests.cs` 的 `7` 个 `MA0048`
  - `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 与 `YamlConfigSchemaValidator.ObjectKeywords.cs` 的高耦合 warning 热点

## 当前风险

- `GFramework.Cqrs.Tests/Mediator/*` 仍有 `47` / `44` / `34` 个唯一 warning 位点，属于高 changed-file 风险的 `MA0048` 大波次。
  - 缓解措施：优先继续处理 `6-7` 个 warning 的小文件切片，避免一次性推高文件数。
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

1. 提交本轮 PR `#297` review follow-up 与 `ai-plan` 同步。
2. 下一波优先挑选 `ArchitectureContextTests.cs` 或 `AsyncQueryExecutorTests.cs` 这类 `7`-warning 的纯 `MA0048` 单文件切片。
3. 继续将 `YamlConfigSchemaValidator*` 与 `GFramework.Cqrs.Tests/Mediator/*` 作为独立高风险波次处理。
