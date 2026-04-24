# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-058`
- 当前阶段：`Phase 58`
- 当前焦点：
  - `2026-04-24` 使用 `$gframework-pr-review` 复核当前分支 PR #286 的 latest-head review threads、MegaLinter 与测试状态
  - 已确认最新 head 上唯一未解决的实质代码线程指向 `GFramework.Godot/Scene/SceneBehaviorBase.cs` 中 `OnPauseAsync` 的缩进异常，并顺带对齐 `OnResumeAsync`、`OnUnloadAsync`
  - `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release` 通过，结果为 `565 Warning(s)`、`0 Error(s)`；当前跟进只处理 PR review 指向的格式问题，不扩散到既有 warning 基线
  - `dotnet format GFramework.Godot/GFramework.Godot.csproj --verify-no-changes --no-restore --include GFramework.Godot/Scene/SceneBehaviorBase.cs` 已通过，当前文件不再残留格式差异

## 当前活跃事实

- 之前记录的 plain `dotnet build` `0 Warning(s)` 属于增量构建假阴性，不能再作为 warning 检查真值
- 仓库根目录 `dotnet clean GFramework.sln -c Release` 仍在 `ValidateSolutionConfiguration` 阶段失败，项目级 `dotnet clean GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release` 也未能稳定提供 clean 基线
- 当前整仓最近一次直接观测值仍是 `dotnet build GFramework.sln -c Release` 的 `116 warning(s)`
- `RP-056` 已验证 `GeneratedConfigConsumerIntegrationTests.cs` 不再出现在项目 build warning 输出中
- `RP-057` 已验证 `PersistenceTests.cs` 不再出现在 `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-incremental` 的 warning 输出中
- 本轮已验证 `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~UnifiedSettingsDataRepository_SaveAsync_When_Persist_Fails_Should_Keep_Cache_Consistent|FullyQualifiedName~UnifiedSettingsDataRepository_DeleteAsync_When_Persist_Fails_Should_Keep_Cache_Consistent"`，结果为 `Passed: 2`
- `GFramework.Game.Tests` 当前剩余热点已经几乎完全集中到 `YamlConfigLoaderTests.cs` 这一高上下文文件
- PR #286 当前标题为 `Fix/analyzer warning reduction batch`；最新抓取时间点的 PR 状态仍为 `OPEN`
- 最新 reviewed commit 为 `2b707343577193fc9904517e6078149653e95698`，CodeRabbit 于 `2026-04-24T12:44:12Z` 给出 `CHANGES_REQUESTED`
- latest-head review threads 中只有 `1` 个未解决线程，内容是 `SceneBehaviorBase.OnPauseAsync` 的缩进不一致；本地源码已修复并扩展到同段的 `OnResumeAsync` / `OnUnloadAsync`
- MegaLinter 的 `dotnet-format` 详细问题与上述格式异常一致；本地 `dotnet format --verify-no-changes` 已通过
- PR 上其余 nitpick 仅为可选建议或已明确留待后续批次处理，当前没有额外需要立即修复的 latest-head 代码线程

## 当前风险

- 如果后续继续依赖增量 `dotnet build`，容易再次把 warning 数量误判为 0
  - 缓解措施：每轮 warning 检查前先执行 `dotnet clean`，再执行目标 `dotnet build`
- 仓库根目录与 `GFramework.Game.Tests` 的 `dotnet clean` 目前都无法给出新的 clean 基线
  - 缓解措施：后续若继续整仓 warning reduction，需要单独定位 clean 失败原因，或明确继续沿用 direct build 观测值作为临时真值
- 当前 worktree 仍存在未跟踪的 `.codex` 目录
  - 缓解措施：提交当前批次时只暂存 analyzer-warning-reduction 相关源码与 `ai-plan` 文件，避免把工作目录辅助文件混入提交
- 下一轮若继续深入 `GFramework.Game.Tests`，很可能需要进入 `YamlConfigLoaderTests.cs` 这种高上下文大文件
  - 缓解措施：把它单独作为一个明确的新批次处理，不与其它 warning family 混批
- PR 标题检查当前仍显示 `Inconclusive`
  - 缓解措施：如需让该检查转绿，需要单独更新 GitHub PR 标题；这不属于本地代码修改范围

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
  - 结果：成功；`116 Warning(s)`、`0 Error(s)`
- `dotnet clean GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
  - 结果：失败；clean 阶段在 MSBuild 清理路径结束前返回 `0 Warning(s)`、`0 Error(s)`，未输出额外错误文本
- `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
  - `RP-055` 收尾结果：成功；`63 Warning(s)`、`0 Error(s)`
  - `RP-056` 当前结果：成功；`59 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-incremental`
  - `RP-057` 热点重排前：成功；`253 Warning(s)`、`0 Error(s)`
  - `RP-057` 当前结果：成功；`249 Warning(s)`、`0 Error(s)`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureConfigIntegrationTests|FullyQualifiedName~GameConfigBootstrapTests|FullyQualifiedName~JsonSerializerTests"`
  - 结果：成功；`Passed: 19`、`Failed: 0`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedConfigConsumerIntegrationTests"`
  - 结果：成功；`Passed: 4`、`Failed: 0`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~UnifiedSettingsDataRepository_SaveAsync_When_Persist_Fails_Should_Keep_Cache_Consistent|FullyQualifiedName~UnifiedSettingsDataRepository_DeleteAsync_When_Persist_Fails_Should_Keep_Cache_Consistent"`
  - 结果：成功；`Passed: 2`、`Failed: 0`
- `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release`
  - 结果：成功；`565 Warning(s)`、`0 Error(s)`
- `dotnet format GFramework.Godot/GFramework.Godot.csproj --verify-no-changes --no-restore --include GFramework.Godot/Scene/SceneBehaviorBase.cs`
  - 首次运行：失败；restore 阶段异常退出，未进入格式验证
  - 第二次运行（同命令追加 sandbox 提权）：成功；workspace 仅提示加载 warning，无格式差异

## 下一步建议

1. 提交 `SceneBehaviorBase.cs` 与 `RP-058` tracking/trace 更新，清掉 PR #286 当前 latest-head 上的格式类 review thread
2. 若继续 warning reduction 主线，应回到 `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs`，把它作为独立高上下文批次处理
