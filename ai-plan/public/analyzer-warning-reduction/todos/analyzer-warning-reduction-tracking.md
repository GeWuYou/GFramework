# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-052`
- 当前阶段：`Phase 52`
- 当前焦点：
  - `2026-04-24` 本轮从当前 PR review 的未解决线程回切到 `GFramework.Game` / `GFramework.Godot.SourceGenerators.Tests`
  - `UnifiedSettingsFile.Sections` 与 `CloneFile` fallback 已对齐为“可保留原 comparer 时保留，否则显式回退到 `StringComparer.Ordinal`”的文档与实现契约
  - `AutoRegisterExportedCollectionsGeneratorTests` 中剩余的 `await test.RunAsync();` 已统一补齐 `.ConfigureAwait(false)`，并同步让 `VerifyDiagnosticsAsync` 内部消费异步等待
  - 当前批次仍需避免混入与 analyzer-warning-reduction 无关的既有工作树改动

## 当前活跃事实

- 之前记录的 plain `dotnet build` `0 Warning(s)` 属于增量构建假阴性，不能再作为 warning 检查真值
- 本轮已完成 `GFramework.Godot.SourceGenerators` warning 清理：clean `Release` build 从 9 个 warning 降至 0 个 warning
- 当前已确认解决的文件包括 `BindNodeSignalGenerator.cs`、`GetNodeGenerator.cs`、`GodotProjectMetadataGenerator.cs`、`Registration/AutoRegisterExportedCollectionsGenerator.cs`
- 本轮直接执行仓库根目录 `dotnet clean` 仍在 `ValidateSolutionConfiguration` 阶段失败，输出未提供具体 error 文本
- 本轮直接执行仓库根目录 `dotnet build` 成功，并给出 `1184 warning(s)` 的真实输出
- `GFramework.Godot.SourceGenerators.Tests` 已通过测试辅助模板抽取与 `ConfigureAwait(false)` 修正，当前 `Debug` / `Release` 构建均为 `0 Warning(s)`
- 本轮已验证 `dotnet test GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj -c Release --no-build`，结果为 `Passed: 48`
- 本轮已把 PR #283 中仍打开的 `UnifiedSettingsDataRepository.cs` comparer 契约线程落到代码与 XML 注释，避免 fallback 语义继续依赖隐式默认 comparer
- 本轮已确认 `AutoRegisterExportedCollectionsGeneratorTests` 的 5 处裸 `await test.RunAsync();` 不是当前 Release build 告警来源，但仍作为 PR review 一致性项一并修正

## 当前风险

- 如果后续继续依赖增量 `dotnet build`，容易再次把 warning 数量误判为 0
  - 缓解措施：每轮 warning 检查前先执行 `dotnet clean`，再执行目标 `dotnet build`
- 仓库根目录 `dotnet clean` 目前仍然无法给出新的 clean 基线
  - 缓解措施：若下一轮继续做整仓 warning reduction，先定位 `dotnet clean` 的 solution-level 失败原因，或明确继续沿用用户确认的 `1193 warning(s)` clean 基线与本轮 `1184 warning(s)` direct build 观测值
- 当前 worktree 已存在与本批次无关的未提交改动
  - 缓解措施：提交当前批次时只暂存 `GFramework.Godot.SourceGenerators.Tests` 与对应 `ai-plan` 文件，避免混入其他 topic 变更
- `GFramework.Game` 当前 `Release` build 仍带有既有 analyzer warning 基线
  - 缓解措施：本轮仅验证改动未新增 `UnifiedSettingsDataRepository` / `UnifiedSettingsFile` 相关 warning；若继续在该模块做 warning reduction，需要另开切片处理现存基线

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

- `dotnet clean`
  - 结果：失败；停在 solution `ValidateSolutionConfiguration`，`0 Warning(s)`、`0 Error(s)`，未输出更具体的 error 文本
- `dotnet build`
  - 结果：成功；`1184 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj`
  - 初始结果：成功；`24 Warning(s)`、`0 Error(s)`
  - 本轮收尾结果：成功；`0 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj -c Release`
  - 结果：成功；`0 Warning(s)`、`0 Error(s)`
- `dotnet test GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj -c Release --no-build`
  - 结果：成功；`Passed: 48`、`Failed: 0`
- `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
  - 结果：成功；`533 Warning(s)`、`0 Error(s)`；模块仍存在既有 warning 基线，本轮 follow-up 仅处理 PR review 指向的 comparer 契约与测试异步等待一致性
- `dotnet build GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj -c Release`
  - 结果：成功；`0 Warning(s)`、`0 Error(s)`
- `dotnet test GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj -c Release --no-build`
  - 结果：成功；`Passed: 48`、`Failed: 0`

## 下一步建议

1. 提交当前 comparer 契约与 `ConfigureAwait(false)` PR follow-up，并确认只纳入本 topic 相关文件
2. 视 PR review 反馈决定是否继续收敛 `GFramework.Game` 现有 warning 基线，或返回下一轮整仓 warning 热点筛选
