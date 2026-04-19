# Coroutine Optimization 跟踪

## 目标

继续以“先收敛语义一致性，再补宿主验证和迁移文档”为原则推进当前协程体系，避免 Core 与 Godot 两侧 API 名称、
阶段语义、可观测性和文档入口再次发生漂移。

## 当前恢复点

- 恢复点编号：`COROUTINE-OPTIMIZATION-RP-001`
- 当前阶段：`Phase 1`
- 当前焦点：
  - 已将 worktree-root 遗留的 `local-plan/` 迁入 `ai-plan/public/coroutine-optimization/`，active 入口只保留当前恢复信息
  - 基于早期计划中已经完成的第一轮实现，重新收敛后续切入点，避免把语义命名、宿主集成、测试扩面和文档清理混成一次大任务
  - 明确记录“旧计划没有 durable trace，只有 todo 基线”，后续恢复时先读 active 入口，再按需展开 archive

## 当前状态摘要

- Core 协程第一轮语义收拢已完成，包括真实时间源、执行阶段与阶段型等待的基础行为调整
- 调度器第一版控制与可观测能力已落地，包括完成状态、等待完成、快照查询和完成事件
- Godot 宿主第一版接入已落地，包括分段时间源、节点归属协程入口与退树终止语义
- Core 与 Godot 两侧已经具备一轮基础测试与文档更新，但更贴近运行时的集成验证、兼容性说明和迁移对照仍未收口

## 当前活跃事实

- 本主题的详细历史不是从已有 trace 迁入，而是由旧 `local-plan/todos/coroutine/*.md` 整合出的计划基线
- `RP-001` 的详细工作流拆分、验收标准和缺失 trace 说明已归档到主题内 `archive/`
- 当前工作树分支 `feat/coroutine-optimization` 已在 `ai-plan/public/README.md` 建立 topic 映射

## 当前风险

- 语义兼容性风险：`Delay`、`WaitForSecondsScaled`、`WaitForNextFrame`、`WaitOneFrame` 等命名与行为若继续调整，可能影响既有调用认知
  - 缓解措施：下一轮只先挑一个语义面收敛，并同步补足迁移说明与宿主前提文档
- 宿主验证缺口风险：Godot 节点归属、退树、暂停与各 segment 差异仍缺少更贴近运行时的自动化回归
  - 缓解措施：优先规划 Godot 集成测试宿主，再决定是否扩展更多运行时诊断 API
- 历史信息稀疏风险：旧计划没有同步保留当时的执行 trace 与完整验证记录
  - 缓解措施：active 文档只保留当前结论；需要历史语义时回看 archive，并明确哪些内容是从早期 todo 推导出的基线

## 活跃文档

- 历史跟踪归档：[coroutine-optimization-history-pre-rp001.md](../archive/todos/coroutine-optimization-history-pre-rp001.md)
- 历史 trace 归档：[coroutine-optimization-history-pre-rp001.md](../archive/traces/coroutine-optimization-history-pre-rp001.md)

## 验证说明

- 旧 `local-plan` 的五份 coroutine todo 已整合进主题内历史归档，不再作为 worktree-root durable recovery 入口保留
- active 跟踪文件只保留当前恢复点、活跃事实、风险与下一步，避免把更早期计划直接平移成新的追加式日志

## 下一步

1. 若继续该主题，先在 `Core Semantics`、`Control And Observability`、`Godot Runtime Integration`、`Tests And Regressions`、`Docs And Migration` 中只选一个切入点推进
2. 若优先补验证，先规划 Godot 集成测试宿主与节点归属/退树/暂停场景，再扩运行时诊断 API
3. 若优先补文档与迁移说明，先清理其余 `StartCoroutine()/StopCoroutine()` 残留，再为阶段等待和新入口补统一对照说明
