# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-078`
- 当前阶段：`Phase 78`
- 当前焦点：
  - `2026-04-27` 第三轮 `Core.Tests` 低风险 slice 已完成验证，当前建议在 `30 / 50` files 处收口当前波次，而不是继续硬碰高耦合热点
  - 当前轮次已重新确认 `origin/main` 基线与 `HEAD` 同为 `617e0bf`，已提交 branch diff 现为 `30 / 50` files、`642` changed lines
  - 当前剩余 warning 主要集中在 `YamlConfigSchemaValidator*`、`YamlConfigLoader.cs` 与大批量 `MA0048` 文件名拆分；这些 slice 已明显高于本轮“低风险、单文件、可并行收口”的目标

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `617e0bf`（`2026-04-26T12:17:15+08:00`）。
- 提权后的直接仓库根验证当前确认为：
  - `dotnet clean`
    - 结果：成功；此前沙箱内缺失 Windows fallback package folder 的 clean 失败属于环境噪音，不是仓库真值
  - `dotnet build`
    - 最新结果：成功；`397 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 最新结果：成功；`0 Warning(s)`、`0 Error(s)`
- 当前分支 stop-condition 指标：
  - `git diff --name-only refs/remotes/origin/main...HEAD | wc -l`
    - 最新结果：`30`
  - `git diff --numstat refs/remotes/origin/main...HEAD`
    - 最新结果：`642` changed lines
- 当前批次已完成的 warning slice：
  - `GFramework.Core` 事件 / 状态 / 属性 / 协程统计中的 `MA0158` 专用锁迁移
  - `GFramework.Game/Data` 中 `DataRepository`、`UnifiedSettingsDataRepository`、`SaveRepository` 的 `ConfigureAwait` / 比较器 / 专用锁修正
  - `GFramework.Game/Scene/SceneRouterBase.cs` 与 `GFramework.Game/UI/UiRouterBase.cs` 中的显式上下文 / 参数名 / 比较器修正
- 当前批次已完成并提交的收口：
  - `fb0a55f` `fix(analyzer): 收口首轮并行警告清理`
- 当前批次已完成的第三轮 slice：
  - `GFramework.Core.Tests/Concurrency/AsyncKeyLockManagerTests.cs` 的 `MA0004`
  - `GFramework.Core.Tests/Pause/PauseStackManagerTests.cs` 的 `MA0158`
  - `GFramework.Core.Tests/Extensions/AsyncExtensionsTests.cs` 的 `MA0015`
  - `GFramework.Core.Tests/Architectures/ArchitectureModulesBehaviorTests.cs` 的 `MA0004`
- 当前建议保留到下一波次的候选：
  - `GFramework.Game/Config/YamlConfigLoader.cs` 的 `MA0158`（单点可修，但文件本身同时承载其他高耦合 warning）
  - 测试项目中的 `MA0048` 文件名拆分波次（会显著增加 changed-file 数）
- 当前批次验证结果：
  - `dotnet clean`
    - 最新主线程结果：提权直接执行成功，确认为当前权威 clean 基线
  - `dotnet build`
    - 最新主线程结果：提权直接构建成功；`397 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 最新主线程结果：提权直接构建成功；`0 Warning(s)`、`0 Error(s)`

## 当前风险

- `GFramework.Game/Config/YamlConfigSchemaValidator*.cs` 仍然聚集多类高耦合 warning。
  - 缓解措施：本轮先避开该热点，只清理低风险且 ownership 清晰的文件集合。
- `MA0158` 迁移涉及 `net8.0` / `net9.0` / `net10.0` 多目标兼容。
  - 缓解措施：复用 `StoreSelection.cs` 已存在的 `#if NET9_0_OR_GREATER` 专用锁模式，不在 `net8.0` 引入不兼容 API。
- 本轮会并行使用多个 subagent，存在交叉修改风险。
  - 缓解措施：每个 worker 仅拥有互不重叠的文件集合，并要求保留其他 agent 的并发更改。

## 活跃文档

- 当前轮次归档：
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/todos/analyzer-warning-reduction-history-rp042-rp048.md)
- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp062-rp071.md](../archive/traces/analyzer-warning-reduction-history-rp062-rp071.md)
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)

## 验证说明

- 权威验证结果统一维护在“当前活跃事实”和“当前批次验证结果”。
- 后续若刷新构建或 PR review 真值，只更新上述权威区块，不在本节重复抄录。

## 下一步建议

1. 提交第三轮 `Core.Tests` 修复与 `ai-plan` 同步，保持当前恢复点可直接继续。
2. 若后续继续推进，建议另开下一波次，优先明确是否接受 `YamlConfigLoader.cs` 热点触碰，或是否要专门做测试项目 `MA0048` 拆分波次。
3. 默认在当前恢复点停下，因为继续推进已不再符合本轮“低风险、少文件、可并行收口”的原始边界。
