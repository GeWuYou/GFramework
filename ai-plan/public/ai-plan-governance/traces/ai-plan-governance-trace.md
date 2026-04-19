# AI-Plan 治理追踪

## 2026-04-19

### 阶段：active 入口归档收口（RP-005）

- 建立 `AI-PLAN-GOV-RP-005` 恢复点
- 复核现有活跃主题后确认：虽然治理规则已提到主题内 `archive/`，但 active `todos/` / `traces/` 仍在持续堆积已完成历史
- 已据此完成本轮收口：
  - 为三个活跃主题补齐并实际使用 `archive/todos/` 与 `archive/traces/`
  - 将 `ai-first-config-system` 与 `cqrs-rewrite` 的已完成阶段详细历史迁入主题内归档
  - 将治理主题自身的 RP-002 至 RP-004 历史迁入归档，只保留当前治理入口
  - 同步更新 `AGENTS.md`、`ai-plan/README.md` 与 `gframework-boot`，明确 active 文档必须保持精简

### Archive Context

- 主题治理历史归档：
  - `ai-plan/public/ai-plan-governance/archive/todos/ai-plan-governance-history-rp002-rp004.md`
  - `ai-plan/public/ai-plan-governance/archive/traces/ai-plan-governance-history-rp002-rp004.md`
- AI-First Config 历史归档：
  - `ai-plan/public/ai-first-config-system/archive/todos/ai-first-config-system-history-through-2026-04-17.md`
  - `ai-plan/public/ai-first-config-system/archive/traces/ai-first-config-system-history-through-2026-04-17.md`
- CQRS 历史归档：
  - `ai-plan/public/cqrs-rewrite/archive/todos/cqrs-rewrite-history-through-rp043.md`
  - `ai-plan/public/cqrs-rewrite/archive/traces/cqrs-rewrite-history-through-rp043.md`

### 下一步 — 归档收口

1. 未来若 active 入口再次因为已完成阶段累积而膨胀，直接按同一模式归档，不再保留为追加式历史日志
2. 后续新增 topic 时，默认同步创建 `todos/`、`traces/` 与 `archive/` 目录

### 阶段：遗留 local-plan 迁移验证（RP-005）

- 复核当前工作树后确认，仍存在未纳入 `ai-plan/` 的遗留恢复目录 `local-plan/`
- 将 `local-plan` 中属于 analyzer warning reduction 主题的 tracking / trace 拆分迁入：
  - `ai-plan/public/analyzer-warning-reduction/todos/`
  - `ai-plan/public/analyzer-warning-reduction/traces/`
  - `ai-plan/public/analyzer-warning-reduction/archive/todos/`
  - `ai-plan/public/analyzer-warning-reduction/archive/traces/`
- 为新 topic 补齐 active 入口与 archive 历史，并更新 `ai-plan/public/README.md` 的 active topics 与 worktree 映射
- 删除旧 `local-plan` 文件，验证治理规则不仅适用于现有 topic，也适用于从 worktree 遗留目录迁入的新 topic
- 额外完成验证：
  - `find ai-plan/public/analyzer-warning-reduction -maxdepth 3 -type f | sort`
  - `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`

### 下一步 — analyzer-warning-reduction 迁移

1. 后续若再发现 `local-plan` 一类目录，直接按 topic 归属迁入 `ai-plan/public/<topic>/`
2. 保持新 topic 的 active 入口精简，避免把迁移动作变成简单目录平移

### 阶段：文档治理 local-plan 迁移验证（RP-005）

- 再次复核当前工作树后确认：遗留的 `local-plan/` 内容属于 documentation governance and refresh 主题
- 按同一治理规则建立 `ai-plan/public/documentation-governance-and-refresh/`，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将旧 `local-plan` 中详细的文档治理 todo / trace 收入主题内归档，只保留精简版 active 入口
- 在 `ai-plan/public/README.md` 中建立
  `docs/sdk-update-documentation` -> `documentation-governance-and-refresh` 的 worktree 映射
- 删除旧 `local-plan` 文件，验证当前工作树已无 legacy 根目录恢复入口
- 额外完成验证：
  - `find ai-plan/public/documentation-governance-and-refresh -maxdepth 3 -type f | sort`
  - `test ! -e local-plan`

### 下一步 — 文档治理迁移

1. 后续若其他 worktree 仍存在 `local-plan` 一类目录，继续按 topic 归属迁入对应 `ai-plan/public/<topic>/`
2. 继续保持 topic active 入口精简，避免把迁移后的公共目录重新写成追加式日志

### 阶段：coroutine early-plan local-plan 迁移验证（RP-005）

- 复核当前工作树后确认：遗留的 `local-plan/` 内容属于 coroutine 主题，但它比前两次迁移更早，只保留了 `5` 份 todo，
  没有任何独立 trace
- 按同一治理规则建立 `ai-plan/public/coroutine-optimization/`，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将旧 `local-plan` 中分散的五个阶段计划整合进主题内历史跟踪归档，并额外补写一份基于 todo 基线整理出的历史 trace，
  显式记录"缺少原始 trace，只能恢复稳定结论"的边界
- 新建精简版 active tracking / trace 入口，只保留当前恢复点、活跃事实、风险与下一步
- 在 `ai-plan/public/README.md` 中建立
  `feat/coroutine-optimization` -> `coroutine-optimization` 的 worktree 映射，并把 `ai-plan-governance` 作为 secondary topic 保留
- 删除旧 `local-plan` 目录，验证当前工作树根目录已不再保留 legacy 私有恢复入口
- 额外完成验证：
  - `find ai-plan/public/coroutine-optimization -maxdepth 3 -type f | sort`
  - `test ! -e local-plan`
  - `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`

### 下一步 — coroutine 早期计划迁移

1. 后续若再遇到"只有 todo、没有 trace"的更早期计划，继续按同一模式迁入 topic archive，并明确标注推导边界
2. 保持新 topic 的 active 入口精简，不把补写 trace 变成伪造逐日执行日志