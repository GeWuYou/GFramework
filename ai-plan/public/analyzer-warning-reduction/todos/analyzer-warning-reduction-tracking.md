# Analyzer Warning Reduction 跟踪

## 目标

继续以“优先低风险、保持行为兼容”为原则收敛当前仓库的 Meziantou analyzer warnings，并在首轮大规模清理完成后，
判断剩余结构性 warning 是否值得在下一轮继续推进。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-021`
- 当前阶段：`Phase 21`
- 当前焦点：
  - 已完成 `GFramework.Core` 当前 `MA0016` / `MA0002` / `MA0015` / `MA0077` 低风险收口批次
  - 已复核 `net10.0` 下的 `MA0158` 基线：`GFramework.Core` / `GFramework.Cqrs` 当前共有 `16` 个 object lock
    建议点，属于跨 target 兼容性风险，不在本轮直接批量替换
  - 已完成 `GFramework.Core.SourceGenerators/Rule/ContextAwareGenerator.cs` 的剩余 `MA0051` 结构拆分，生成输出保持不变
  - 已完成 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 的 `MA0051` 结构拆分，生成输出保持不变
  - 已完成 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的 `MA0006` 低风险收口，schema 关键字比较显式使用
    `StringComparison.Ordinal`
  - 已完成 `SchemaConfigGenerator.cs` 的第一批 `MA0051` 结构拆分：schema 入口解析、属性解析、schema 遍历、数组属性解析、
    约束文档生成与若干生成代码发射 helper 已拆出语义阶段
  - 已完成当前 PR #269 review follow-up：`CqrsHandlerRegistryGenerator` 按职责拆分为 partial 生成器文件，
    `ContextAwareGenerator` 改用稳定前缀字段并补上 provider null 防御，`Option<T>` 补齐 `<remarks>` 契约说明
  - `LoggingConfiguration`、`FilterConfiguration` 与 `CollectionExtensions` 已改用集合抽象接口，并保留内部具体集合默认值
  - `CoroutineScheduler` 的 tag/group 字典已显式使用 `StringComparer.Ordinal`，保持既有区分大小写语义
  - `EasyEvents.AddEvent<T>()` 的重复注册路径已改为状态冲突异常，避免把泛型类型参数伪装成方法参数名
  - `Option<T>` 已声明 `IEquatable<Option<T>>`，与已有强类型 `Equals(Option<T>)` 契约对齐
  - 当前 `GFramework.Core` `net8.0` warnings-only 基线已降到 `0` 条
  - 当前 `GFramework.Core.SourceGenerators` warnings-only 基线已降到 `0` 条
  - 当前 `GFramework.Cqrs.SourceGenerators` warnings-only 基线已降到 `0` 条
  - 当前 `GFramework.Game.SourceGenerators` warnings-only 基线已从 `46` 条降到 `9` 条，剩余均为
    `SchemaConfigGenerator.cs` 的 `MA0051`
  - `GFramework.Godot` 的 `Timing.cs` 已同步适配新事件签名，但当前 worktree 的 Godot restore 资产仍受 Windows fallback package folder 干扰，独立 build 需在修复资产后补跑
  - 后续继续按 warning 类型和数量批处理，而不是回退到按单文件切片推进
  - 下一轮默认继续拆分 `GFramework.Game.SourceGenerators` 的 `MA0051` 热点，或评估跨 target 的 `MA0158`
    锁替换风险
  - 单次 `boot` 的工作树改动上限控制在约 `100` 个文件以内，避免 recovery context 与 review 面同时失控
  - 若任务边界互不冲突，允许使用不同模型的 subagent 并行处理不同 warning 类型或不同目录，但必须遵守显式 ownership

## 当前状态摘要

- 已完成 `GFramework.Core`、`GFramework.Cqrs`、`GFramework.Godot` 与部分 source generator 的低风险 warning 清理
- 已完成多轮 CodeRabbit follow-up 修复，并用定向测试与项目/解决方案构建验证了关键回归风险
- 已完成当前 PR #265 review follow-up：修复 `CoroutineScheduler` 的零容量扩容边界，并补上 `Store` dispatch 作用域的异常安全回滚
- 已继续完成当前 PR #265 review follow-up：修复 `Event<T>` 与 `Event<T, TK>` 监听器计数的 off-by-one，并补充回归测试
- 已增强 `gframework-pr-review` 脚本与 skill 文档，降低超长 JSON 直出导致的 review 信号漏看风险
- 已完成 `GFramework.Core` 当前 `MA0046` 批次：将阶段、协程与异步日志事件统一迁移到 `EventHandler<TEventArgs>` 形状，
  并同步更新 `GFramework.Godot` 订阅点、定向测试与 `docs/zh-CN` 示例
- 已完成当前 PR #267 review follow-up：修复 `AsyncLogAppender` 的 `ILogAppender.Flush()` 双重完成通知，并补齐
  `PhaseChanged` / `CoroutineExceptionEventArgs` XML 文档、`PhaseChanged` 迁移说明和 `ai-plan` 基线注释
- 已完成当前 PR #267 failed-test follow-up：修复 `AsyncLogAppender.Flush()` 在队列已被后台线程提前清空时仍可能
  等待满默认超时并返回 `false` 的竞态，并通过整包 `GFramework.Core.Tests` 重新验证
- 已完成当前 `GFramework.Core` `net8.0` 剩余低风险 analyzer warning 批次；warnings-only 基线已降到 `0` 条
- 已完成 `GFramework.Core.SourceGenerators` 中 `ContextAwareGenerator` 的剩余 `MA0051` 收口；warnings-only 基线已降到 `0` 条
- 已完成 `GFramework.Cqrs.SourceGenerators` 中 `CqrsHandlerRegistryGenerator` 的剩余 `MA0051` 收口；warnings-only 基线已降到 `0` 条
- 已完成当前 PR #269 的 review follow-up：收口 `ContextAwareGenerator` 的字段命名冲突 / provider null 契约、
  `Option<T>` 的 XML 文档缺口，以及 `CqrsHandlerRegistryGenerator` 的超大文件拆分
- 已完成 `GFramework.Game.SourceGenerators` 中 `SchemaConfigGenerator` 的第一批 `MA0051` 收口；warnings-only 基线剩余 `9` 条
  `MA0051`

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
- `RP-009` 在不改公共 API 的前提下，将同名泛型家族收拢到与类型名一致的单文件中，清空当前 `GFramework.Core`
  `net8.0` 基线中的 `MA0048`，并通过定向 build/test 验证 `Command`、`Query`、`Event` 路径未回归
- `RP-010` 使用 `gframework-pr-review` 复核当前分支 PR #265 后，修复了仍在本地成立的两个 follow-up 风险：
  `CoroutineScheduler` 的 `initialCapacity: 0` 扩容越界，以及 `Store` 在 dispatch 快照阶段抛异常时可能残留
  `_isDispatching = true` 的锁死问题
- `RP-011` 根据补充复核继续收口 PR #265 的 outside-diff comment，修复 `Event<T>` / `Event<T, TK>` 默认 no-op
  委托导致的 `GetListenerCount()` off-by-one，并以定向事件测试验证注册、注销和计数语义
- `RP-012` 为 `gframework-pr-review` 增加 `--json-output`、`--section`、`--path` 与文本截断能力，并更新 skill 推荐用法，
  让“先落盘、再定向抽取”成为默认可操作路径
- `RP-013` 已完成 `GFramework.Core` 当前 `MA0046` 批次，并以新的事件参数类型替换阶段、协程和异步日志事件的
  非标准签名；`GFramework.Core` `net8.0` warnings-only 基线由 `15` 降至 `9`
- `RP-014` 使用 `gframework-pr-review` 复核当前分支 PR #267 的 latest head review threads、outside-diff comment 与
  nitpick comment 后，确认 8 条高信号项中仍成立的是 1 个行为 bug 与 7 个文档/测试/跟踪缺口，并按最小改动收口
- `RP-015` 使用 `$gframework-pr-review` 复核 PR #267 的 CTRF 失败测试评论后，确认 `AsyncLogAppender` 仍存在
  “队列已空但 Flush 仍超时失败”的竞态；该问题在本地整包 `GFramework.Core.Tests` 中可复现，现已修复并补上稳定回归测试
- `RP-016` 将 `GFramework.Core` 当前剩余 `MA0016` / `MA0002` / `MA0015` / `MA0077` 低风险批次清零，并用
  warnings-only build 与 focused tests 验证配置反序列化、集合扩展、事件重复注册、`Option<T>` 相等性和协程 tag/group 语义
- `RP-017` 复核 `MA0158` 当前仍是跨 target 锁类型迁移问题，因此先收口单点 `ContextAwareGenerator` `MA0051`，
  并通过 source generator 项目 build 与 `ContextAwareGeneratorSnapshotTests` 验证生成输出未回归
- `RP-018` 暂缓跨 target `MA0158`，转入 `GFramework.Cqrs.SourceGenerators` 的单文件结构性 warning；
  通过拆分 handler 分析、运行时类型引用构造、注册器源码发射与精确反射注册输出阶段，清空该项目当前 `MA0051`
- `RP-019` 转入 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs`，先完成低风险 `MA0006` 批次；
  通过 schema 类型比较 helper 与显式 `StringComparison.Ordinal` 清空当前项目的 `MA0006`
