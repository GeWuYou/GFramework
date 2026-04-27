# Analyzer Warning Reduction 追踪

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
