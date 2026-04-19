# Documentation Governance And Refresh 追踪

## 2026-04-19

### 阶段：local-plan 迁移收口（RP-001）

- 复核当前工作树后确认：worktree 根目录仅剩一个 legacy `local-plan/`，其内容属于文档治理与重写主题的
  durable recovery state，不应继续作为独立根目录入口存在
- 按 `ai-plan` 治理规则建立 `ai-plan/public/documentation-governance-and-refresh/` 主题目录，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将原 `local-plan` 中的详细 tracking / trace 迁入主题内历史归档，并为 active 入口只保留当前恢复点、
  活跃事实、风险与下一步
- 在 `ai-plan/public/README.md` 中建立
  `docs/sdk-update-documentation` -> `documentation-governance-and-refresh` 的 worktree 映射
- 同步更新 `ai-plan-governance` 的 tracking / trace，记录本次迁移已验证当前工作树不再依赖 worktree-root
  `local-plan/`

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/documentation-governance-and-refresh/archive/todos/documentation-governance-and-refresh-history-through-2026-04-18.md`
- 历史 trace 归档：
  - `ai-plan/public/documentation-governance-and-refresh/archive/traces/documentation-governance-and-refresh-history-through-2026-04-18.md`

### 下一步

1. 后续继续该主题时，只从 `ai-plan/public/documentation-governance-and-refresh/` 进入，不再恢复 `local-plan/`
2. 若 active 入口再次积累多轮已完成且已验证阶段，继续按同一模式迁入该主题自己的 `archive/`
