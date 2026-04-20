# Coroutine Optimization 跟踪

## 目标

继续以“先收敛语义一致性，再补宿主验证和迁移文档”为原则推进当前协程体系，避免 Core 与 Godot 两侧 API 名称、
阶段语义、可观测性和文档入口再次发生漂移。

## 当前恢复点

- 恢复点编号：`COROUTINE-OPTIMIZATION-RP-002`
- 当前阶段：`Phase 4`
- 当前焦点：
  - 已为 `Timing` 补齐纯托管测试宿主入口，允许在 `dotnet test` 下验证 Godot 协程宿主阶段语义，而不依赖原生 `Node` 构造
  - 已补充 `GFramework.Godot.Tests/Coroutine/TimingTests.cs`，锁定暂停、segment 路由和阶段型等待指令的回归覆盖
  - 已根据 PR #259 的最新 CodeRabbit review 收口测试宿主清理对称性，并将 `TimingTests` 固定为非并行执行
  - 下一轮优先补“仍需真实场景树参与”的归属协程 / 退树语义，或转入文档迁移收口，不再回到“Godot 宿主没有自动化回归”的旧状态

## 当前状态摘要

- Core 协程第一轮语义收拢已完成，包括真实时间源、执行阶段与阶段型等待的基础行为调整
- 调度器第一版控制与可观测能力已落地，包括完成状态、等待完成、快照查询和完成事件
- Godot 宿主第一版接入已落地，包括分段时间源、节点归属协程入口与退树终止语义
- Core 与 Godot 两侧已经具备一轮基础测试与文档更新；其中 Godot 侧现已补齐 `Timing` 的 pause / segment / stage-wait 自动化回归
- 更贴近真实场景树的节点归属、退树与 `queue_free` 集成验证，以及迁移对照文档仍未收口

## 当前活跃事实

- 本主题的详细历史不是从已有 trace 迁入，而是由旧 `local-plan/todos/coroutine/*.md` 整合出的计划基线
- `RP-001` 的详细工作流拆分、验收标准和缺失 trace 说明已归档到主题内 `archive/`
- 当前工作树分支 `feat/coroutine-optimization` 已在 `ai-plan/public/README.md` 建立 topic 映射
- `RP-002` 已在 `GFramework.Godot` 内新增仅供测试使用的 `Timing` 纯托管宿主入口，不改公开 API
- `RP-002` 已新增 `TimingTests`，覆盖：
  - 暂停时 `Process` / `ProcessIgnorePause` 的差异
  - `Process` / `PhysicsProcess` / `DeferredProcess` 的推进边界
  - `WaitForFixedUpdate` 与 `WaitForEndOfFrame` 的阶段型等待语义
- 针对 PR #259 的最新未解决 review 线程，已补充两项收口：
  - `TimingTests` 已添加 `[NonParallelizable]`，避免共享静态实例槽位在 NUnit 并行执行时互相污染
  - `Timing` 的测试清理与运行时退树清理现仅在当前实例持有共享 `_instance` 引用时才会清空单例状态
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --no-restore` 当前通过，合计 `58` 个测试

## 当前风险

- 语义兼容性风险：`Delay`、`WaitForSecondsScaled`、`WaitForNextFrame`、`WaitOneFrame` 等命名与行为若继续调整，可能影响既有调用认知
  - 缓解措施：下一轮只先挑一个语义面收敛，并同步补足迁移说明与宿主前提文档
- 宿主验证缺口风险：Godot 节点归属、退树、`queue_free` 与真实场景树回调仍缺少更贴近运行时的自动化回归
  - 缓解措施：下一轮仅补真实场景树相关宿主验证；已完成的 `Timing` 纯托管语义测试不再重复规划
- 历史信息稀疏风险：旧计划没有同步保留当时的执行 trace 与完整验证记录
  - 缓解措施：active 文档只保留当前结论；需要历史语义时回看 archive，并明确哪些内容是从早期 todo 推导出的基线

## 活跃文档

- 历史跟踪归档：[coroutine-optimization-history-pre-rp001.md](../archive/todos/coroutine-optimization-history-pre-rp001.md)
- 历史 trace 归档：[coroutine-optimization-history-pre-rp001.md](../archive/traces/coroutine-optimization-history-pre-rp001.md)

## 验证说明

- 旧 `local-plan` 的五份 coroutine todo 已整合进主题内历史归档，不再作为 worktree-root durable recovery 入口保留
- active 跟踪文件只保留当前恢复点、活跃事实、风险与下一步，避免把更早期计划直接平移成新的追加式日志
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~TimingTests" --no-restore`
  - 结果：通过
  - 备注：新增 `TimingTests` 共 `5` 个测试全部通过
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --no-restore`
  - 结果：通过
  - 备注：Godot 测试项目共 `58` 个测试全部通过
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~TimingTests" --no-restore`
  - 结果：通过
  - 备注：针对 PR #259 review 修复后的 `TimingTests` 共 `5` 个测试全部通过

## 下一步

1. 若继续补验证，优先只做真实场景树相关的节点归属 / 退树 / `queue_free` 回归，不再重新设计 `Timing` 纯托管宿主
2. 当前 PR 合并前可直接回到 GitHub 处理这两条 review 线程的回复与 resolve，避免后续重复审阅同一问题
3. 若转入文档收口，优先清理其余 `StartCoroutine()/StopCoroutine()` 残留，并补 Godot 新入口与阶段等待的迁移对照
