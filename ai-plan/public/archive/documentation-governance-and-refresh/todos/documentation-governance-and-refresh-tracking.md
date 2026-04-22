# Documentation Governance And Refresh 跟踪

## 目标

继续以“文档必须可追溯到源码、测试与真实接入方式”为原则，维护 `GFramework` 的仓库入口、模块入口与
`docs/zh-CN` 采用链路，避免 README、专题页与教程再次偏离当前实现。

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-GOVERNANCE-REFRESH-RP-019`
- 当前阶段：`Completed`
- 当前焦点：
  - 用户已确认 PR #268 合并，本 topic 对应的文档治理收口工作完成
  - 当前目录将在本轮迁入 `ai-plan/public/archive/documentation-governance-and-refresh/`
  - 后续若需历史回溯，应从 archive 中恢复，而不是继续把该 topic 作为 active 默认入口

## 当前状态摘要

- `docs/zh-CN/godot/` 当前高优先级页面集与 `docs/zh-CN/tutorials/godot-integration.md` 已完成源码优先收口
- PR #268 已合并，上一轮保留 active 的唯一原因已经解除
- 本 topic 已达到归档条件：实现完成、校验完成、PR 生命周期结束

## 当前活跃事实

- 当前 worktree 下未发现 `ai-plan/private/` 恢复目录，本主题一直以 public artifacts 作为唯一恢复入口
- 已存在的阶段归档：
  - `ai-plan/public/documentation-governance-and-refresh/archive/todos/documentation-governance-and-refresh-history-through-2026-04-22.md`
  - `ai-plan/public/documentation-governance-and-refresh/archive/traces/documentation-governance-and-refresh-history-through-2026-04-22.md`
- 2026-04-22 之前的长篇历史已存在于 2026-04-18 与 RP-001 through RP-008 的归档文件中
- 当前待处理事项已清零；后续只保留历史查询价值

## 当前风险

- 后续历史定位风险：如果不把 topic 从 active 列表中移除，`boot` 会继续把已经完成的文档治理主题当作默认入口
  - 缓解措施：本轮同步更新 `ai-plan/public/README.md` 并把整个 topic 目录迁入 `ai-plan/public/archive/`
- 文档回漂风险：未来若有新的 README / `docs/zh-CN` 变更，仍可能重新引入与源码不一致的表述
  - 缓解措施：新任务应创建或复用新的 active topic，而不是重启当前已完成主题

## 活跃文档

- 当前 trace：[documentation-governance-and-refresh-trace.md](../traces/documentation-governance-and-refresh-trace.md)
- 2026-04-22 跟踪归档：[documentation-governance-and-refresh-history-through-2026-04-22.md](../archive/todos/documentation-governance-and-refresh-history-through-2026-04-22.md)
- 2026-04-22 trace 归档：[documentation-governance-and-refresh-history-through-2026-04-22.md](../archive/traces/documentation-governance-and-refresh-history-through-2026-04-22.md)
- 2026-04-18 历史归档：[documentation-governance-and-refresh-history-through-2026-04-18.md](../archive/todos/documentation-governance-and-refresh-history-through-2026-04-18.md)
- RP-001 到 RP-008 trace 归档：[documentation-governance-and-refresh-rp-001-through-rp-008.md](../archive/traces/documentation-governance-and-refresh-rp-001-through-rp-008.md)

## 验证说明

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/architecture.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/scene.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/extensions.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/logging.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/godot-integration.md`
- `cd docs && bun run build`

## 下一步

1. 将整个 `documentation-governance-and-refresh` 目录迁入 `ai-plan/public/archive/`
2. 从 `ai-plan/public/README.md` 删除该 topic 的 active 声明与 worktree 映射
