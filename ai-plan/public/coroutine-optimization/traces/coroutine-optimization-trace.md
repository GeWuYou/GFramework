# Coroutine Optimization 追踪

## 2026-04-19

### 阶段：legacy local-plan 迁移建档（RP-001）

- 复核当前工作树后确认：`local-plan/` 仅保存 coroutine 主题的早期 todo 基线，共 `5` 份分阶段文档，没有独立 trace
- 按 `ai-plan` 治理规则建立 `ai-plan/public/coroutine-optimization/` 主题目录，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将旧 `local-plan` 中分散的五个阶段计划整合为主题内历史跟踪归档，避免后续恢复仍依赖 worktree-root 私有目录
- 因旧计划没有 durable trace，本次额外补写一份“基于早期 todo 基线整理出的历史 trace”，显式记录信息缺口与推导边界
- 新建精简版 active tracking / trace 入口，并在 `ai-plan/public/README.md` 中建立
  `feat/coroutine-optimization` -> `coroutine-optimization` 的 worktree 映射
- 删除旧 `local-plan` 目录，确保后续 `boot` 只从 `ai-plan/public/coroutine-optimization/` 进入
- 额外完成验证：
  - `find ai-plan/public/coroutine-optimization -maxdepth 3 -type f | sort`
  - `test ! -e local-plan`
  - `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/coroutine-optimization/archive/todos/coroutine-optimization-history-pre-rp001.md`
- 历史 trace 归档：
  - `ai-plan/public/coroutine-optimization/archive/traces/coroutine-optimization-history-pre-rp001.md`

### 下一步

1. 后续若继续 coroutine 主题，只从 `ai-plan/public/coroutine-optimization/` 进入，不再恢复 `local-plan/`
2. 下一轮只选择一个主切入点推进，避免语义、宿主、测试和文档扩面同时发生
3. 若 active 入口后续积累多轮已完成且已验证阶段，再按同一模式迁入该主题自己的 `archive/`

## 2026-04-20

### 阶段：Godot 宿主回归覆盖补齐（RP-002）

- 选择只推进 `Tests And Regressions` 切面，不同时改动协程语义与迁移文档
- 新增 `GFramework.Godot/Coroutine/Timing.Testing.cs`，为 `Timing` 提供仅供测试使用的纯托管初始化与帧推进入口
- 新增 `GFramework.Godot.Tests/Coroutine/TimingTests.cs`，覆盖：
  - 暂停时 `Process` 与 `ProcessIgnorePause` 的推进差异
  - `Process` / `PhysicsProcess` / `DeferredProcess` 的执行边界与顺序
  - `WaitForFixedUpdate` 与 `WaitForEndOfFrame` 的阶段型等待语义
- 关键决策：在 `dotnet test` 宿主中不直接运行 `Timing : Node` 的原生构造，而是使用未初始化对象配合测试入口补齐纯托管字段
  - 原因：直接构造 `Godot.Node` 派生类型会导致 VSTest test host 崩溃，无法作为稳定回归路径
  - 约束：当前测试覆盖的是宿主调度语义，不覆盖真实场景树信号、节点归属与退树回调
- 为支持上述测试入口，将 `Timing` 的节点归属字典从只读字段调整为可在测试初始化阶段重建的私有字段，未改动任何公共 API
- 完成验证：
  - `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~TimingTests" --no-restore`
  - `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --no-restore`
- 同日根据 PR #259 的最新 unresolved CodeRabbit review 继续收口：
  - 在 `GFramework.Godot.Tests/Coroutine/TimingTests.cs` 为 fixture 添加 `[NonParallelizable]`，避免静态实例槽位在 NUnit 并行执行时互相污染
  - 将 `Timing` 的 `_instance` 清理改为“仅当当前实例仍持有共享单例引用时才执行”，同时覆盖运行时 `_ExitTree()` 与测试入口 `DisposeForTests()`
- 额外完成验证：
  - `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~TimingTests" --no-restore`

### 下一步

1. 若继续补验证，优先规划真实场景树参与的节点归属 / 退树 / `queue_free` 测试宿主
2. 若转入文档收口，优先清理仍引用 `StartCoroutine()/StopCoroutine()` 的教程残留，并补迁移对照
