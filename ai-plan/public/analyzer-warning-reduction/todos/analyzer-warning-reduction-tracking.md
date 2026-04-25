# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-063`
- 当前阶段：`Phase 63`
- 当前焦点：
  - `2026-04-25` 当前 turn 先执行 `$gframework-pr-review`，核对 PR #288 的 latest-head AI review 与本地真实状态
  - 已修复 `GFramework.Core.Tests/Extensions/AsyncExtensionsTests.cs` 的 `ConfiguredTaskAwaitable -> Task` 编译错误，并顺手收敛一批同类测试/样式残留
  - 基线 `origin/main` 仍为 `9964962`（`2026-04-24T23:05:53+08:00`）
  - 当前累计 branch diff 相对 `origin/main` 为 `75` 个文件、`2098` 行，已触达本轮 `75 files` 阈值
  - `RP-061` 之后已接受 2 个批次提交：`03c73a8`、`9ce1fa6`
  - 当前默认恢复入口不再继续扩写集；若要继续 analyzer reduction，优先先处理 WSL 下 NuGet fallback package folder 指向失效 Windows 路径的构建环境阻塞

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
- 本 turn 结合 PR #288 latest-head review 额外收敛了以下仍然成立的问题：
  - `AsyncExtensionsTests.cs`：修复 `WithTimeoutAsync` 无返回值测试中错误返回 `ConfiguredTaskAwaitable` 导致的 `CS0029` / `CS1662`
  - `AsyncKeyLockManagerTests.cs`：去掉两处不会产生额外价值的 `Assert.DoesNotThrowAsync(() => Task.WhenAll(...))` 包装，并把取消断言改为直接消费 `ValueTask.AsTask()`
  - `AsyncArchitectureTests.cs`
  - `ArchitectureLifecycleBehaviorTests.cs`
  - `StateMachineSystemTests.cs`
  - `RegistryInitializationHookBaseTests.cs`
  - `NumericExtensions.cs`
  - `StringExtensions.cs`
  - `StoreBuilder.cs`
  - `StoreSelection.cs`
- 当前 PR review 观察：
  - PR：`#288`
  - latest reviewed commit：`be336b2088b7c283a140add76d5cff30618ad16d`
  - `coderabbitai[bot]` 仍有 `7` 个 open threads，`greptile-apps[bot]` 仍有 `2` 个 open threads
  - 本 turn 已优先修复 latest-head 中明确指向 `AsyncExtensionsTests.cs:126` 的 critical 编译错误
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

- 当前环境下 `GFramework.Core` / `GFramework.Core.Tests` 的 Release build 会命中 `MSB4018`。
  - 直接原因：`ResolvePackageAssets` 仍从历史 restore 生成的 `obj/*.csproj.nuget.g.props` 读取失效的 Windows fallback package folder `D:\Tool\Development Tools\Microsoft Visual Studio\Shared\NuGetPackages`。
  - 缓解措施：下次恢复时先重建 WSL 原生 restore 元数据，或显式清理并重做 `obj` 下的 NuGet restore 工件，再重新建立可信 build 基线。
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
  - 历史结果：成功；`0 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-incremental --no-restore -p:RestoreFallbackFolders= -v:diag`
  - 历史结果：失败；`MSB4276`，默认 SDK resolver 无法解析 `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator`，属于当前 WSL / dotnet 10 环境阻塞
- `DOTNET_CLI_HOME=/tmp/dotnet-home MSBuildEnableWorkloadResolver=false dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -v minimal`
  - 结果：失败；`MSB4018`，`ResolvePackageAssets` 命中失效 Windows fallback package folder `D:\Tool\Development Tools\Microsoft Visual Studio\Shared\NuGetPackages`
- `DOTNET_CLI_HOME=/tmp/dotnet-home MSBuildEnableWorkloadResolver=false dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net9.0 -p:RestoreFallbackFolders="" -v minimal`
  - 结果：失败；`MSB4018`，原因同上
- `DOTNET_CLI_HOME=/tmp/dotnet-home MSBuildEnableWorkloadResolver=false dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore -p:TargetFramework=net10.0 -p:RestoreFallbackFolders="" -v minimal`
  - 结果：失败；`MSB4018`，原因同上
- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json`
  - 结果：成功；定位到 PR `#288`，提取 latest-head unresolved AI review threads、MegaLinter 与 Docstring Coverage 信号
- `dotnet restore GFramework.Core.Tests/GFramework.Core.Tests.csproj -p:TestTargetFrameworks=net8.0 -p:RestoreFallbackFolders="" -v minimal`
  - 结果：失败；`NU1201`，`GFramework.Tests.Common` 仅支持 `net10.0`，因此不能用 `net8.0` 旁路验证 `Core.Tests`
- `git diff --name-only origin/main...HEAD | wc -l`
  - 当前结果：`75`
- `git diff --numstat origin/main...HEAD`
  - 当前结果：累计 `1083` added、`1015` deleted，即 `2098` changed lines

## 下一步建议

1. 当前 turn 已先修复 latest-head PR review 中最紧急的编译错误；后续若继续 PR #288 收尾，优先重新抓取 unresolved threads，确认剩余 8 个 open threads 哪些仍成立。
2. 若后续要继续 `Core` / `Core.Tests` warning reduction，先修复 WSL 下 stale NuGet restore metadata 导致的 `MSB4018`，再重新建立可信 build 基线。
3. 若要开启下一轮批处理，优先选择新的 stop-condition（例如新的 file 阈值、warning 目标或限定到单模块）后再继续。
