# Analyzer Warning Reduction 追踪历史（RP-062 至 RP-071）

## 范围说明

本归档承接 `RP-062` 至 `RP-071` 的 active trace 明细，保留 active 入口在 `RP-072` 压缩前的执行背景、验证里程碑与 PR review 跟进记录。

## superseded by

- [analyzer-warning-reduction-trace.md](../../traces/analyzer-warning-reduction-trace.md)

---

# Analyzer Warning Reduction 追踪

## 2026-04-25 — RP-071

### 阶段：同步 PR #291 latest-head 对 active todo 的唯一剩余线程

- 触发背景：
  - 用户再次显式要求执行 `$gframework-pr-review`，当前分支仍对应 PR `#291`
  - 最新抓取结果显示 latest-head open review thread 只剩 `1` 条，且不再指向源码 warning，而是指向 `ai-plan/public/analyzer-warning-reduction/todos/analyzer-warning-reduction-tracking.md` 中已过时的 `.codex` 风险描述
  - `.gitignore` 已在当前分支的 `chore(git)` 提交中加入 `.codex`，因此 active todo 若继续保留“当前 worktree 仍存在未跟踪的 `.codex` 目录”会与当前 head 真值冲突
- 主线程实施：
  - 重新运行 PR review 抓取脚本，确认 PR `#291` 仍为 `OPEN`，且 latest-head 唯一 open thread 是 CodeRabbit 针对 active todo 的文档同步建议
  - 更新 active todo 恢复点为 `RP-071`，将 `.gitignore` 纳入“已提交的低风险批次文件”，并移除已过时的 `.codex` 活跃风险描述
  - 在 active todo 中明确保留当前仍未采纳的两条 non-blocking nitpick：`VersionedMigrationRunner.cs` 的上下文一致性建议，以及 active trace 归档 RP-062 ~ RP-064 的建议
- 验证里程碑：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output <current-pr-review-json>`
    - 结果：成功；确认 PR `#291` latest-head open review threads 为 `1`，唯一路径为 `ai-plan/public/analyzer-warning-reduction/todos/analyzer-warning-reduction-tracking.md:54`
  - `dotnet build`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`；该次为增量 Debug 构建，只作为本轮文档同步的完成校验，warning 权威基线仍保持 `RP-070` 记录的 `639 Warning(s)`
- 当前结论：
  - 当前 PR `#291` 唯一仍成立的 latest-head thread 已被本地文档同步吸收，后续只需在新 head 推送后复核 GitHub 状态
  - 剩余 CodeRabbit nitpick 仍是可选整理项，本轮保持 analyzer-warning-reduction 的小写集策略，不把它们混入当前提交
  - 下一轮默认先推送并重新抓取 PR review；若 open thread 清零，则继续回到 warning 热点选择

## 2026-04-25 — RP-070

### 阶段：按 PR #291 latest-head review 收口仍有效的小批次，并刷新新的仓库根基线

- 触发背景：
  - 用户显式要求执行 `$gframework-pr-review`，当前分支对应 PR `#291`
  - 抓取结果显示 latest-head 只有 1 条未解决 review thread 指向 `AGENTS.md` 英文标点不一致；同时最新 CodeRabbit review body 还包含 `VersionedMigrationRunner.cs` 参数过多与 `MediatorAdvancedFeaturesTests.cs` 未使用测试基础设施这两条本地仍成立的建议
  - MegaLinter 仅报出 `dotnet-format` restore 失败，test report 为 `2156 passed / 0 failed`，因此本轮重点改为“只吸收仍有效且低风险的 review 建议”
- 主线程实施：
  - 将 `AGENTS.md` 中英文规则段的 `dotnet clean` / `dotnet build` / `dotnet test` 列表标点改为英文逗号，直接消化 latest-head open thread
  - 删除 `GFramework.Cqrs.Tests/Mediator/MediatorAdvancedFeaturesTests.cs` 中未被任何测试引用的 `TestLoggingBehavior` 静态类型，移除无收益的可变测试基础设施
  - 在 `GFramework.Game/Internal/VersionedMigrationRunner.cs` 内引入私有 `MigrationExecutionContext<TData, TMigration>`，把多处 helper 共享的不变迁移上下文收口为参数对象，并同步补齐新增泛型 helper 的 XML 文档
  - 明确拒绝把 `TestLogger` 重复实现与 `YamlConfigLoaderTests.cs` 常量位置这类“可选整理”混入本轮 warning 收敛批次
