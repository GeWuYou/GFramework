# Analyzer Warning Reduction 追踪

## 2026-04-21 — RP-013

### 阶段：`MA0046` 事件签名批次收口（RP-013）

- 依据 `RP-012` 的下一步建议，本轮恢复到 `GFramework.Core` 的 `MA0046` 主批次，而不是继续停留在 PR review workflow 优化
- 本地 warnings-only 基线确认当前 `GFramework.Core` `net8.0` 仍有 `6` 个 `MA0046`：
  - `Architecture.cs`
  - `ArchitectureLifecycle.cs`
  - `ArchitecturePhaseCoordinator.cs`
  - `AsyncLogAppender.cs`
  - `CoroutineScheduler.cs` 两处事件
- 方案选择：
  - 不再保留 `Action<...>` 事件签名，统一改为标准 `EventHandler<TEventArgs>`
  - 为 `Architecture`、`AsyncLogAppender` 新增放在 `GFramework.Core.Abstractions` 的事件参数类型
  - 为 `CoroutineScheduler` 新增放在 `GFramework.Core` 的事件参数类型，因为 `CoroutineHandle` 定义在 runtime 层，不适合反向放入 Abstractions
  - `Architecture` 相关事件采用 `Coordinator -> Lifecycle -> Architecture` relay，而不是直接透传底层事件，确保公开事件的 sender 始终是实际发布者，并避免引入新的 `MA0091`
- 同步适配：
  - 更新 `GFramework.Godot/Coroutine/Timing.cs` 的 `OnCoroutineFinished` 订阅签名
  - 更新 `ArchitectureLifecycleBehaviorTests`、`CoroutineSchedulerTests`、`AsyncLogAppenderTests` 以覆盖 sender / event args 契约
  - 更新 `docs/zh-CN/core/architecture.md` 与 `docs/zh-CN/core/lifecycle.md` 的 `PhaseChanged` 示例
- 验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`9 Warning(s)`，`0 Error(s)`；当前 `GFramework.Core` `net8.0` 输出中已无 `MA0046`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureLifecycleBehaviorTests|FullyQualifiedName~CoroutineSchedulerTests|FullyQualifiedName~AsyncLogAppenderTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`50 Passed`，`0 Failed`
  - `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -nologo`
    - 结果：失败；当前 worktree 的 `project.assets.json` 仍引用 Windows fallback package folder，尚未完成 Godot 独立编译验证
- 当前结论：
  - `MA0046` 已从 active 批次中移除
  - 剩余 `GFramework.Core` `net8.0` warning 分布更新为：`MA0016=5`、`MA0002=2`、`MA0015=1`、`MA0077=1`
  - 若继续本主题，下一步默认转入 `MA0016` 批次；若继续触达 Godot，再先修复该项目 restore 资产

## 2026-04-21 — RP-012

### 阶段：PR review workflow 输出收窄增强（RP-012）

- 背景：上一轮虽然脚本已经能解析 `outside_diff_comments`，但直接把超长 JSON 打到终端时仍可能因为输出截断而漏看高价值 review 信号
- 本轮对 `gframework-pr-review` 做了工作流级增强，而不是继续依赖 shell 重定向技巧：
  - 为 `fetch_current_pr_review.py` 增加 `--json-output <path>`，允许把完整 JSON 稳定写入文件
  - 增加 `--section`，可只输出 `outside-diff`、`open-threads`、`megalinter` 等高信号文本摘要
  - 增加 `--path`，允许把文本输出收窄到特定文件或路径片段
  - 增加 `--max-description-length`，避免超长 comment/body 在 text 模式下刷屏
  - 当 text 模式搭配 `--json-output` 时，stdout 保持精简，并显式提示完整 JSON 文件路径
- 同步更新 `SKILL.md`：
  - 将“先落盘，再用 `jq` 或 `--section` / `--path` 缩小范围”写成推荐机器工作流
  - 补充按 section 和按路径聚焦的示例命令
- 预期收益：
  - 不再要求操作者肉眼阅读整份长 JSON
  - outside-diff、nitpick 和 open thread 都能成为一等可过滤输出
  - 即使终端输出有 token/长度上限，完整结果仍可通过文件稳定回查
- 定向验证命令：
  - `python3 -m py_compile .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py`
    - 结果：通过；使用 `PYTHONPYCACHEPREFIX=/tmp/codex-pycache` 规避 `__pycache__` 写入限制
  - `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --help`
    - 结果：通过；新增 CLI 选项均已出现在帮助输出中
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`0 Warning(s)`，`0 Error(s)`
- 下一步建议：
  - 之后执行 `$gframework-pr-review` 时，默认优先使用 `--json-output`
  - 在 review 跟进阶段，先看 `outside-diff`、`open-threads`、`megalinter` 三个 section，再决定是否需要打开完整 JSON

