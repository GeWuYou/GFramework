# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-064`
- 当前阶段：`Phase 64`
- 当前焦点：
  - `2026-04-25` 当前 turn 先执行 `$gframework-pr-review`，复核 PR #288 的 latest-head unresolved 线程与折叠评论
  - 已收敛一批经本地复核后仍成立的 review 建议，包括 `ThrowIfNull` 回退、测试桩 XML 注释修正、`FileStorage` 资源所有权、`SceneRouterBase` 线程亲和语义与若干测试噪音
  - 已确认用户在 WSL 下直接执行的标准 `dotnet build -c Release` 路径可用；前一轮失败主要来自主线程附加的 workaround 参数而非仓库本身不可构建
  - 基线 `origin/main` 仍为 `9964962`（`2026-04-24T23:05:53+08:00`）
  - 当前累计 branch diff 相对 `origin/main` 为 `75` 个文件、`2098` 行，已触达本轮 `75 files` 阈值
  - `RP-061` 之后已接受 2 个批次提交：`03c73a8`、`9ce1fa6`
  - 当前默认恢复入口不再继续扩写集；若要继续 analyzer reduction，优先重新抓取 PR #288 的 unresolved 线程并按最新 head 再做一轮收口

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
  - `ContextAwareCommandExtensions.cs`
  - `ContextAwareQueryExtensions.cs`
  - `ContextAwareEventExtensions.cs`
  - `AsyncExtensions.cs`
  - `AsyncKeyLockManagerTests.cs`：去掉两处不会产生额外价值的 `Assert.DoesNotThrowAsync(() => Task.WhenAll(...))` 包装，并把取消断言改为直接消费 `ValueTask.AsTask()`
  - `AsyncArchitectureTests.cs`
  - `ArchitectureLifecycleBehaviorTests.cs`
  - `StateMachineSystemTests.cs`
  - `RegistryInitializationHookBaseTests.cs`
  - `NumericExtensions.cs`
  - `StringExtensions.cs`
  - `StoreBuilder.cs`
  - `StoreSelection.cs`
  - `ArchitectureServicesTests.cs`
  - `GameContextTests.cs`
  - `RollingFileAppenderTests.cs`
  - `TaskCoroutineExtensionsTests.cs`
  - `WaitForTaskTests.cs`
  - `ScopedStorage.cs`
  - `FileStorage.cs`
  - `SceneRouterBase.cs`
- 当前 PR review 观察：
  - PR：`#288`
  - latest reviewed commit：`70c42b579f70c90ab5461a02e611c0fbd8d8a6f2`
  - 抓取时 `coderabbitai[bot]` 有 `6` 个 open threads，`greptile-apps[bot]` 有 `2` 个 open threads
  - `Actionable comments posted: 7` 与 `outside diff + nitpick = 19` 并不等于必须全收；本 turn 仅接受经本地复核后仍成立且不与仓库约束冲突的建议
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

- `dotnet clean GFramework.sln -c Release` 与 `dotnet clean GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release` 仍无法稳定提供新的 clean 基线。
  - 缓解措施：后续若继续整仓 warning reduction，需要单独定位 clean 失败原因，或明确继续沿用 direct build 观测值作为临时真值。
- 当前 worktree 仍存在未跟踪的 `.codex` 目录。
  - 缓解措施：提交当前批次时只暂存 analyzer-warning-reduction 相关源码与 `ai-plan` 文件，避免把工作目录辅助文件混入提交。
- 将分支继续推过 `75 files` 会明显降低本轮 reviewability。
  - 缓解措施：当前恢复点默认停止；如需继续，建议在新 turn 明确新的文件阈值或先 rebase / refresh baseline。
- `GFramework.Core`、`GFramework.Game`、`GFramework.Core.Tests` 当前都仍存在模块级历史 warning 基线。
  - 缓解措施：本 turn 已确保本次 touched files 不再引入新的编译错误，并消化了当前 PR review 中仍成立的高信号问题；若要继续 warning reduction，应开新批次按模块系统化收敛。

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
- `dotnet restore GFramework.sln -p:RestoreFallbackFolders="" -v minimal`
  - 结果：成功；已刷新 WSL 原生 restore 元数据，清除先前的 stale fallback package folder 阻塞
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
  - 结果：成功；`28 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
  - 结果：成功；`329 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：成功；`137 Warning(s)`、`0 Error(s)`
- `dotnet restore GFramework.Core.Tests/GFramework.Core.Tests.csproj -p:TestTargetFrameworks=net8.0 -p:RestoreFallbackFolders="" -v minimal`
  - 结果：失败；`NU1201`，`GFramework.Tests.Common` 仅支持 `net10.0`，因此不能用 `net8.0` 旁路验证 `Core.Tests`
- `git diff --name-only origin/main...HEAD | wc -l`
  - 当前结果：`75`
- `git diff --numstat origin/main...HEAD`
  - 当前结果：累计 `1083` added、`1015` deleted，即 `2098` changed lines

## 下一步建议

1. 当前 turn 已按标准 WSL `dotnet build` 路径完成 `Core` / `Game` / `Core.Tests` Release build 验证；后续若继续 PR #288 收尾，优先重新抓取 unresolved threads，确认哪些线程已可直接 resolve。
2. 若后续要继续 `Core` / `Core.Tests` / `Game` warning reduction，应以当前标准 build 输出为新真值，而不是继续沿用上一轮带 workaround 参数的失败命令。
3. 若要开启下一轮批处理，优先选择新的 stop-condition（例如新的 file 阈值、warning 目标或限定到单模块）后再继续。