- 验证里程碑：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output <current-pr-review-json>`
    - 结果：成功；确认 PR `#291` latest-head open review threads 为 `1`，MegaLinter 仅有 `dotnet-format` restore 失败，tests 为 `2156 passed`
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 结果：成功；`326 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
    - 结果：成功；`149 Warning(s)`、`0 Error(s)`
  - `dotnet clean`
    - 结果：成功
  - `dotnet build`
    - 结果：成功；`639 Warning(s)`、`0 Error(s)`，相较 `RP-069` 的 `640` 再下降 `1`
- 当前结论：
  - PR #291 当前最有价值的 review follow-up 已被主线程吸收，且没有把“可选整理”误当成必须修复项
  - 当前仓库根 warning 基线继续下降到 `639`，说明这轮 review 驱动的小批次仍符合 analyzer warning reduction 主题
  - 下一轮可继续围绕 `GFramework.Game` 或 `GFramework.Cqrs.Tests` 选择新的单文件低风险热点，或在新 head 推送后重新抓取 PR review 判断是否还有剩余有效线程

## 2026-04-25 — RP-069

### 阶段：继续收口 Cqrs.Tests 双文件集合抽象 warning，并刷新新的仓库根基线

- 触发背景：
  - `RP-068` 收尾后，当前分支的仓库根基线已降到 `645 Warning(s)`，branch diff 仍远低于 `$gframework-batch-boot 50`
  - 为保持批次小而连续，主线程继续留在 `GFramework.Cqrs.Tests` 项目内，选取两个不涉及跨文件重构的 `MA0016` 切片
- 接受的委派范围：
  - worker `Chandrasekhar`
    - 文件：`GFramework.Cqrs.Tests/Mediator/MediatorAdvancedFeaturesTests.cs`
    - 目标：在同一文件内收敛 `TestLoggingBehavior.LoggedMessages`、`SagaData`、`TestDatabaseRequest` 的集合抽象暴露问题
    - 结果：未自行提交；主线程接受其工作树改动并纳入本轮批次
- 主线程实施：
  - 本地修改 `GFramework.Cqrs.Tests/Logging/TestLogger.cs`
  - 将 `Logs` 从 `List<LogEntry>` 收口为 `IReadOnlyList<LogEntry>`，保留私有 `_logs` 作为内部存储
  - 与 worker 的 `MediatorAdvancedFeaturesTests.cs` 改动合并后，重新执行 `GFramework.Cqrs.Tests` 与仓库根验证，确认双文件批次的净效果
- 验证里程碑：
  - `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet clean`
    - 结果：成功
  - `dotnet build`
    - 结果：成功；`640 Warning(s)`、`0 Error(s)`，相较 `RP-068` 的 `645` 再下降 `5`
- 当前结论：
  - `Cqrs.Tests` 双文件批次已确认有效，并继续压低仓库根 warning 基线
  - 当前分支距离 `$gframework-batch-boot 50` 的停止阈值仍有很大空间，可以继续按“主线程小切片 + subagent 并行单文件”推进
  - 下一轮可优先回到 `GFramework.Core.Tests` 或继续选择新的 `GFramework.Cqrs.Tests` 单文件热点

## 2026-04-25 — RP-068

### 阶段：吸收并行 subagent 小批次，并继续压低仓库根 warning 基线

- 触发背景：
  - `RP-067` 收尾后，当前分支的仓库根基线已降到 `649 Warning(s)`，branch diff 仅 `9 files`
  - 用户明确允许主线程与 subagent 在不冲突的写集上并行推进，因此本轮继续按 `$gframework-batch-boot 50` 规则拆成 3 个单文件切片
- 接受的委派范围：
  - worker `Averroes`
    - 文件：`GFramework.Core.Tests/Logging/LogContextTests.cs`
    - 目标：收敛 `Push_InAsyncContext_ShouldIsolateAcrossThreads()` 内的 `MA0004`
    - 结果：提交 `a7fa70e` `fix(core-tests): 清理 LogContextTests 异步等待 warning`
  - worker `Laplace`
    - 文件：`GFramework.Core.Tests/Logging/LoggerTests.cs`
    - 目标：把 `TestLogger.Logs` 从 `List<LogEntry>` 收口为集合抽象以修复 `MA0016`
    - 结果：提交 `9f6204d` `fix(core-tests): 收口 LoggerTests 日志集合抽象`
- 主线程实施：
  - 本地重构 `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs`
  - 将 `RegisterHandlers_Should_Cache_Assembly_Metadata_Across_Containers()` 中的 mock 装配、handler 类型读取、预期集合断言与 metadata lookup verify 拆分为具名 helper
  - 在吸收两个 worker 提交后，主线程重新执行直接受影响模块与仓库根验证，确保并行切片合并后的真值一致
- 验证里程碑：
  - `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
    - 结果：成功；`155 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~CqrsHandlerRegistrarTests.RegisterHandlers_Should_Cache_Assembly_Metadata_Across_Containers"`
    - 结果：成功；`Passed 1/1`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet clean`
    - 结果：成功
  - `dotnet build`
    - 结果：成功；`645 Warning(s)`、`0 Error(s)`，相较 `RP-067` 的 `649` 再下降 `4`
- 当前结论：
  - 并行 3 文件批次已确认有效，且主线程已把 subagent 的负责范围和验证结果收口进 active trace
  - 当前分支距离 `$gframework-batch-boot 50` 的停止阈值仍有充足空间，可以继续用“主线程验证 + subagent 并行单文件切片”的节奏推进
  - 下一轮可优先回到 `GFramework.Cqrs.Tests` 或 `GFramework.Game` 的单文件 `MA0051` / `MA0016` 热点

## 2026-04-25 — RP-067

### 阶段：收口 Game runtime 单文件长方法切片，并继续压低根构建 warning 基线

- 触发背景：
  - `RP-066` 收尾后，当前分支已通过 `be26640` 把 `YamlConfigLoaderTests.cs` 的 4 个 `MA0051` 落地，仓库根基线降到 `652 Warning(s)`
  - 主线程随后切到 `GFramework.Game/Internal/VersionedMigrationRunner.cs`，继续挑选单文件、低风险、可独立验证的 runtime warning 切片
- 主线程实施：
  - 将 `MigrateToTargetVersion` 中的运行时版本校验、迁移解析、单步应用与结果一致性校验拆分为具名 helper
  - 为新增 helper 补齐 XML 注释，保持该共享迁移执行器的职责边界可读，并避免仅靠机械拆分留下语义不清的私有方法
  - 保持外部行为不变，只收敛长方法 warning，不扩展到存储或日志相关调用方
- 验证里程碑：
  - `dotnet clean`
    - 结果：成功
  - `dotnet build`
    - 结果：成功；`649 Warning(s)`、`0 Error(s)`，相较 `RP-066` 的 `652` 再下降 `3`
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
- 当前结论：
  - `VersionedMigrationRunner.cs` 这个 runtime 单文件批次已被主线程收口，并继续压低仓库根 warning 基线
  - 本轮只新增 1 个源码唯一文件，branch diff 仍显著低于 `$gframework-batch-boot 50` 的主停止阈值
  - 下一轮可以继续挑选 `GFramework.Cqrs.Tests` 或 `GFramework.Game` 的单文件轻量切片，并保持主线程验证、subagent 并行探索的节奏

## 2026-04-25 — RP-066

### 阶段：主线程回收停滞的单文件批次，并继续压低根构建 warning 基线

- 触发背景：
  - `RP-065` 收尾后，`fix/analyzer-warning-reduction-batch` 已通过 `6a704f3` 把 AGENTS / active ai-plan 真值修正和 4 文件测试噪音批次提交到分支
  - 原先负责 `YamlConfigLoaderTests.cs` 的 worker 长时间无结果，主线程收回该单文件批次以避免继续阻塞
- 主线程实施：
  - 关闭停滞 worker，直接重构 `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs`
  - 通过提取固定夹具内容、热重载接线 helper 与共享断言，收敛以下 4 个长方法 warning：
    - `EnableHotReload_Should_Keep_Previous_State_When_Contains_Reference_Dependency_Breaks`
    - `EnableHotReload_Should_Support_Options_Object`
    - `EnableHotReload_Should_Keep_Previous_Table_When_Schema_Change_Makes_Reload_Fail`
    - `EnableHotReload_Should_Keep_Previous_State_When_Dependency_Table_Breaks_Cross_Table_Reference`
  - 在第一次仓库根重建中命中了两个 `CS0411` 泛型推断错误，主线程随即补上显式类型参数并重新建立 clean/build 基线
- 验证里程碑：
  - `dotnet clean`
    - 结果：成功
  - `dotnet build`
    - 结果：成功；`652 Warning(s)`、`0 Error(s)`，相较 `RP-065` 的 `656` 再下降 `4`
  - `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