## 2026-04-21 — RP-011

### 阶段：PR #265 outside-diff follow-up 补收口（RP-011）

- 用户补充指出 CodeRabbit 在 `Some comments are outside the diff` 中还有 `GFramework.Core/Events/Event.cs` 的 minor finding：
  默认 no-op 委托会被 `GetInvocationList()` 计入，导致 `GetListenerCount()` 在无监听器和单监听器场景分别返回 `1` 和 `2`
- 本地复核确认该问题仍成立：
  - `Event<T>` 当前字段初始化为 `_ => { }`
  - `Event<T, TK>` 当前字段初始化为 `(_, _) => { }`
  - 两个 `Trigger(...)` 实现本身已是 null-safe，因此无需依赖占位委托规避空引用
- 实施最小修复：
  - 移除两个事件字段的 no-op 初始委托，改为以 `null` 表示“无监听器”
  - 保持 `Register` / `UnRegister` / `Trigger` 的公开 API 和调用方式不变
  - 在 `EventTests` 中新增单参数与双参数 `GetListenerCount()` 回归测试，覆盖初始值、注册后和注销后的计数语义
- 过程说明：
  - 这条不是 skill 设计遗漏；`gframework-pr-review` 的目标本来就包含 latest review body 和 outside-diff 信号
  - 上一轮是我在处理时漏看了这条 outside-diff item，且终端里展示的超长 JSON 输出被截断，未单独把 `Event.cs` 项再抽出来复核
- 定向验证命令：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`15 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~EventTests.EventT_GetListenerCount_Should_Exclude_Placeholder_Handler|FullyQualifiedName~EventTests.EventTTK_GetListenerCount_Should_Exclude_Placeholder_Handler" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`2 Passed`，`0 Failed`
- 下一步建议：
  - 若继续 PR #265 follow-up，只接受当前本地仍成立的剩余 outside-diff 或 unresolved review 项
  - 若没有新的有效 review 点，再恢复到 `MA0046` 主批次

## 2026-04-21 — RP-010

### 阶段：PR #265 follow-up 收口（RP-010）

- 使用 `gframework-pr-review` 抓取当前分支 PR #265 的 latest head review threads、CodeRabbit review body、MegaLinter 摘要与 CTRF
  测试结果；确认最新 unresolved thread 只剩 `CoroutineScheduler` 零容量扩容边界
- 本地复核后确认两处仍成立的风险：
  - `CoroutineScheduler.Expand()` 在 `_slots.Length == 0` 时会把容量从 `0` 扩到 `0`，首次 `Run` 写槽位会越界
  - `Store.EnterDispatchScope()` 在 `_isDispatching = true` 之后、快照构建完成之前若抛异常，会留下永久的嵌套分发误判
- 实施最小修复：
  - 将 `Expand()` 调整为 `Math.Max(1, _slots.Length * 2)`，保持已有倍增策略，只补上零容量边界
  - 为 `EnterDispatchScope()` 增加快照阶段的异常回滚，确保 `_isDispatching` 与实际 dispatch 生命周期保持一致
  - 新增回归测试覆盖零容量启动路径，以及 dispatch 快照阶段抛错后的可恢复性
- 当前 PR 信号复核结论：
  - CTRF：最新评论显示 `2135 passed / 0 failed`
  - MegaLinter：唯一告警仍是 CI 中 `dotnet-format` restore 失败，未发现新的本地代码格式问题
  - 旧 review body 中提到的 `Store` 异常安全问题虽未表现为最新 open thread，但在本地代码中仍可成立，因此一并收口
- 定向验证命令：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`15 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CoroutineSchedulerTests.Run_Should_Grow_From_Zero_Initial_Capacity|FullyQualifiedName~StoreTests.Dispatch_Should_Reset_Dispatching_Flag_When_Snapshot_Creation_Throws" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`2 Passed`，`0 Failed`
- 下一步建议：
  - 若继续本主题，恢复到 `MA0046` 主批次，不再停留在当前 PR follow-up
  - 若 PR review 还出现新线程，继续遵守“只修复当前本地仍成立的问题”的策略

## 2026-04-21 — RP-009

### 阶段：`MA0048` 批次收口（RP-009）

- 依据 `RP-008` 的批处理策略，本轮继续从 `GFramework.Core` 的 `MA0048` 启动，但不采用重命名公共类型的高风险做法；
  改为把同名不同泛型 arity 的家族收拢到与类型名一致的单文件中
