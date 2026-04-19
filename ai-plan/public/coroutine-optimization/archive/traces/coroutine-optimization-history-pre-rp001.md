# Coroutine Optimization 历史追踪（Pre-RP-001）

## 说明

旧 `local-plan/` 只保留了五份 coroutine todo，没有逐日 trace。本文件是基于这些早期计划文档整理出的历史追踪基线，
用于说明当时已经完成的第一轮工作、仍然打开的 follow-up 面，以及哪些结论属于“由 todo 推导出的恢复信息”。

## 2026-04-19

### 阶段：早期 coroutine 计划基线补录（RP-001）

- 复核 `local-plan/todos/coroutine/` 后确认，共存在 `5` 份主题文档，覆盖：
  - Core semantics
  - Core control and observability
  - Godot runtime integration
  - Tests and regressions
  - Docs and migration
- 这些文档都明确指向同一个状态：第一轮 coroutine 语义、宿主接入、基础测试和主路径文档已经完成，后续任务主要是补收口
- 从文档文字可推导出的已完成能力包括：
  - `CoroutineScheduler` 已支持真实时间源
  - `CoroutineExecutionStage` 与阶段型等待已落地
  - 完成状态、等待完成、快照查询和完成事件 API 已落地
  - Godot 的分段时间源、节点归属协程和退树终止语义已落地
  - `docs/zh-CN` 下的 coroutine 主文档与教程已完成一轮纠偏
- 从文档文字可推导出的未收口面包括：
  - 命名与真实行为的一致性仍需继续审视
  - 完成历史清理策略、异常可观测性与更多快照字段仍待评估
  - Godot 复杂场景切换、暂停、`queue_free` 等行为仍需更强验证
  - 更贴近运行时的集成测试与迁移对照文档仍待补齐
- 已显式记录信息缺口：
  - 没有原始执行 trace
  - 没有与每个阶段一一对应的完整验证日志
  - 没有可直接还原当时提交边界的恢复点编号
- 因此，本轮迁移不伪造“已执行的逐步流水”，只把早期计划中可稳定恢复的结论提炼为 archive 基线

### 后续恢复约束

1. 若继续 coroutine 主题，应把本文件当作“早期计划背景”，而不是完整实现日志
2. 新一轮推进时必须重新建立当轮 trace、验证命令和恢复点编号
3. 如果需要更强历史精度，只能再从 Git 历史、测试记录或代码差异中补证，不能把旧 todo 当作完整 trace 使用