- 当前结论：
  - `YamlConfigLoaderTests.cs` 这 4 个根构建直接确认的 `MA0051` 已被消化
  - 当前分支在 `6a704f3` 之后的下一提交只会新增 1 个唯一文件，因此 branch diff 仍明显低于 `$gframework-batch-boot 50` 阈值
  - 下一轮可继续选择新的单文件或小写集热点，而不必暂停当前 batch loop

## 2026-04-25 — RP-065

### 阶段：确认 .NET 验证噪音来自沙箱，并把无沙箱直跑写成仓库规则

- 触发背景：
  - 用户明确指出“之前很多清理、构建、测试报错像是环境问题，需要申请权限在沙箱外执行”，并要求把该解决方案写入 `AGENTS.md`
  - 主线程随后在同一 worktree 中对比了沙箱内与提权后直接 shell 的 `dotnet clean` / `dotnet build`
- 主线程实施：
  - 在沙箱内直接运行 `dotnet clean` 时再次复现“Build FAILED but 0 errors”的无诊断噪音
  - 申请提权后重新执行同一条 `dotnet clean`，确认命令可正常完成，说明先前 clean 失败并非仓库真值
  - 在同一提权上下文直接执行 `dotnet build`，拿到当前仓库根权威基线：`656 Warning(s)`、`0 Error(s)`
  - 关闭正在运行的 warning-reduction worker，把工作重心切到仓库治理与 active recovery 文档净化
  - 更新 `AGENTS.md`，新增规则：当沙箱内 `dotnet clean` / `dotnet build` / `dotnet test` 产生缺少诊断、权限错误或其他环境噪音时，必须申请沙箱外重跑同一条直接命令，并以该结果为准
  - 刷新 active todo/trace，把“环境阻塞”从默认恢复入口中降级为历史噪音，不再作为当前真值