- 具体调整：
  - 将 `AbstractCommand<TInput>` 与 `AbstractCommand<TInput, TResult>` 合并进 `AbstractCommand.cs`
  - 将 `AbstractAsyncCommand<TInput>` 与 `AbstractAsyncCommand<TInput, TResult>` 合并进 `AbstractAsyncCommand.cs`
  - 将 `AbstractQuery<TInput, TResult>` 合并进 `AbstractQuery.cs`
  - 将 `AbstractAsyncQuery<TInput, TResult>` 合并进 `AbstractAsyncQuery.cs`
  - 将泛型 `Event<T>` / `Event<T, TK>` 从 `EasyEventGeneric.cs` 迁移到 `Event.cs`
- 首次构建暴露出合并后的 `ICommand<TResult>` / `IQuery<TResult>` 命名空间歧义；随后改用
  `GFramework.Core.Abstractions.*` 的限定名完成最小修正，没有引入行为改动
- 定向验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`15 Warning(s)`，`0 Error(s)`；`MA0048` 已从当前 `GFramework.Core` `net8.0` warnings-only 基线中清空
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CommandExecutorTests|FullyQualifiedName~AbstractAsyncCommandTests|FullyQualifiedName~QueryExecutorTests|FullyQualifiedName~AbstractAsyncQueryTests|FullyQualifiedName~EventTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`83 Passed`，`0 Failed`
- 当前建议的下一批次顺序更新为：
  - 第一优先级：`MA0046`
  - 第二优先级：`MA0016`
  - 顺手吸收：`MA0015`、`MA0077`
  - 单独评估：`MA0002`

## 2026-04-21 — RP-008

### 阶段：批处理策略切换（RP-008）

- 根据当前 `GFramework.Core` warnings-only build 的剩余分布，后续不再默认沿用“单文件、单 warning family”的切片节奏，
  改为按 warning 类型和数量优先级批量推进
- 当前数量基线：
  - `MA0048 = 8`
  - `MA0046 = 6`
  - `MA0016 = 5`
  - `MA0002 = 2`
  - `MA0015 = 1`
  - `MA0077 = 1`
- 新的批处理规则：
  - 先按类型选择主批次，而不是按单文件选切入点
  - 若主批次数量不够，则允许顺手并入其他低冲突类型；`MA0015` 与 `MA0077` 只是当前明显的低数量尾项示例，不是限定范围
  - 单次 `boot` 的工作树改动规模控制在约 `100` 个文件以内，避免 recovery context 和 review 面同时膨胀
  - 当 warning 类型或目录边界清晰且写集不冲突时，允许使用不同模型的 subagent 并行处理，但必须先定义独占 ownership
- 当前建议的下一批次顺序：
  - 第一优先级：`MA0048`
  - 第二优先级：`MA0046`
  - 顺手吸收：其他低冲突类型，当前可见示例包括 `MA0015`、`MA0077`
  - 单独评估：`MA0016`、`MA0002`
- 本轮仅更新 recovery strategy，不改生产代码；验证继续沿用当前基线构建：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`23 Warning(s)`，`0 Error(s)`

## 2026-04-21 — RP-007

### 阶段：CoroutineScheduler `MA0051` 收口（RP-007）

- 依据 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，本轮选择
  `GFramework.Core/Coroutine/CoroutineScheduler.cs`，因为剩余两个 `MA0051` 都集中在协程启动与完成清理路径，且已有
  `CoroutineSchedulerTests`、`CoroutineSchedulerAdvancedTests` 覆盖句柄创建、取消、完成状态、标签分组和等待语义
- 将 `Run` 拆分为：
  - `AllocateSlotIndex`
  - `CreateRunningSlot`
  - `RegisterCancellationCallback`
  - `RegisterStartedCoroutine`
  - `CreateCoroutineMetadata`
  - `ResetCompletionTracking`
- 将 `FinalizeCoroutine` 拆分为：
  - `TryGetFinalizableCoroutine`
  - `UpdateCompletionMetadata`
  - `ApplyCompletionMetadata`
  - `ReleaseCompletedCoroutine`
  - `CompleteCoroutineLifecycle`
