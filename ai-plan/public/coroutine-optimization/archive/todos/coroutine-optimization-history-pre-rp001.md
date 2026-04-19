# Coroutine Optimization 历史跟踪（Pre-RP-001）

## 背景

本文件整合自旧 `local-plan/todos/coroutine/*.md`。这些材料属于更早期的计划基线，记录了当时已经完成的第一轮实现、
仍待收口的后续任务、风险和验收标准，但没有与之配套的 durable trace。

## 来源文档

- `local-plan/todos/coroutine/01-core-semantics.md`
- `local-plan/todos/coroutine/02-core-control-and-observability.md`
- `local-plan/todos/coroutine/03-godot-runtime-integration.md`
- `local-plan/todos/coroutine/04-tests-and-regressions.md`
- `local-plan/todos/coroutine/05-docs-and-migration.md`

## 历史阶段基线

- 当时的整体判断：协程体系已经完成第一轮实现、基础回归和主路径文档同步，后续任务主要是围绕语义一致性、
  宿主基础设施化、运行时回归覆盖和迁移说明收口
- 该阶段没有留下独立 trace，因此下述内容应视为“早期计划与状态快照整合稿”，而不是逐日执行日志

## Phase 1：Core Semantics

### 已有基础

- `CoroutineScheduler` 已支持 `realtimeTimeSource`
- 已新增 `CoroutineExecutionStage`
- `WaitForSecondsRealtime` 已优先使用真实时间源
- `WaitForFixedUpdate` / `WaitForEndOfFrame` 已只在匹配阶段推进

### 目标

- 继续让 Core API 的名字和真实行为完全一致
- 降低不同宿主对同一等待指令的理解偏差

### 后续必做项

- 评估 `Delay` 与 `WaitForSecondsScaled` 是否需要长期并存
- 评估 `WaitForNextFrame` 与 `WaitOneFrame` 的命名差异是否值得保留
- 为阶段型等待补更多跨宿主说明与样例
- 审视 `WaitForCoroutine` 在父子调度器不同阶段时的语义

### 可选增强

- 为等待指令补统一的语义类别元数据
- 支持宿主自定义阶段映射

### 风险与验收

- 风险：阶段等待在旧调用路径中可能表现为“以前能过、现在会一直等”
- 风险：文档与示例若不同步强调阶段前提，会放大兼容性误解
- 验收标准：等待指令名称不再过度承诺，Core 文档能清楚解释时间与阶段语义

### 当时状态

- 已完成第一轮实现与回归测试
- 下一步原计划：继续审视其他等待指令的命名与边界

## Phase 2：Core Control And Observability

### 已有基础

- 调度器已新增 `CoroutineCompletionStatus`
- 已新增 `WaitForCompletionAsync(...)`
- 已新增 `TryGetCompletionStatus(...)`
- 已新增 `TryGetSnapshot(...)`
- 已新增 `GetActiveSnapshots()`
- 已新增 `OnCoroutineFinished`

### 目标

- 让协程不仅能运行，还能稳定接入业务控制、调试和诊断链路

### 后续必做项

- 评估是否需要完成历史的上限或清理策略
- 为快照补更多可观测字段时保持分配与遍历成本可控
- 审查取消、终止、异常三种完成路径的外部可见语义
- 评估是否需要暴露最后异常查询 API

### 可选增强

- 编辑器或调试面板中的协程列表
- 导出运行中协程报告

### 风险与验收

- 风险：完成历史无限增长会带来内存累积风险
- 风险：同步完成事件必须保持主线程安全假设
- 验收标准：业务代码可以等待、查询并区分协程最终状态；运行时诊断不需要反射或私有字段访问

### 当时状态

- 第一版完成状态与快照 API 已落地
- 下一步原计划：评估历史清理策略和异常可观测性

## Phase 3：Godot Runtime Integration

### 已有基础

