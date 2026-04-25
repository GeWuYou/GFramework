# Analyzer Warning Reduction 追踪

## 2026-04-25 — RP-062

### 阶段：触达 `$gframework-batch-boot 75` 停止阈值并收口到 `75 files / 1855 lines`

- 触发背景：
  - `RP-061` 收尾时分支相对 `origin/main` 仍只有 `48` 个已提交文件，距离本轮 `75 files` 停止条件还有明显空间
  - 用户明确允许继续委派 subagent，因此主线程继续把低风险机械型写集拆成互不重叠的 test / runtime 小批次
  - 本轮主目标不是继续深挖单个高上下文热点，而是用新的低风险文件精确把 branch diff 推到阈值后停止
- 主线程实施：
  - 先接受并提交 7 文件 `Core.Tests` 收尾批次为 `03c73a8` `test(core-tests): 收敛测试桩与辅助类型 warning`
  - 随后主线程与多个 worker 并行收口以下新增文件：
    - `ArchitectureAdditionalCqrsHandlersTests.cs`
    - `RegistryInitializationHookBaseTests.cs`
    - `CommandCoroutineExtensionsTests.cs`
    - `TaskCoroutineExtensionsTests.cs`
    - `WaitForTaskTTests.cs`
    - `AsyncExtensionsTests.cs`
    - `LogContextTests.cs`
    - `PauseStackManagerTests.cs`
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
  - 将上述 22 文件批次收口为 `9ce1fa6` `refactor(core): 收敛 Core 扩展与测试的机械 warning`
- 验证里程碑：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-incremental --no-restore -p:RestoreFallbackFolders= -v:diag`
    - 结果：失败；`MSB4276`，默认 SDK resolver 缺少 `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator`
  - `dotnet restore GFramework.Core.Tests/GFramework.Core.Tests.csproj -p:TestTargetFrameworks=net8.0 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：失败；`NU1201`，`GFramework.Tests.Common` 仅支持 `net10.0`，不能作为 `Core.Tests` 的 net8 旁路验证
  - `git diff --name-only origin/main...HEAD | wc -l`
    - 结果：`75`
  - `git diff --numstat origin/main...HEAD`
    - 结果：累计 `1115` added、`740` deleted，即 `1855` changed lines
- 当前结论：
  - 本轮 `$gframework-batch-boot 75` 已精确达到主停止条件，默认恢复点应停止在 `9ce1fa6`
  - `Core` runtime 的本轮机械型改动已有可通过的最小 Release build 验证
  - `Core.Tests` 的继续推进当前首先受 `MSB4276` 环境阻塞影响；下一轮若要继续，应先修复构建环境，再重新建立 warning 基线

## 历史归档指针

- 早期 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)