- `RP-020` 继续拆分 `SchemaConfigGenerator.cs` 的 `MA0051` 热点，将当前项目 warnings-only 基线从 `19` 条降到 `9` 条，
  并用 focused schema generator tests 验证 50 个用例通过
- `RP-021` 使用 `$gframework-pr-review` 复核当前分支 PR #269 后，修复仍在本地成立的 4 个项：将
  `CqrsHandlerRegistryGenerator` 拆分为职责清晰的 partial 文件、为 `ContextAwareGenerator` 生成字段增加稳定前缀并补上
  `SetContextProvider` 的运行时 null 校验、为 `Option<T>` 补齐 `<remarks>`，并新增字段重名场景的生成器快照测试
- 当前工作树分支 `fix/analyzer-warning-reduction-batch` 已在 `ai-plan/public/README.md` 建立 topic 映射

## 当前风险

- 公共契约兼容风险：本轮将部分配置与扩展方法返回值从具体集合类型改为集合抽象接口
  - 缓解措施：保留具体集合默认值，并通过配置反序列化、工厂创建与集合扩展定向测试覆盖主要消费路径
- 测试宿主稳定性风险：部分 Godot 失败路径在当前 .NET 测试宿主下仍不稳定
  - 缓解措施：继续优先使用稳定的 targeted test、项目构建和相邻 smoke test 组合验证
