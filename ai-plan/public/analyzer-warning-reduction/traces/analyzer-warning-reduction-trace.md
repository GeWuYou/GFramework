# Analyzer Warning Reduction 追踪

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
