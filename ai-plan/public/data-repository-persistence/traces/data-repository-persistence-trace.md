# Data Repository Persistence 追踪

## 2026-04-19

### 阶段：legacy local-plan 迁移建档（RP-001）

- 复核当前工作树后确认：根目录 `local-plan/` 只剩一份
  `settings-persistence-serialization-tracking.md`，它同时承担 todo 与 trace 角色
- 按 `ai-plan` 治理规则建立 `ai-plan/public/data-repository-persistence/` 主题目录，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将旧混合文件拆分为主题内历史跟踪归档与基于同一材料整理的历史 trace，active 入口只保留当前恢复点、
  活跃事实、风险与下一步
- 在 `ai-plan/public/README.md` 中建立
  `feat/data-repository-persistence` -> `data-repository-persistence` 的 worktree 映射
- 同步更新 `ai-plan-governance` 的 tracking / trace，记录 legacy 单文件计划也已按新目录语义收口

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/data-repository-persistence/archive/todos/data-repository-persistence-history-pre-rp001.md`
- 历史 trace 归档：
  - `ai-plan/public/data-repository-persistence/archive/traces/data-repository-persistence-history-pre-rp001.md`

### 下一步

1. 后续继续该主题时，只从 `ai-plan/public/data-repository-persistence/` 进入，不再恢复 `local-plan/`
2. 若 active 入口再次积累多轮已完成且已验证阶段，继续按同一模式迁入该主题自己的 `archive/`