- 保持取消回调只做跨线程入队、`Prewarm` 时机、统计记录文本、`RemoveTag` / `RemoveGroup` / `WakeWaiters` 顺序以及
  `OnCoroutineFinished` 的同步触发时机不变，只收缩主方法长度并补齐辅助方法意图注释
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`23 Warning(s)`，`0 Error(s)`；`CoroutineScheduler.cs` 已不再出现在 `MA0051` 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~CoroutineScheduler -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo`
    - 结果：`34 Passed`，`0 Failed`
- 当前 `MA0051` 主线已经在本主题下完成；下一步若继续，应先重新评估剩余 `MA0048`、`MA0046`、`MA0002`、`MA0016` 的
  收敛价值与改动风险，再决定是否开启下一轮 warning family

## 2026-04-21 — RP-006

### 阶段：Store `MA0051` 收口（RP-006）

- 依据 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，本轮选择
  `GFramework.Core/StateManagement/Store.cs`，因为该文件的两个 `MA0051` 都集中在 dispatch / reducer snapshot 逻辑，
  且已有 `StoreTests` 覆盖 dispatch、batch、history 和多态 reducer 匹配语义
- 在正式验证前先处理 WSL 环境噪音：当前 worktree 的 `GFramework.Core/obj/project.assets.json` 是 Windows 侧 restore
  产物，`--no-restore` 构建会继续引用宿主 Windows fallback package folder；本轮先执行一次 Linux 侧
  `dotnet restore GFramework.Core/GFramework.Core.csproj -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> --ignore-failed-sources -nologo`
  刷新资产文件，再继续 warnings-only build
- 将 `Dispatch` 拆分为：
  - `EnterDispatchScope`
  - `TryCommitDispatchResult`
  - `ExitDispatchScope`
- 将 `CreateReducerSnapshotCore` 拆分为：
  - `CreateExactReducerSnapshot`
  - `CreateAssignableReducerSnapshot`
  - `CollectReducerMatches`
  - `CompareReducerMatch`
- 保持 `_dispatchGate -> _lock` 的锁顺序、middleware 锁外执行、批处理通知折叠以及“精确类型 -> 基类 -> 接口 ->
  注册顺序”的 reducer 稳定排序语义不变，只收缩主方法长度并补齐辅助方法意图注释
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`25 Warning(s)`，`0 Error(s)`；`Store.cs` 已不再出现在 `MA0051` 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~StoreTests -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo`
    - 结果：`30 Passed`，`0 Failed`
- 下一步保持同一节奏：只在 `CoroutineScheduler.cs` 的 `Run` / `FinalizeCoroutine` 两个 `MA0051` 中继续，不与其他
  warning 家族混做

## 2026-04-21 — RP-005

### 阶段：PauseStackManager `MA0051` 收口（RP-005）

- 按 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，本轮选择
  `GFramework.Core/Pause/PauseStackManager.cs`，因为该文件体量明显小于 `CoroutineScheduler` 和 `Store`，
  且已有稳定的 `PauseStackManagerTests` 覆盖暂停栈、跨组独立性、事件通知与并发 `Push/Pop` 行为
- 先用 `warnings-only` 定向构建确认 `DestroyAsync` 与 `Pop` 仍分别命中 `MA0051`，再把逻辑拆分为：
  - `TryBeginDestroy`
  - `NotifyDestroyedGroups`
  - `TryPopEntry`
  - `RemoveEntryFromStack`
- 额外抽出 `CreateHandlerSnapshot` 与 `NotifyHandlersSnapshot`，统一普通通知与销毁补发路径的处理器排序和异常日志，
  保持原有“锁内采集快照、锁外调用处理器与事件”的并发策略不变
- 为销毁路径新增 `DestroyAsync_Should_NotifyResumedGroups`，验证当多个暂停组在销毁前仍为暂停态时，
  处理器和事件订阅者都会收到 `IsPaused=false` 的恢复信号
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`27 Warning(s)`，`0 Error(s)`；`PauseStackManager.cs` 已不再出现在 `MA0051` 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~PauseStackManagerTests -p:RestoreFallbackFolders=`
    - 结果：`25 Passed`，`0 Failed`
- 下一步保持原节奏：只在 `CoroutineScheduler` 或 `Store` 中二选一继续，不与其他 warning 家族混做

## 2026-04-21 — RP-003

### 阶段：Architecture 生命周期 `MA0051` 收口（RP-003）

- 依据 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，选定
  `GFramework.Core/Architectures/ArchitectureLifecycle.cs`，因为文件体量适中且已有
  `ArchitectureLifecycleBehaviorTests` 覆盖阶段流转、销毁顺序和 late registration 行为
- 先用 `warnings-only` 定向构建确认 `ArchitectureLifecycle.InitializeAllComponentsAsync` 仍在报
  `MA0051`，随后把主流程拆成：
  - `CreateInitializationPlan`
  - `InitializePhaseComponentsAsync`
  - `MarkInitializationCompleted`
- 保持原有阶段顺序 `Before* -> After*`、批量日志文本和异步初始化策略不变，只压缩主方法长度
- 修正新增 `InitializationPlan` 记录类型的 XML `<param>` 名称大小写，避免引入文档告警
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders= -nologo -clp:Summary;WarningsOnly`
    - 结果：`29 Warning(s)`，`0 Error(s)`；`ArchitectureLifecycle.cs` 已不再出现在 warning 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~ArchitectureLifecycleBehaviorTests -p:RestoreFallbackFolders=`
    - 结果：`6 Passed`，`0 Failed`

## 2026-04-21 — RP-004

### 阶段：PR review follow-up（RP-004）

- 使用 `gframework-pr-review` 抓取当前分支 PR #263 的最新 CodeRabbit review threads、MegaLinter 摘要与 CTRF 测试结果，
  只接受仍能在本地工作树复现的 review 点
- 在 `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 中将 `TryCreateGeneratedRegistry` 的 `out` 参数改为
  `[NotNullWhen(true)] out ICqrsHandlerRegistry?`，移除三处 `null!` 抑制，保持激活失败时的日志文本与回退语义不变
