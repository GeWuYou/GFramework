# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-060`
- 当前阶段：`Phase 60`
- 当前焦点：
  - `2026-04-25` 继续按 `$gframework-batch-boot 75` 自动推进，并明确允许使用 subagent 处理互不重叠的写集
  - 当前 `HEAD` 为 `b27bcb5`，基线 `origin/main` 仍为 `9964962`
  - 当前累计 branch diff 相对 `origin/main` 为 `9` 个文件、`480` 行，仍远低于 `75 files` 停止阈值
  - 本轮已并行完成 4 个批次：`YamlConfigLoaderTests.cs` 的 4 个纯加载 `MA0051`、`SceneTransitionPipeline.cs` / `UiTransitionPipeline.cs` 的 `MA0004`、3 个配置测试文件的机械型 `MA0004`、以及 `AbstractArchitectureModuleInstallationTests.cs` 的机械型 `MA0004`
  - `GFramework.Game.Tests` 当前 `--no-incremental Release build` 已进一步降到 `189 Warning(s)`；`YamlConfigLoaderTests.cs` 当前只剩热重载相关 `MA0051`

## 当前活跃事实

- 之前记录的 plain `dotnet build` `0 Warning(s)` 属于增量构建假阴性，不能再作为 warning 检查真值
- 仓库根目录 `dotnet clean GFramework.sln -c Release` 仍在 `ValidateSolutionConfiguration` 阶段失败，项目级 `dotnet clean GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release` 也未能稳定提供 clean 基线
- 当前整仓最近一次直接观测值仍是 `dotnet build GFramework.sln -c Release` 的 `116 warning(s)`
- `RP-056` 已验证 `GeneratedConfigConsumerIntegrationTests.cs` 不再出现在项目 build warning 输出中
- `RP-057` 已验证 `PersistenceTests.cs` 不再出现在 `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-incremental` 的 warning 输出中
- `RP-059` 已验证 `YamlConfigLoaderTests.cs` 中的 `MA0004` 已清零，本轮继续把该文件 4 个纯加载 `MA0051` 热点也降掉了
- `GFramework.Game.Tests` 当前 `--no-incremental Release build` 结果为 `189 Warning(s)`、`0 Error(s)`
- `GFramework.Game/GFramework.Game.csproj -c Release` 当前结果为 `519 Warning(s)`、`0 Error(s)`；本轮新增 touched files `SceneTransitionPipeline.cs` 与 `UiTransitionPipeline.cs` 未在可见 warning 输出中残留
- `GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release` 当前结果为 `0 Warning(s)`、`0 Error(s)`；`AbstractArchitectureModuleInstallationTests.cs` 已通过单测复验
- 当前 `origin/main` 基线提交为 `9964962`（`2026-04-24T23:05:53+08:00`）
- 当前累计 branch diff 相对 `origin/main` 为 `9` 个文件、`480` 行；主停止条件仍然是 `75 changed files`

## 当前风险

- 如果后续继续依赖增量 `dotnet build`，容易再次把 warning 数量误判为 0
  - 缓解措施：每轮 warning 检查前先执行 `dotnet clean`，再执行目标 `dotnet build`
- 仓库根目录与 `GFramework.Game.Tests` 的 `dotnet clean` 目前都无法给出新的 clean 基线
  - 缓解措施：后续若继续整仓 warning reduction，需要单独定位 clean 失败原因，或明确继续沿用 direct build 观测值作为临时真值
- 当前 worktree 仍存在未跟踪的 `.codex` 目录
  - 缓解措施：提交当前批次时只暂存 analyzer-warning-reduction 相关源码与 `ai-plan` 文件，避免把工作目录辅助文件混入提交
- `YamlConfigLoaderTests.cs` 剩余切片已经收敛到热重载相关 `MA0051`，继续处理它的单文件收益不再能明显提升 branch diff 文件数
  - 缓解措施：后续优先切回新的单文件热点，只有在缺少低风险新文件时再回到该文件的热重载方法
- 并行 subagent 已经证明能加快批次落地，但主线程仍需逐批复核并统一记录，否则容易让恢复点失真
  - 缓解措施：每轮并行批次完成后先更新 active tracking / trace，再继续下一批

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
  - `RP-059` 当前结果：成功；`203 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-incremental`
  - `RP-060` 当前结果：成功；`189 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
  - `RP-060` 当前结果：成功；`519 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release`
  - `RP-060` 当前结果：成功；`0 Warning(s)`、`0 Error(s)`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureConfigIntegrationTests|FullyQualifiedName~GameConfigBootstrapTests|FullyQualifiedName~JsonSerializerTests"`
  - 结果：成功；`Passed: 19`、`Failed: 0`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedConfigConsumerIntegrationTests"`
  - 结果：成功；`Passed: 4`、`Failed: 0`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~UnifiedSettingsDataRepository_SaveAsync_When_Persist_Fails_Should_Keep_Cache_Consistent|FullyQualifiedName~UnifiedSettingsDataRepository_DeleteAsync_When_Persist_Fails_Should_Keep_Cache_Consistent"`
  - 结果：成功；`Passed: 2`、`Failed: 0`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~YamlConfigLoaderTests"`
  - 结果：成功；`Passed: 74`、`Failed: 0`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~YamlConfigTextValidatorTests|FullyQualifiedName~GameConfigBootstrapTests|FullyQualifiedName~YamlConfigLoaderDependentRequiredTests"`
  - 结果：成功；`Passed: 15`、`Failed: 0`
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~InstallGodotModuleAsync_ShouldThrowBeforeInvokingModuleInstall_WhenAnchorIsMissing"`
  - 结果：成功；`Passed: 1`、`Failed: 0`

## 下一步建议

1. 提交 `RP-060` tracking/trace 更新，固定本轮 4 个并行批次的恢复点与当前 `9 files / 480 lines` 体积
2. 下一轮优先挑新的单文件热点来提升 branch diff 文件数；只有在新文件候选不够低风险时，再回到 `YamlConfigLoaderTests.cs` 的热重载 `MA0051`
