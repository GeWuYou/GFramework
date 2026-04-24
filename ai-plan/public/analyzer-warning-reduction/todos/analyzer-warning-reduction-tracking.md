# Analyzer Warning Reduction 跟踪

## 目标

继续以“低风险、可审查、可恢复”为原则收敛 analyzer warning，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-047`
- 当前阶段：`Phase 47`
- 当前焦点：
  - 已重新用经典 logger 形态执行 `dotnet build GFramework.sln -c Release -tl:off -nologo`，当前 solution 基线是 `0 Warning(s)` / `0 Error(s)`
  - 受影响项目 `GFramework.Game` 与 `GFramework.SourceGenerators.Tests` 的 Release build 也已通过，当前工作树中的 warning-reduction 切片至少在编译层面成立
  - 当前 baseline 仍为 `origin/main`（`e692ed3`, `2026-04-24 09:36:17 +0800`）；batch stop condition 仍为 branch diff 接近 `75` 个文件
  - 当前已提交 branch diff 仍只有 `3` 个文件、`234` 行，距离 stop condition 很远；工作树另外保留一批未提交切片待整理与提交

## 当前活跃事实

- `UnifiedSettingsFile.Sections` 已抽象为 `IDictionary<string, string>`；`UnifiedSettingsDataRepository.CloneFile` 会在底层仍是 `Dictionary` 时保留 comparer，避免改变既有键比较语义
- `LocalizationMap` 通过私有 `Dictionary` 字段配合 `IReadOnlyDictionary` 暴露映射，继续避免把可变集合直接暴露给调用方
- `CqrsHandlerRegistryGeneratorTests.cs` 已把一批大型 fixture 提升到类级常量，当前目标是以更低噪音方式消化 `MA0051`
- 当前工作树的 tracked 变更集中在：
  - `GFramework.Game/Data/UnifiedSettingsFile.cs`
  - `GFramework.Game/Data/UnifiedSettingsDataRepository.cs`
  - `GFramework.Godot/Setting/Data/LocalizationMap.cs`
  - `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs`
  - `docs/zh-CN/contributing.md`
  - `docs/zh-CN/troubleshooting.md`
- 当前还存在未跟踪的 `scripts/dotnet-wsl.sh`，用于 WSL Windows-backed worktree 下统一 `dotnet` 环境参数

## 当前风险

- `dotnet build` 默认 terminal logger 输出会折叠成进度视图，不适合作为 warning 基线采样入口
  - 缓解措施：继续使用 `-tl:off` 收集 warning 计数
- 当前工作树仍有多处未提交修改；如果直接继续扩展批次，会降低 reviewability
  - 缓解措施：先整理并提交当前切片，再决定是否继续下一轮 warning cleanup
- `scripts/dotnet-wsl.sh` 与文档更新属于环境治理切片，是否与本轮 warning-reduction 一起提交需要显式判断
  - 缓解措施：提交前按主题拆分 staging，避免把环境文档与 warning 修正混成一个提交

## 活跃文档

- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)

## 验证说明

- `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
  - 结果：成功
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：成功
- `dotnet build GFramework.sln -c Release -tl:off -nologo`
  - 结果：成功；`0 Warning(s)`、`0 Error(s)`、`Time Elapsed 00:00:12.72`

## 下一步建议

1. 先把当前工作树中的 warning-reduction 切片与环境文档切片拆分清楚，避免混合提交
2. 若确认本轮目标只是收口 warning reduction，则优先提交 `Game` / `Godot` / `SourceGenerators.Tests` 相关修改
3. 若 `scripts/dotnet-wsl.sh` 与中文文档属于独立环境治理工作，则单独跟踪或另起提交