- 修正 active trace 中重复的 `## 2026-04-21` 二级标题，消除 CodeRabbit 报告的 markdownlint `MD024`
- 核实 PR 信号后确认：当前 CTRF 报告为 `2134 passed / 0 failed`；MegaLinter 唯一告警来自 CI 环境中的 `dotnet-format`
  restore 失败，不是本地代码格式问题
- 验证通过：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
    - 结果：`0 Warning(s)`，`0 Error(s)`

## 2026-04-21 — RP-002

### 阶段：CQRS `MA0051` 收口（RP-002）

- 依据 active tracking 中“先只选一个结构性切入点”的约束，选定 `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs`
  作为低风险下一步，因为它已有稳定的 targeted test 覆盖 generated registry、reflection fallback、缓存和重复注册行为
- 将 `TryRegisterGeneratedHandlers` 拆分为 registry 激活、批量注册和 fallback 结果构建三个辅助阶段，同时把
  `GetReflectionFallbackMetadata` 的直接类型解析与按名称解析拆开，降低长方法复杂度但不改日志文本与回退语义
- 顺手修正 `RegisterAssemblyHandlers` 内部调试日志的缩进，未改注册顺序、生命周期或服务描述符写入逻辑
- 验证通过：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter FullyQualifiedName~CqrsHandlerRegistrarTests -p:RestoreFallbackFolders=`
    - 结果：`11 Passed`，`0 Failed`
- 新发现的环境注意事项：
  - 当前 WSL worktree 下若不显式传入 `-p:RestoreFallbackFolders=`，Linux `dotnet` 会读取不存在的 Windows fallback package
    folder 并导致 `ResolvePackageAssets` 失败
  - sandbox 内运行 `dotnet` 会因 MSBuild named-pipe 限制失败；需要在提权上下文中执行 .NET 验证

## 2026-04-19

### 阶段：local-plan 迁移收口（RP-001）

- 复核当前工作树后确认：`local-plan/` 仅保存 analyzer warning reduction 主题的 durable recovery state，不应继续作为
  worktree-root 遗留目录存在
- 按 `ai-plan` 治理规则建立 `ai-plan/public/analyzer-warning-reduction/` 主题目录，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将旧 `local-plan` 中的详细 tracking / trace 迁入主题内历史归档，保留 `RP-001` 的完整实现与验证上下文
- 新建精简版 active tracking / trace 入口，并在 `ai-plan/public/README.md` 中建立
  `fix/analyzer-warning-reduction-batch` -> `analyzer-warning-reduction` 的 worktree 映射
- 删除旧 `local-plan` 文件，避免 `boot` 或后续协作者继续从过时目录恢复
- 验证通过：
  - `find ai-plan/public/analyzer-warning-reduction -maxdepth 3 -type f | sort`
  - `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/analyzer-warning-reduction/archive/todos/analyzer-warning-reduction-history-rp001.md`
- 历史 trace 归档：
  - `ai-plan/public/analyzer-warning-reduction/archive/traces/analyzer-warning-reduction-history-rp001.md`

### 下一步

1. 若继续 analyzer warning reduction，优先回到 `GFramework.Core` 剩余 `MA0051` 热点，并继续保持“单 warning family、单切入点”的节奏
2. 后续所有 WSL 下的 .NET 定向验证命令继续显式附带 `-p:RestoreFallbackFolders=`，避免把环境问题误判成代码回归
