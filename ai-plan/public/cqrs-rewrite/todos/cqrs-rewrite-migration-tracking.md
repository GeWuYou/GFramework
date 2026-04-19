# CQRS 重写迁移跟踪

## 目标

围绕 `GFramework` 当前的双轨 CQRS 现状，继续完成以“去外部依赖、降低反射、收口公开入口”为目标的
CQRS 迁移与收敛。

## 当前恢复点

- 恢复点编号：`CQRS-REWRITE-RP-044`
- 当前阶段：`Phase 8`
- 当前焦点：
  - 当前功能历史已归档，active 跟踪仅保留 `Phase 8` 主线的恢复入口
  - 短期上先处理 `PR #253` 的 latest head review thread 复核，确认当前本地修正是否已在远端收敛
  - 中期上继续 `Phase 8` 主线：参考 `ai-libs/Mediator`，扩大 generator 覆盖、减少 dispatch/invoker 热路径反射，并继续收口 package / facade / 兼容层

## 当前状态摘要

- 已完成 `Mediator` 外部依赖移除、CQRS runtime 重建、默认架构接线和显式程序集 handler 注册入口
- 已完成 `GFramework.Cqrs.Abstractions` / `GFramework.Cqrs` 项目骨架与 runtime seam 收敛
- 已完成 handler registry generator 的多轮收敛，当前合法 closed handler contract 已统一收敛到更窄的注册路径
- 已完成一轮公开入口文档与 source-generator 命名空间收口
- 已接入 `$gframework-pr-review`，可直接抓取当前分支对应 PR 的 CodeRabbit 评论、checks 和测试结果

## 当前活跃事实

- `Phase 8` 仍是当前主线，不再回退到 `Phase 7`
- 最近一轮功能恢复点是 `RP-043`：
  - tracking 顶部阶段与恢复建议已对齐到 `Phase 8`
  - `$gframework-pr-review` 会在 open thread 中显式提醒“`Addressed in commit ...` 文案不等于线程已关闭”
- 若当前分支已推送，应优先重新执行 `$gframework-pr-review`，确认 PR `#253` 的 latest head review threads 是否已收敛
- 若 PR review 噪音已收敛，再回到以下主线优先级：
  - generator 覆盖面继续扩大
  - dispatch/invoker 反射占比继续下降
  - package / facade / 兼容层继续收口

## 当前风险

- 当前 `dotnet build GFramework.sln -c Release` 在 WSL 环境仍会受顶层 `GFramework.csproj` 的 Windows NuGet fallback 配置影响
- 当前 `GFramework.Cqrs.Tests` 仍直接引用 `GFramework.Core`，说明测试已按模块意图拆分，但 runtime 物理迁移尚未完全切断依赖
- `RegisterMediatorBehavior`、`MediatorCoroutineExtensions` 与 `ContextAwareMediator*Extensions` 仍作为兼容层存在，未来真正移除时仍需单独规划弃用窗口

## 活跃文档

- 模块拆分计划：`ai-plan/migration/CQRS_MODULE_SPLIT_PLAN.md`
- 历史跟踪归档：[cqrs-rewrite-history-through-rp043.md](../archive/todos/cqrs-rewrite-history-through-rp043.md)
- 历史 trace 归档：[cqrs-rewrite-history-through-rp043.md](../archive/traces/cqrs-rewrite-history-through-rp043.md)

## 验证说明

- `RP-043` 之前的详细阶段记录、定向验证命令和阶段性决策均已移入主题内归档
- active 跟踪文件只保留当前恢复点、当前活跃事实、风险和下一步，避免 `boot` 在默认入口中重复扫描 1000+ 行历史 trace

## 下一步

1. 推送当前分支后重新执行 `$gframework-pr-review`，确认 `PR #253` 的 latest head review threads 是否已收敛
2. 若 PR review 已收敛，回到 `Phase 8` 主线，优先选择一个收益明确的反射收敛点继续推进
3. 若继续文档主线，优先再扫 `docs/zh-CN/api-reference` 与教程入口页，补齐仍过时的 CQRS API / 命名空间表述