- `Timing` 已为各段提供缩放时间源与真实时间源
- `PhysicsProcess` / `DeferredProcess` 已与阶段语义对齐
- 已新增 `RunOwnedCoroutine(...)` 与 `Node.RunCoroutine(...)`
- 节点退树时已终止归属协程
- 已新增节点归属数量和句柄快照查询

### 目标

- 把 Godot 协程从“可运行”提升到“宿主级基础设施”

### 后续必做项

- 验证节点归属协程在复杂场景切换中的行为
- 评估是否需要为 `SceneTree` 或页面级作用域提供批量清理 API
- 评估是否要把 `ProcessIgnorePause` 独立暴露更多调试指标
- 评估节点退出与 `queue_free` 之间的行为是否还需更早终止

### 可选增强

- 编辑器内协程调试面板
- 与 Pause 系统的更细粒度联动

### 风险与验收

- 风险：Godot 线程限制要求所有宿主回调保持主线程驱动
- 风险：节点归属逻辑需要持续验证信号解绑是否完整
- 验收标准：节点归属协程在退树时不再泄漏，`WaitForFixedUpdate` 与 `WaitForEndOfFrame` 在 Godot 中语义真实

### 当时状态

- 第一版宿主接入已落地，并补充了基础时间源测试
- 下一步原计划：增加更贴近运行时的集成测试

## Phase 4：Tests And Regressions

### 已有基础

- 已补充 Core 协程增强测试
- 已补充 Godot 时间源测试项目

### 目标

- 为后续协程能力扩展提供稳定回归网

### 后续必做项

- 增加 Godot 运行时级测试，覆盖节点归属、退树、暂停和各 segment 差异
- 补异常传播、完成历史与快照字段的更多边界测试
- 评估是否需要把 `GFramework.Godot.Tests` 接入解决方案级测试流

### 可选增强

- 文档样例 smoke test
- 基准测试或分配回归测试

### 风险与验收

- 风险：Godot 运行时测试可能需要额外的测试宿主或场景搭建
- 风险：解决方案外测试项目容易被遗漏
- 验收标准：Core 与 Godot 两侧关键协程行为都具备自动化回归；新增阶段和生命周期语义有明确测试覆盖

### 当时状态

- `dotnet test GFramework.Core.Tests -c Release --filter "FullyQualifiedName~Coroutine"` 已通过
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release` 已通过
- 下一步原计划：规划 Godot 集成测试宿主

## Phase 5：Docs And Migration

### 已有基础

- 已更新 `docs/zh-CN/core/coroutine.md`
- 已更新 `docs/zh-CN/godot/coroutine.md`
- 已更新 `docs/zh-CN/tutorials/coroutine-tutorial.md`

### 已修正重点

- 时间语义与阶段语义说明
- 节点归属协程入口
- `StartCoroutine()/StopCoroutine()` 旧示例误导

### 目标

- 让用户看到的主文档与当前实现保持一致

### 后续必做项

- 继续扫描仓库中其他 `StartCoroutine()/StopCoroutine()` 文档残留
- 为迁移场景补“旧示例到新入口”的对照说明
- 为 `WaitForFixedUpdate` / `WaitForEndOfFrame` 的宿主前提补更多页面说明

### 可选增强

- 增加 FAQ 和排障章节
- 增加 Godot 场景级最佳实践示例

### 风险与验收

- 风险：老文档残留会继续制造错误接入路径
- 验收标准：用户按主文档示例能直接跑通当前 API，旧接口误导在主路径文档中被清理

### 当时状态

- 主文档与教程已对齐当前实现
- 下一步原计划：继续清理其他文档残留并补迁移说明

## 历史结论

- 旧 `local-plan` 记录的不是“待从零开始”的需求池，而是“第一轮实现已完成后仍待收口的 follow-up backlog”
- 由于当时没有同步留下 trace，后续恢复时不应把本文件视作完整执行历史；真正要继续推进时，应基于 active tracking
  重新选择一个最小切入点，并补回当轮 trace 与验证记录