- 多目标框架 warning 解释风险：同一源位置会在多个 target framework 下重复计数
  - 缓解措施：继续以唯一源位置和 warning 家族为主要决策依据，而不是只看原始 warning 总数
- net10 专属 warning 风险：`MA0158` 建议使用 `System.Threading.Lock`，但项目多 target 时需要确认兼容边界
  - 缓解措施：下一轮先按 target framework 与 API 可用性评估，不直接批量替换共享源码中的 `object` lock
- source generator warning 外溢风险：运行 `GFramework.SourceGenerators.Tests` 会构建相邻 generator/test 项目并显示既有
  `GFramework.Game.SourceGenerators` 与测试项目 warning
  - 缓解措施：继续以被修改 generator 项目的独立 warnings-only build 作为主验收，并用 focused generator test 验证行为
- Godot 资产文件环境风险：当前 worktree 的 `GFramework.Godot` restore/build 仍会命中 Windows fallback package folder
  - 缓解措施：后续若继续触达 Godot 模块，先用 Linux 侧 restore 资产或 Windows-hosted 构建链刷新该项目，再补跑定向 build
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
- `RP-009` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`15 Warning(s)`，`0 Error(s)`；当前 `GFramework.Core` `net8.0` warnings-only 输出中已不再出现 `MA0048`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CommandExecutorTests|FullyQualifiedName~AbstractAsyncCommandTests|FullyQualifiedName~QueryExecutorTests|FullyQualifiedName~AbstractAsyncQueryTests|FullyQualifiedName~EventTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`83 Passed`，`0 Failed`
- `RP-010` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`15 Warning(s)`，`0 Error(s)`；新增修复未引入新的 `GFramework.Core` `net8.0` 构建错误
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CoroutineSchedulerTests.Run_Should_Grow_From_Zero_Initial_Capacity|FullyQualifiedName~StoreTests.Dispatch_Should_Reset_Dispatching_Flag_When_Snapshot_Creation_Throws" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`2 Passed`，`0 Failed`
- `RP-011` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`15 Warning(s)`，`0 Error(s)`；`Event.cs` 的 listener count 修复未引入新的 `GFramework.Core` `net8.0` 构建错误
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~EventTests.EventT_GetListenerCount_Should_Exclude_Placeholder_Handler|FullyQualifiedName~EventTests.EventTTK_GetListenerCount_Should_Exclude_Placeholder_Handler" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`2 Passed`，`0 Failed`
- `RP-012` 的定向验证结果：
  - `python3 -m py_compile .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py`
    - 结果：通过；使用 `PYTHONPYCACHEPREFIX=/tmp/codex-pycache` 规避技能目录只读导致的 `__pycache__` 写入限制
  - `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --help`
    - 结果：通过；`--json-output`、`--section`、`--path`、`--max-description-length` 已出现在 CLI 帮助中
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`0 Warning(s)`，`0 Error(s)`
- `RP-013` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`9 Warning(s)`，`0 Error(s)`；相对 `RP-009` / `RP-011` 的 warnings-only 基线 `15 Warning(s)` 已降到 `9 Warning(s)`，
      当前 `GFramework.Core` `net8.0` 输出中已不再出现 `MA0046`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureLifecycleBehaviorTests|FullyQualifiedName~CoroutineSchedulerTests|FullyQualifiedName~AsyncLogAppenderTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`50 Passed`，`0 Failed`
  - `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -nologo`
    - 结果：失败；当前 worktree 的 Godot restore 资产仍引用 Windows fallback package folder，尚未完成独立项目编译验证
