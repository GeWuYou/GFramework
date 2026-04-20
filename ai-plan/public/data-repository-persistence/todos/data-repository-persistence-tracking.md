# Data Repository Persistence 跟踪

## 目标

继续收敛 `GFramework.Game` 当前的数据仓库持久化、设置模型与序列化语义，确保第一轮高优先级修复、测试与文档
同步之后，剩余设计性 follow-up 仍有清晰、可共享的恢复入口。

## 当前恢复点

- 恢复点编号：`DATA-REPOSITORY-PERSISTENCE-RP-001`
- 当前阶段：`Phase 1`
- 当前焦点：
  - 已将根目录 legacy `local-plan/settings-persistence-serialization-tracking.md` 迁入
    `ai-plan/public/data-repository-persistence/`
  - 第一轮 settings / persistence / serialization 修复、测试与文档同步已完成，并收入主题内 `archive/`
  - 当前正在补齐 `JsonSerializer` 的配置生命周期、只读约束与线程安全说明
  - 下一轮需要继续评估迁移模型统一抽象与 codec / persistence pipeline 边界

## 当前状态摘要

- 高优先级实现、测试与文档对齐已在本主题历史阶段完成，当前 active 入口主要保留后续 design/backlog 恢复点
- 当前分支 `feat/data-repository-persistence` 已在 `ai-plan/public/README.md` 建立 topic 映射
- 旧单文件不再同时承担 todo 与 trace 角色，后续恢复统一从本 topic 的 active tracking / trace 进入

## 当前活跃事实

- 原 `local-plan` 只有一份混合 tracking 文件，没有独立的 `todos/` 与 `traces/`
- 详细历史已拆分迁入主题内 `archive/`，active tracking / trace 只保留当前恢复点、风险与下一步
- 历史已验证结果包括 `GFramework.Game.Tests` 的定向与全量通过，以及 `docs/zh-CN/game/*` 的同步更新
- `GFramework.Game.Serializer.JsonSerializer` 当前直接暴露活动中的 `JsonSerializerSettings` 与 converters 集合，配置不会被复制
- `docs/zh-CN/game/serialization.md` 现有“序列化器本身线程安全”表述与源码契约不一致，需要在本轮修正

## 当前风险

- 迁移模型分叉风险：`SettingsModel`、`DataRepository` 与 `SaveRepository<T>` 的版本演进机制仍可能继续分叉
  - 缓解措施：在新增更多 persistence feature 前，先评估能否抽出统一的 migration abstraction
- Active 入口回膨胀风险：若后续把实现细节继续堆回 active 文档，会重新退化成旧 `local-plan`
  - 缓解措施：后续阶段完成并验证后，继续迁入本 topic 的 `archive/`

## 活跃文档

- 历史跟踪归档：[data-repository-persistence-history-pre-rp001.md](../archive/todos/data-repository-persistence-history-pre-rp001.md)
- 历史 trace 归档：[data-repository-persistence-history-pre-rp001.md](../archive/traces/data-repository-persistence-history-pre-rp001.md)

## 验证说明

- 旧混合 `local-plan` 已拆分迁入主题内 archive
- active 跟踪文件已按 `ai-plan` 治理规则精简为当前恢复入口
- 已补充 `JsonSerializer` XML docs、文档示例与最小契约测试
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~JsonSerializerTests"` 已通过（9/9）
- 本次定向验证过程中出现的 analyzer warning 来自仓库既有代码，不属于本轮新增问题

## 下一步

1. 再评估设置 / 通用仓库 / 存档仓库的迁移模型是否要统一抽象
2. 最后评估压缩 / 加密 / 元数据策略是否应落入更明确的 codec / persistence pipeline
3. 若进入下一轮实现，先确定是否需要新的 dedicated recovery point 以避免 RP-001 active 入口继续膨胀
