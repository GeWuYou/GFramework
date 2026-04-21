# Analyzer Warning Reduction 跟踪

## 目标

继续以“优先低风险、保持行为兼容”为原则收敛当前仓库的 Meziantou analyzer warnings，并在首轮大规模清理完成后，
判断剩余结构性 warning 是否值得在下一轮继续推进。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-005`
- 当前阶段：`Phase 5`
- 当前焦点：
  - 已完成 `GFramework.Core/Pause/PauseStackManager.cs` 的 `MA0051` 收口：将 `DestroyAsync` 与 `Pop` 拆分为锁内状态迁移、
    栈调整和锁外通知三个阶段，同时保持日志、事件与销毁补发语义不变
  - 已为销毁路径补充 `PauseStackManagerTests.DestroyAsync_Should_NotifyResumedGroups` 回归测试，覆盖“销毁时向所有仍暂停组补发恢复通知”
  - 下一轮若继续推进，优先在 `CoroutineScheduler` 或 `Store` 的剩余 `MA0051` 中只选一个切入点，不回到已完成的
    `PauseStackManager`

## 当前状态摘要

- 已完成 `GFramework.Core`、`GFramework.Cqrs`、`GFramework.Godot` 与部分 source generator 的低风险 warning 清理
- 已完成多轮 CodeRabbit follow-up 修复，并用定向测试与项目/解决方案构建验证了关键回归风险
- 当前 `PauseStackManager` 的长方法 warning 已从 active 入口移除；主题内剩余 warning 主要集中在
  `GFramework.Core/Coroutine/CoroutineScheduler.cs`、`GFramework.Core/StateManagement/Store.cs`、文件/类型命名冲突、
  delegate 形状和少量公共集合抽象接口问题

## 当前活跃事实

- 当前主题仍是 active topic，因为剩余结构性 warning 是否继续推进尚未决策
- `RP-001` 的详细实现历史、测试记录和 warning 热点清单已归档到主题内 `archive/`
- `RP-002` 已在不改公共契约的前提下完成 `CqrsHandlerRegistrar` 结构拆分，并通过定向 build/test 验证
- `RP-003` 已在不改生命周期契约的前提下完成 `ArchitectureLifecycle` 初始化主流程拆分，并通过定向 build/test 验证
- `RP-004` 已完成当前 PR review follow-up：修复 `TryCreateGeneratedRegistry` 的可空 `out` 契约并清理 trace 文档重复标题
- `RP-005` 已在不改公共 API 的前提下完成 `PauseStackManager` 两个 `MA0051` 的结构拆分，并补充销毁通知回归测试
- 当前工作树分支 `fix/analyzer-warning-reduction-batch` 已在 `ai-plan/public/README.md` 建立 topic 映射

## 当前风险

- 结构性重构风险：剩余 `GFramework.Core` 侧 `MA0051` 与 `MA0048` 可能要求较大的文件拆分或类型重命名
  - 缓解措施：只在下一轮明确接受结构调整成本时再继续推进，不在恢复点模糊的情况下顺手扩面
- 测试宿主稳定性风险：部分 Godot 失败路径在当前 .NET 测试宿主下仍不稳定
  - 缓解措施：继续优先使用稳定的 targeted test、项目构建和相邻 smoke test 组合验证
- 多目标框架 warning 解释风险：同一源位置会在多个 target framework 下重复计数
  - 缓解措施：继续以唯一源位置和 warning 家族为主要决策依据，而不是只看原始 warning 总数

## 活跃文档

- 历史跟踪归档：[analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
- 历史 trace 归档：[analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)

## 验证说明

- `RP-001` 的详细 warning 清理、回归修复与定向验证命令均已迁入主题内历史归档
- `RP-002` 的定向验证结果：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter FullyQualifiedName~CqrsHandlerRegistrarTests -p:RestoreFallbackFolders=`
- `RP-003` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders= -nologo -clp:Summary;WarningsOnly`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~ArchitectureLifecycleBehaviorTests -p:RestoreFallbackFolders=`
- `RP-004` 的定向验证结果：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
    - 结果：`0 Warning(s)`，`0 Error(s)`
- `RP-005` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`27 Warning(s)`，`0 Error(s)`；`PauseStackManager.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~PauseStackManagerTests -p:RestoreFallbackFolders=`
    - 结果：`25 Passed`，`0 Failed`
- active 跟踪文件只保留当前恢复点、活跃事实、风险与下一步，不再重复保存已完成阶段的长篇历史

## 下一步

1. 若要继续该主题，先读 active tracking，再按需展开历史归档中的 warning 热点与验证记录
2. 优先在 `GFramework.Core/Coroutine/CoroutineScheduler.cs` 与 `GFramework.Core/StateManagement/Store.cs`
   的 `MA0051` 中只选一个继续，不要在同一轮同时扩多个风险面
3. 若本主题确认暂缓，可保持当前归档状态，不需要再恢复 `local-plan/`