- `RP-014` 的定向验证结果：
  - `dotnet restore GFramework.Core.Tests/GFramework.Core.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；host Windows `dotnet` 首次验证前补齐了缺失的 `Meziantou.Analyzer 3.0.48` 包
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`9 Warning(s)`，`0 Error(s)`；`AsyncLogAppender` 行为修复与 XML / 文档补充未引入新的 `GFramework.Core` `net8.0` 构建错误
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~CoroutineSchedulerTests.Scheduler_Should_Raise_OnCoroutineException_With_EventArgs|FullyQualifiedName~AsyncLogAppenderTests.Flush_Should_Raise_OnFlushCompleted_With_Sender_And_Result|FullyQualifiedName~AsyncLogAppenderTests.ILogAppender_Flush_Should_Raise_OnFlushCompleted_Only_Once|FullyQualifiedName~ArchitectureLifecycleBehaviorTests.InitializeAsync_Should_Raise_PhaseChanged_With_Sender_And_EventArgs" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`4 Passed`，`0 Failed`
- `RP-015` 的验证结果：
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --disable-build-servers --filter "FullyQualifiedName~AsyncLogAppenderTests"`
    - 结果：`15 Passed`，`0 Failed`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --disable-build-servers`
    - 结果：`1607 Passed`，`0 Failed`
- `RP-016` 的验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；当前 `GFramework.Core` `net8.0` analyzer baseline 已清零
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~LoggingConfigurationTests|FullyQualifiedName~ConfigurableLoggerFactoryTests|FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~EasyEventsTests|FullyQualifiedName~OptionTests|FullyQualifiedName~CoroutineGroupTests|FullyQualifiedName~CoroutineSchedulerTests" -m:1 -nologo`
    - 结果：`112 Passed`，`0 Failed`；测试构建仍会显示既有 `net10.0` `MA0158` 与 source generator `MA0051` warning