- 并行工作：
  - worker 收敛了 4 个低风险测试噪音文件：
    - `GFramework.Game.Tests/Config/GameConfigBootstrapTests.cs`
    - `GFramework.Game.Tests/Config/GeneratedConfigConsumerIntegrationTests.cs`
    - `GFramework.Game.Tests/Config/YamlConfigTextValidatorTests.cs`
    - `GFramework.Ecs.Arch.Tests/Ecs/EcsAdvancedTests.cs`
  - worker 验证：
    - `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
      - worker 初次结果：成功；随后主线程在同一提权环境复核后确认当前为 `0 Warning(s)`、`0 Error(s)`
    - `dotnet build GFramework.Ecs.Arch.Tests/GFramework.Ecs.Arch.Tests.csproj -c Release`
      - 主线程复核：成功；`0 Warning(s)`、`0 Error(s)`
- 当前结论：
  - 本仓库当前在 agent 沙箱内执行 `.NET` 验证时，确实可能出现假失败或缺失诊断
  - 当前应把“提权后的直接 `dotnet` 命令输出”视为仓库真值，而不是继续围绕沙箱噪音扩展 workaround 命令形态
  - `fix/analyzer-warning-reduction-batch` 当前 `HEAD` 已与 `origin/main` 对齐；新的 `$gframework-batch-boot 50` 轮次从 `0 files / 0 lines` committed diff 开始
  - 下一轮低风险热点仍是 `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs` 的 `4` 个 `MA0051`

## 2026-04-25 — RP-064

### 阶段：按标准 WSL build 路径复核 PR #288 建议并完成本轮收口

- 触发背景：
  - 用户指出“在 WSL 里直接执行 `dotnet build` 可以成功”，要求主线程按普通路径重新验证，而不是继续使用带 `MSBuildEnableWorkloadResolver=false`、`--no-restore`、手工 `TargetFramework` 的 workaround 命令
  - 当前任务仍属于 PR #288 review follow-up，因此本轮重点改为“区分哪些 AI 建议值得采纳”以及“用真实 WSL build 结果验证”
- 主线程实施：
  - 重新抓取 PR #288 review，确认 latest-head open threads 为 `CodeRabbit 6 + Greptile 2`
  - 复核 `outside diff + nitpick` 的 19 条建议，只采纳本地仍成立的建议；拒绝把“评论总数”机械等同于“必须全改”
  - 完成以下高信号修复：
    - `ContextAware*` / `AsyncExtensions` / `NumericExtensions` / `StringExtensions` / `StoreBuilder`：回退为 `ArgumentNullException.ThrowIfNull(...)`
    - `ArchitectureServicesTests` / `GameContextTests`：同步 XML `<exception>` 到 `NotSupportedException`
    - `RegistryInitializationHookBaseTests`：修复 override 可空签名实现，避免再次引入编译错误
    - `RollingFileAppenderTests` / `TaskCoroutineExtensionsTests` / `WaitForTaskTests` / `ScopedStorage`：移除无收益噪音代码
    - `FileStorage`：通过 `leaveOpen: true` 修正 `FileStream` 的双重释放语义
    - `SceneRouterBase`：统一显式 `ConfigureAwait(true)` 并补齐引擎线程亲和说明
    - `StoreSelection`：保留 `net9.0+` 的 `System.Threading.Lock`，同时修正条件编译旁的注释写法，避免 `CS1587`
- 验证里程碑：
  - `dotnet restore GFramework.sln -p:RestoreFallbackFolders="" -v minimal`
    - 结果：成功；证明先前 `MSB4018` 来自 stale restore 元数据，而不是当前 WSL 默认 build 路径本身不可用
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
    - 结果：成功；`28 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 结果：成功；`329 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`137 Warning(s)`、`0 Error(s)`
- 当前结论：
  - 用户关于“WSL 里直接 `dotnet build` 可行”的判断正确
  - 前一轮失败的核心原因不是仓库不可构建，而是主线程附加的 workaround 参数改变了 MSBuild 行为
  - 本轮已完成 PR #288 中一组仍成立的建议修复，并重新拿到标准 WSL 路径下的 Release build 验证
  - 剩余 review 线程需要在新 head 上重新抓取后再决定是否逐条 resolve

## 2026-04-25 — RP-063

### 阶段：先收口 PR #288 latest-head 编译错误，再暂停在环境阻塞点并准备提交

- 触发背景：
  - 用户显式要求先执行 `$gframework-pr-review`，并指出 `AsyncExtensionsTests.cs(126,23)` 当前存在 `CS0029` / `CS1662` 构建错误
  - 当前 worktree 仍是 `fix/analyzer-warning-reduction-batch`，因此本 turn 继续沿用 `analyzer-warning-reduction` 的 active recovery 文档
- 主线程实施：
  - 运行 PR review 抓取脚本，确认当前分支对应 PR `#288`
  - 核对 latest-head unresolved review threads 后，优先修复 `AsyncExtensionsTests.cs` 中 `ct => Task.Delay(...).ConfigureAwait(false)` 错误返回 `ConfiguredTaskAwaitable` 的问题
  - 顺手收敛多处已被 latest review 点名且本地仍成立的低风险残留：
    - 测试中的 `async` 无 `await`
    - `ValueTask` 断言包装
    - `RegistryInitializationHookBaseTests.cs` 的可空返回签名
    - `NumericExtensions.cs`、`StringExtensions.cs`、`StoreBuilder.cs` 的 Allman 花括号残留
    - `StoreSelection.cs` 在 `net9.0+` 下切到 `System.Threading.Lock`，同时保留 `net8.0` 兼容分支
