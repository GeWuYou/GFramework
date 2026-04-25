# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-062`
- 当前阶段：`Phase 62`
- 当前焦点：
  - `2026-04-25` 本轮 `$gframework-batch-boot 75` 已达到主停止条件，当前代码 stop commit 为 `9ce1fa6`
  - 基线 `origin/main` 仍为 `9964962`（`2026-04-24T23:05:53+08:00`）
  - 当前累计 branch diff 相对 `origin/main` 为 `75` 个文件、`2098` 行，已触达本轮 `75 files` 阈值
  - `RP-061` 之后已接受 2 个批次提交：`03c73a8`、`9ce1fa6`
  - 当前默认恢复入口不再继续扩写集；若要继续 analyzer reduction，优先先处理 `GFramework.Core.Tests` 的 net10 build 环境阻塞

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `9964962`（`2026-04-24T23:05:53+08:00`）。
- 本轮 `Core.Tests` 低风险机械型清理已落地到：
  - `ArchitectureAdditionalCqrsHandlersTests.cs`
  - `RegistryInitializationHookBaseTests.cs`
  - `CommandCoroutineExtensionsTests.cs`
  - `TaskCoroutineExtensionsTests.cs`
  - `WaitForTaskTTests.cs`
  - `AsyncExtensionsTests.cs`
  - `LogContextTests.cs`
  - `PauseStackManagerTests.cs`
- 本轮 `Core` runtime 低风险机械型清理已落地到：
  - `AsyncExtensions.cs`
  - `CollectionExtensions.cs`
  - `ContextAwareCommandExtensions.cs`
  - `ContextAwareEnvironmentExtensions.cs`
  - `ContextAwareEventExtensions.cs`
  - `ContextAwareQueryExtensions.cs`
  - `ContextAwareServiceExtensions.cs`
  - `GuardExtensions.cs`
  - `NumericExtensions.cs`
  - `StoreEventBusExtensions.cs`
  - `StringExtensions.cs`
  - `StoreBuilder.cs`
  - `StoreSelection.cs`
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -v minimal` 当前结果为 `0 Warning(s)`、`0 Error(s)`，可作为本轮 runtime 变更的最终最小 Release build 验证。
- `GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-incremental` 在 `03c73a8` 提交前的最近一次可信主线程结果为 `198 Warning(s)`、`0 Error(s)`；该观测值覆盖了 `ArchitectureContextTests`、`ArchitectureServicesTests`、`GameContextTests`、`ResultTests`、`AsyncTestModel`、`AsyncTestSystem` 与 `ContextAwareEnvironmentExtensionsTests` 的 7 文件批次。
- 当前累计 branch diff 相对 `origin/main` 为 `75` 个文件、`2098` 行；本轮主停止条件已经达到。

## 当前风险

- 当前环境下 `GFramework.Core.Tests` 的默认 net10 build 会在 SDK resolver 阶段命中 `MSB4276`。
  - 缓解措施：继续该 topic 前，优先修复或绕过 `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator` 缺件；不要把该环境失败误判成当前 22 文件批次的代码回归。
- `dotnet clean GFramework.sln -c Release` 与 `dotnet clean GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release` 仍无法稳定提供新的 clean 基线。
  - 缓解措施：后续若继续整仓 warning reduction，需要单独定位 clean 失败原因，或明确继续沿用 direct build 观测值作为临时真值。
- 当前 worktree 仍存在未跟踪的 `.codex` 目录。
  - 缓解措施：提交当前批次时只暂存 analyzer-warning-reduction 相关源码与 `ai-plan` 文件，避免把工作目录辅助文件混入提交。
- 将分支继续推过 `75 files` 会明显降低本轮 reviewability。
  - 缓解措施：当前恢复点默认停止；如需继续，建议在新 turn 明确新的文件阈值或先 rebase / refresh baseline。

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

- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -v minimal`
  - 结果：成功；`0 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-incremental --no-restore -p:RestoreFallbackFolders= -v:diag`
  - 结果：失败；`MSB4276`，默认 SDK resolver 无法解析 `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator`，属于当前 WSL / dotnet 10 环境阻塞
- `dotnet restore GFramework.Core.Tests/GFramework.Core.Tests.csproj -p:TestTargetFrameworks=net8.0 -p:RestoreFallbackFolders="" -v minimal`
  - 结果：失败；`NU1201`，`GFramework.Tests.Common` 仅支持 `net10.0`，因此不能用 `net8.0` 旁路验证 `Core.Tests`
- `git diff --name-only origin/main...HEAD | wc -l`
  - 当前结果：`75`
- `git diff --numstat origin/main...HEAD`
  - 当前结果：累计 `1083` added、`1015` deleted，即 `2098` changed lines

## 下一步建议

1. 当前 `$gframework-batch-boot 75` 已达到阈值；默认在 `9ce1fa6` 停止，不再继续扩写集。
2. 若后续要继续 `Core` / `Core.Tests` warning reduction，先解决 `GFramework.Core.Tests` 的 `MSB4276` 环境阻塞，再重新建立可信 warning 基线。
3. 若要开启下一轮批处理，优先选择新的 stop-condition（例如新的 file 阈值、warning 目标或限定到单模块）后再继续。