- `RP-017` 的验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net10.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`16 Warning(s)`，`0 Error(s)`；当前 `MA0158` 跨 `GFramework.Core` / `GFramework.Cqrs`，本轮只记录基线不批量改锁
  - `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；`ContextAwareGenerator.cs` 已不再出现 `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~ContextAwareGeneratorSnapshotTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`1 Passed`，`0 Failed`；测试构建仍显示相邻 source generator 和测试项目的既有 analyzer warning
- `RP-018` 的验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；`CqrsHandlerRegistryGenerator.cs` 当前 `MA0051` 已清零
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CqrsHandlerRegistryGeneratorTests -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`14 Passed`，`0 Failed`
    - 说明：该 test project 构建仍显示 `GFramework.Game.SourceGenerators` 与测试项目中的既有 analyzer warning；本轮关注的
      `GFramework.Cqrs.SourceGenerators` 独立 build 已清零
- `RP-019` 的验证结果：
  - `dotnet restore GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -p:RestoreFallbackFolders= -nologo`
    - 结果：通过；刷新 Linux 侧资产以清除 stale Windows fallback package folder
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`19 Warning(s)`，`0 Error(s)`；当前项目输出已不再出现 `MA0006`，剩余均为 `SchemaConfigGenerator.cs` 的
      `MA0051`
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders= -nologo`
    - 结果：通过；刷新 test project 资产以清除 stale Windows fallback package folder
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SchemaConfigGenerator -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`50 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示既有 source generator test analyzer warning；不属于本轮写集
- `RP-020` 的验证结果：
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`9 Warning(s)`，`0 Error(s)`；当前项目剩余 warning 均为 `SchemaConfigGenerator.cs` 的 `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SchemaConfigGenerator -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`50 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示既有 source generator test analyzer warning；不属于本轮写集
- `RP-021` 的验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；拆分后最大单文件已降到 `851` 行，满足仓库 800-1000 行上限
  - `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；`ContextAwareGenerator` 的字段命名与 provider 契约修复未引入新的 generator warning
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~ContextAwareGeneratorSnapshotTests|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests" -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：先并行运行两条 `dotnet test` 时触发共享输出文件锁冲突；改为串行重跑后 `ContextAwareGeneratorSnapshotTests=2 Passed`、
      `CqrsHandlerRegistryGeneratorTests=14 Passed`
    - 说明：失败来自测试宿主并行写入同一 build 输出，不是代码回归；串行重跑后快照新增的字段重名场景和 CQRS 快照均通过
- active 跟踪文件只保留当前恢复点、活跃事实、风险与下一步，不再重复保存已完成阶段的长篇历史

## 下一步

1. 若要继续该主题，先读 active tracking，再按需展开历史归档中的 warning 热点与验证记录
2. 下一轮优先继续拆分 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的剩余 `MA0051`；建议先从
   `GenerateBindingsClass`、`AppendGeneratedConfigCatalogType` 或对象/条件 schema target 验证方法切入
3. 若改回推进 `MA0158`，先设计 `net8.0` / `net9.0` / `net10.0` 多 target 条件编译方案，不直接批量替换共享源码中的
   `object` lock
4. 若后续继续改动 `GFramework.Godot`，先修复该项目的 Linux 侧 restore 资产，再补跑独立 build
5. 若本主题确认暂缓，可保持当前归档状态，不需要再恢复 `local-plan/`