- 验证里程碑：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output <current-pr-review-json>`
    - 结果：成功；确认 PR `#288` 的 latest-head unresolved AI review threads 共 `9` 个，其中 `AsyncExtensionsTests.cs:126` 为 critical 编译错误
  - `DOTNET_CLI_HOME=<dotnet-cli-home> MSBuildEnableWorkloadResolver=false dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：失败；`MSB4018`，`ResolvePackageAssets` 仍读取失效 Windows fallback package folder `<stale-windows-fallback-package-folder>`
  - `DOTNET_CLI_HOME=<dotnet-cli-home> MSBuildEnableWorkloadResolver=false dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net9.0 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：失败；原因同上
  - `DOTNET_CLI_HOME=<dotnet-cli-home> MSBuildEnableWorkloadResolver=false dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore -p:TargetFramework=net10.0 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：失败；原因同上
- 当前结论：
  - 用户点名的 `AsyncExtensionsTests.cs` 编译错误已在源码层修复
  - 本 turn 未能拿到新的可通过 Release build，阻塞点已从先前记录的 `MSB4276` 收敛为当前 `obj/*.csproj.nuget.g.props` 中 stale Windows fallback package folder 导致的 `MSB4018`
  - 用户随后要求“先不管这个了，先提交吧”，因此本 turn 在记录环境阻塞后先执行提交收口

## 2026-04-25 — RP-062

### 阶段：触达 `$gframework-batch-boot 75` 停止阈值并收口到 `75 files / 2098 lines`

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
    - 结果：累计 `1083` added、`1015` deleted，即 `2098` changed lines
- 当前结论：
  - 本轮 `$gframework-batch-boot 75` 已精确达到主停止条件，默认恢复点应停止在 `9ce1fa6`
  - `Core` runtime 的本轮机械型改动已有可通过的最小 Release build 验证
  - `Core.Tests` 的继续推进当前首先受 `MSB4276` 环境阻塞影响；下一轮若要继续，应先修复构建环境，再重新建立 warning 基线
