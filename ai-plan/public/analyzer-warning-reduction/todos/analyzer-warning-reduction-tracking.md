# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-053`
- 当前阶段：`Phase 53`
- 当前焦点：
  - `2026-04-24` 本轮按 `$gframework-batch-boot 75` 重新建立当前分支相对 `origin/main` 的 batch 基线，并从整仓 `Release` build 里挑选低风险热点
  - `GFramework.Godot` 已完成一轮独立 warning 清理：`GodotYamlConfigEnvironment` 的目录枚举逻辑已拆分 helper，`AbstractArchitecture` / `SceneBehaviorBase` 中需要保留 Godot 同步上下文的 await 已显式改为 `.ConfigureAwait(true)`
  - `GFramework.Godot.Tests` 已同步清理对应测试 warning：异步断言显式使用 `.ConfigureAwait(false)`，`RichTextMarkupTests` 中测试字典显式指定 `StringComparer.Ordinal`
  - 当前代码批次相对 `origin/main` 的待提交 diff 为 `6` 个文件、`107` 行变更，远低于 `$gframework-batch-boot 75` 的主停止阈值；本轮在这里收口，是因为下一批候选将进入 `GFramework.Game` 等高基线模块，而不再是同等级低风险切片

## 当前活跃事实

- 之前记录的 plain `dotnet build` `0 Warning(s)` 属于增量构建假阴性，不能再作为 warning 检查真值
- 本轮直接执行仓库根目录 `dotnet clean` 仍在 `ValidateSolutionConfiguration` 阶段失败，输出未提供具体 error 文本
- 本轮直接执行仓库根目录 `dotnet build GFramework.sln -c Release` 成功，并给出 `1122 warning(s)` 的当前整仓观测值
- `GFramework.Godot/GFramework.Godot.csproj -c Release` 在本轮收尾验证中已达到 `0 Warning(s)`、`0 Error(s)`
- `GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release` 在串行复验中已达到 `0 Warning(s)`、`0 Error(s)`
- 本轮已验证 `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~AbstractArchitectureModuleInstallationTests|FullyQualifiedName~GodotYamlConfigLoaderTests|FullyQualifiedName~RichTextMarkupTests"`，结果为 `Passed: 15`
- `GFramework.Godot` 原先暴露的 `MA0051` 与 `MA0004` 热点都已清理完成；当前同域低风险切片基本耗尽

## 当前风险

- 如果后续继续依赖增量 `dotnet build`，容易再次把 warning 数量误判为 0
  - 缓解措施：每轮 warning 检查前先执行 `dotnet clean`，再执行目标 `dotnet build`
- 仓库根目录 `dotnet clean` 目前仍然无法给出新的 clean 基线
  - 缓解措施：若下一轮继续做整仓 warning reduction，先定位 `dotnet clean` 的 solution-level 失败原因，或明确继续沿用用户确认的 `1193 warning(s)` clean 基线与本轮 `1122 warning(s)` direct build 观测值
- 当前 worktree 仍存在未跟踪的 `.codex` 目录
  - 缓解措施：提交当前批次时只暂存 analyzer-warning-reduction 相关源码与 `ai-plan` 文件，避免把工作目录辅助文件混入提交
- 下一轮最明显的剩余热点将转入 `GFramework.Game` 等高 warning 基线模块
  - 缓解措施：恢复时先重新跑整仓 build 热点筛选，再决定是否接受更高上下文成本的 `GFramework.Game` 切片，或先排查 solution-level clean 失败原因

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

- `dotnet clean GFramework.sln -c Release`
  - 结果：失败；停在 solution `ValidateSolutionConfiguration`，`0 Warning(s)`、`0 Error(s)`，未输出更具体的 error 文本
- `dotnet build GFramework.sln -c Release`
  - 结果：成功；`1122 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release`
  - 结果：成功；`0 Warning(s)`、`0 Error(s)`
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~AbstractArchitectureModuleInstallationTests|FullyQualifiedName~GodotYamlConfigLoaderTests|FullyQualifiedName~RichTextMarkupTests"`
  - 结果：成功；`Passed: 15`、`Failed: 0`
- `dotnet build GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release`
  - 首次并行验证：成功；`1 Warning(s)`、`0 Error(s)`；`MSB3026` 来自与并行 `dotnet test` 竞争同一输出 DLL
  - 串行复验：成功；`0 Warning(s)`、`0 Error(s)`

## 下一步建议

1. 提交当前 `GFramework.Godot` / `GFramework.Godot.Tests` warning 清理批次，并继续保持只纳入本 topic 相关文件
2. 下一轮若继续使用 `$gframework-batch-boot 75`，先决定是优先排查 solution-level `dotnet clean` 失败，还是接受更高上下文成本进入 `GFramework.Game` warning 热点
