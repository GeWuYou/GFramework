# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-050`
- 当前阶段：`Phase 50`
- 当前焦点：
  - warning 基线已修正为仓库根目录执行 `dotnet clean` 后再执行 `dotnet build`
  - `2026-04-24` 用户确认的 clean solution build 结果为 `Build succeeded with 1193 warning(s)`
  - 当前主线程切片为 `GFramework.Godot.SourceGenerators`
  - 当前工作树除未跟踪的 `.codex` 目录外，存在待提交的 source generator / `AGENTS.md` / `ai-plan` 修改

## 当前活跃事实

- 之前记录的 plain `dotnet build` `0 Warning(s)` 属于增量构建假阴性，不能再作为 warning 检查真值
- 本轮已完成 `GFramework.Godot.SourceGenerators` warning 清理：clean `Release` build 从 9 个 warning 降至 0 个 warning
- 当前已确认解决的文件包括 `BindNodeSignalGenerator.cs`、`GetNodeGenerator.cs`、`GodotProjectMetadataGenerator.cs`、`Registration/AutoRegisterExportedCollectionsGenerator.cs`
- 后续 warning-reduction 仍应以 clean solution build 的真实输出为切片来源

## 当前风险

- 如果后续继续依赖增量 `dotnet build`，容易再次把 warning 数量误判为 0
  - 缓解措施：每轮 warning 检查前先执行 `dotnet clean`，再执行目标 `dotnet build`
- 当前只验证了受影响项目 `GFramework.Godot.SourceGenerators`；整仓库 warning 总量仍应以用户确认的 clean solution build 为基线
  - 缓解措施：下一轮从 clean solution build 输出里选择新的低风险 warning 热点继续切片

## 活跃文档

- 当前轮次归档：
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/todos/analyzer-warning-reduction-history-rp042-rp048.md)
- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)

## 验证说明

- `dotnet clean GFramework.Godot.SourceGenerators/GFramework.Godot.SourceGenerators.csproj -c Release`
  - 结果：成功；`0 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Godot.SourceGenerators/GFramework.Godot.SourceGenerators.csproj -c Release`
  - 结果：成功；`0 Warning(s)`、`0 Error(s)`
- `dotnet build`
  - 结果：此前被误记为 `0 Warning(s)`；现已确认这是增量构建假阴性，不再作为有效基线

## 下一步建议

1. 在仓库根目录先执行 `dotnet clean`、再执行 `dotnet build`，重新采集当前 solution 的真实 warning 列表
2. 以 clean build 输出中的下一个低风险热点作为新切片，优先继续 source generator、测试或单模块可局部验证的问题
