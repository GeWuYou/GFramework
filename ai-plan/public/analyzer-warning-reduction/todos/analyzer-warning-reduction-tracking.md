# Analyzer Warning Reduction 跟踪

## 目标

继续以“优先低风险、保持行为兼容”为原则收敛当前仓库的 Meziantou analyzer warnings，并在首轮大规模清理完成后，
判断剩余结构性 warning 是否值得在下一轮继续推进。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-008`
- 当前阶段：`Phase 8`
- 当前焦点：
  - 当前 `MA0051` 主线已完成；后续改为按 warning 类型和数量批处理，而不是继续按单文件切片推进
  - 优先批量收敛当前数量最高且风险较低的类型；当某一轮主类型数量不足时，允许顺手合并其他低冲突 warning 类型，
    `MA0015` 与 `MA0077` 只是当前最明显的低数量示例，不构成限定
  - 单次 `boot` 的工作树改动上限控制在约 `100` 个文件以内，避免 recovery context 与 review 面同时失控
  - 若任务边界互不冲突，允许使用不同模型的 subagent 并行处理不同 warning 类型或不同目录，但必须遵守显式 ownership

## 当前状态摘要

- 已完成 `GFramework.Core`、`GFramework.Cqrs`、`GFramework.Godot` 与部分 source generator 的低风险 warning 清理
- 已完成多轮 CodeRabbit follow-up 修复，并用定向测试与项目/解决方案构建验证了关键回归风险
- 当前 `PauseStackManager`、`Store` 与 `CoroutineScheduler` 的长方法 warning 已从 active 入口移除；主题内剩余 warning
  主要集中在 `MA0048` 文件/类型命名冲突、`MA0046` delegate 形状、`MA0016` 集合抽象接口、`MA0002` comparer 重载，
  以及 `MA0015` / `MA0077` 两个低数量尾项

## 当前活跃事实

- 当前主题仍是 active topic，因为剩余结构性 warning 是否继续推进尚未决策
- `RP-001` 的详细实现历史、测试记录和 warning 热点清单已归档到主题内 `archive/`
- `RP-002` 已在不改公共契约的前提下完成 `CqrsHandlerRegistrar` 结构拆分，并通过定向 build/test 验证
- `RP-003` 已在不改生命周期契约的前提下完成 `ArchitectureLifecycle` 初始化主流程拆分，并通过定向 build/test 验证
- `RP-004` 已完成当前 PR review follow-up：修复 `TryCreateGeneratedRegistry` 的可空 `out` 契约并清理 trace 文档重复标题
- `RP-005` 已在不改公共 API 的前提下完成 `PauseStackManager` 两个 `MA0051` 的结构拆分，并补充销毁通知回归测试
- `RP-006` 已在不改公共 API 的前提下完成 `Store` 两个 `MA0051` 的结构拆分，并通过定向 build/test 验证 dispatch、
  多态 reducer 匹配与历史语义未回归
- `RP-007` 已在不改公共 API 的前提下完成 `CoroutineScheduler` 两个 `MA0051` 的结构拆分，并通过定向 build/test 验证
  调度、取消与完成状态语义未回归
- `RP-008` 将后续策略从“单文件 warning 切片”切换为“按类型批处理 + 文件数上限控制”，并允许在非冲突前提下使用
  不同模型的 subagent 并行处理
- 当前工作树分支 `fix/analyzer-warning-reduction-batch` 已在 `ai-plan/public/README.md` 建立 topic 映射

## 当前风险

- 结构性重构风险：剩余 `GFramework.Core` 侧 `MA0051` 与 `MA0048` 可能要求较大的文件拆分或类型重命名
  - 缓解措施：按 warning 类型分批推进，并把单次 `boot` 的文件改动规模控制在约 `100` 个文件以内
- 测试宿主稳定性风险：部分 Godot 失败路径在当前 .NET 测试宿主下仍不稳定
  - 缓解措施：继续优先使用稳定的 targeted test、项目构建和相邻 smoke test 组合验证
- 多目标框架 warning 解释风险：同一源位置会在多个 target framework 下重复计数
  - 缓解措施：继续以唯一源位置和 warning 家族为主要决策依据，而不是只看原始 warning 总数
- 并行实现风险：批量收敛时若 subagent 写入边界不清晰，容易引入命名冲突或重复重构
  - 缓解措施：只在 warning 类型或目录边界清晰时并行；每个 subagent 必须有独占文件 ownership，主代理负责合并验证

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
- `RP-006` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`25 Warning(s)`，`0 Error(s)`；`Store.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~StoreTests -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo`
    - 结果：`30 Passed`，`0 Failed`
- `RP-007` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`23 Warning(s)`，`0 Error(s)`；`CoroutineScheduler.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~CoroutineScheduler -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo`
    - 结果：`34 Passed`，`0 Failed`
- `RP-008` 的策略基线：
  - 当前 `GFramework.Core` 剩余 warning 分布：`MA0048=8`、`MA0046=6`、`MA0016=5`、`MA0002=2`、`MA0015=1`、`MA0077=1`
  - 后续批处理规则：优先按类型推进；若当轮主类型数量不足，可顺手吸收其他低冲突类型，不限定于 `MA0015` 与 `MA0077`
- active 跟踪文件只保留当前恢复点、活跃事实、风险与下一步，不再重复保存已完成阶段的长篇历史

## 下一步

1. 若要继续该主题，先读 active tracking，再按需展开历史归档中的 warning 热点与验证记录
2. 下一轮优先以 `MA0048` 为主批次启动；若改动规模和风险允许，可顺手并入其他低冲突类型，而不限定于单独的尾项 warning
3. 若 `MA0048` 的文件 ownership 可以清晰切分，允许使用不同模型的 subagent 并行处理互不冲突的目录或类型簇
4. 若本主题确认暂缓，可保持当前归档状态，不需要再恢复 `local-plan/`
