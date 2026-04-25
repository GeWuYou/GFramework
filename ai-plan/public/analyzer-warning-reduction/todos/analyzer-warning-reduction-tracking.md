# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-059`
- 当前阶段：`Phase 59`
- 当前焦点：
  - `2026-04-25` 按 `$gframework-batch-boot 75` 继续 warning reduction，基线使用本地现有 `origin/main`
  - 当前 `HEAD` 与 `origin/main` 同为 `9964962`，已提交 branch diff 为 `0` 个文件；本轮工作树投影为 `1` 个文件、`92` 行
  - 已将 `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs` 中 `44` 处 `Assert.ThrowsAsync(... async () => await ...)` 改为直接返回 `Task`，并为 `WaitForTaskWithinAsync` 补齐 `ConfigureAwait(false)`
  - `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-incremental` 通过，结果从 `249 Warning(s)` 降到 `203 Warning(s)`；该文件不再出现在 `MA0004` 输出中
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~YamlConfigLoaderTests"` 已通过，结果为 `Passed: 74`

## 当前活跃事实

- 之前记录的 plain `dotnet build` `0 Warning(s)` 属于增量构建假阴性，不能再作为 warning 检查真值
- 仓库根目录 `dotnet clean GFramework.sln -c Release` 仍在 `ValidateSolutionConfiguration` 阶段失败，项目级 `dotnet clean GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release` 也未能稳定提供 clean 基线
- 当前整仓最近一次直接观测值仍是 `dotnet build GFramework.sln -c Release` 的 `116 warning(s)`
- `RP-056` 已验证 `GeneratedConfigConsumerIntegrationTests.cs` 不再出现在项目 build warning 输出中
- `RP-057` 已验证 `PersistenceTests.cs` 不再出现在 `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-incremental` 的 warning 输出中
- `RP-059` 已验证 `YamlConfigLoaderTests.cs` 中的 `MA0004` 已清零，当前该文件在项目 build 输出里只剩 `MA0051`
- `GFramework.Game.Tests` 当前 `--no-incremental Release build` 结果为 `203 Warning(s)`、`0 Error(s)`
- 当前 `origin/main` 基线提交为 `9964962`（`2026-04-24T23:05:53+08:00`），与本地 `HEAD` 相同
- 本轮 batch 的主停止条件仍为相对 `origin/main` 的 changed files `< 75`；当前工作树投影仅 `1` 个文件，因此停止原因不是体积，而是剩余切片不再低风险

## 当前风险

- 如果后续继续依赖增量 `dotnet build`，容易再次把 warning 数量误判为 0
  - 缓解措施：每轮 warning 检查前先执行 `dotnet clean`，再执行目标 `dotnet build`
- 仓库根目录与 `GFramework.Game.Tests` 的 `dotnet clean` 目前都无法给出新的 clean 基线
  - 缓解措施：后续若继续整仓 warning reduction，需要单独定位 clean 失败原因，或明确继续沿用 direct build 观测值作为临时真值
- 当前 worktree 仍存在未跟踪的 `.codex` 目录
  - 缓解措施：提交当前批次时只暂存 analyzer-warning-reduction 相关源码与 `ai-plan` 文件，避免把工作目录辅助文件混入提交
- `YamlConfigLoaderTests.cs` 剩余切片已经从机械性的 `MA0004` 进入 `MA0051` 长方法重构，继续自动推进的风险明显高于前几轮
  - 缓解措施：下一轮若继续处理该文件，只处理一到两个长方法，并以 helper 抽取为主，不与其他文件混批

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
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureConfigIntegrationTests|FullyQualifiedName~GameConfigBootstrapTests|FullyQualifiedName~JsonSerializerTests"`
  - 结果：成功；`Passed: 19`、`Failed: 0`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedConfigConsumerIntegrationTests"`
  - 结果：成功；`Passed: 4`、`Failed: 0`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~UnifiedSettingsDataRepository_SaveAsync_When_Persist_Fails_Should_Keep_Cache_Consistent|FullyQualifiedName~UnifiedSettingsDataRepository_DeleteAsync_When_Persist_Fails_Should_Keep_Cache_Consistent"`
  - 结果：成功；`Passed: 2`、`Failed: 0`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~YamlConfigLoaderTests"`
  - 结果：成功；`Passed: 74`、`Failed: 0`

## 下一步建议

1. 提交 `YamlConfigLoaderTests.cs` 与 `RP-059` tracking/trace 更新，保留这一轮单文件 `MA0004` 清理的独立边界
2. 若继续 warning reduction 主线，优先把 `YamlConfigLoaderTests.cs` 的 `MA0051` 作为新的高上下文批次单独处理；若希望继续保持低风险节奏，则改选其它单文件小热点
