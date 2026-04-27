# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-083`
- 当前阶段：`Phase 83`
- 当前焦点：
  - `2026-04-27` 主线程已修复 `GFramework.Game/Config/YamlConfigLoader.cs` 的 `MA0051`、`MA0002` 与 `MA0158`，当前非增量仓库根构建已不再报告该文件 warning
  - 并行 worker 已将 `GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs` 末尾的 `10` 个测试辅助接口/类拆分到 `Ioc/` 同目录独立文件
  - 已接受第二波 worker 的已落地结果：`GFramework.Core.Tests/Query/AbstractAsyncQueryTests.cs` 末尾辅助类型已拆分到 `Query/` 同目录独立文件
  - 最新 non-incremental 仓库根基线已从 `397` 条 warning / `316` 个唯一位点降到 `353` 条 warning / `279` 个唯一位点
  - 当前剩余 warning 热点仍集中在 `GFramework.Cqrs.Tests/Mediator/*` 的大体量 `MA0048`、以及 `YamlConfigSchemaValidator*` 等高耦合 slice

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `b6a9fef`（`2026-04-27T10:53:34+08:00`）。
- 当前直接验证结果：
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 最新结果：成功；`111 Warning(s)`、`0 Error(s)`，其中不再包含 `GFramework.Game/Config/YamlConfigLoader.cs` 的 warning
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 最新结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet clean`
    - 最新结果：成功；为本轮最终 warning 基线刷新提供非增量起点
  - `dotnet build`
    - 最新结果：成功；`353 Warning(s)`、`0 Error(s)`，唯一 warning 位点 `279`
    - 当前构建输出已不再包含 `GFramework.Game/Config/YamlConfigLoader.cs`、`GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs` 与 `GFramework.Core.Tests/Query/AbstractAsyncQueryTests.cs`
- 当前分支 stop-condition 指标：
  - 当前待提交工作树 footprint：
    - 最新结果：`22` changed files，距离 `$gframework-batch-boot 50` 的停止线仍有余量
- 当前批次摘要：
  - 本轮完成 `YamlConfigLoader.cs` 的单文件 warning 清理，并通过受影响模块 Release 构建验证
  - 本轮完成 `MicrosoftDiContainerTests.cs` 的 ownership-bounded `MA0048` 拆分 slice，新增 `10` 个同目录辅助类型文件并保持测试语义不变
  - 本轮还完成 `AbstractAsyncQueryTests.cs` 的 `MA0048` 拆分 slice，新增 `7` 个同目录辅助类型文件并保持测试语义不变
  - 本轮 non-incremental 仓库根 warning 真值从 `397` 降到 `353`，减少 `44` 条；唯一位点从 `316` 降到 `279`，减少 `37` 个
  - 已尝试为 `ArchitectureContextTests.cs` 启动下一波 subagent，但在共享工作树落地前已停止，不计入本轮已完成事实
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

1. 提交本轮 `YamlConfigLoader.cs`、`MicrosoftDiContainerTests.cs`、`AbstractAsyncQueryTests.cs` 的 warning reduction 结果及 `ai-plan` 同步。
2. 下一波优先挑选 `ArchitectureContextTests.cs` 或 `AsyncQueryExecutorTests.cs` 这类 `7`-warning 的纯 `MA0048` 单文件切片。
3. 继续将 `YamlConfigSchemaValidator*` 与 `GFramework.Cqrs.Tests/Mediator/*` 作为独立高风险波次处理。
