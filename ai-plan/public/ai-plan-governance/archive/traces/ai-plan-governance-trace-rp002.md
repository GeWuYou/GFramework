# AI-Plan Governance 追踪

## 2026-05-09

### 阶段：多 Agent 协作治理入口落地（AI-PLAN-GOVERNANCE-RP-002）

- 确认本轮目标采用混合方案，而不是只写 skill 或只写 `AGENTS.md`：
  - `AGENTS.md` 承载仓库级强约束
  - `.agents/skills/gframework-multi-agent-batch/` 承载主 Agent 持续派发 / review / `ai-plan` / validation 的执行流程
- 修复 repository boot 入口的一处一致性问题：
  - `AGENTS.md` 先前仍引用 `.codex/skills/gframework-boot/`
  - 仓库实际技能目录是 `.agents/skills/`
  - 本轮已对齐为 `.agents/skills/gframework-boot/`，并补入新的多 Agent skill 路径
- 本轮已落地的治理变更：
  - 在 `AGENTS.md` 增加 `Multi-Agent Coordination Rules`
  - 在 `.agents/skills/README.md` 新增 `gframework-multi-agent-batch` 公开入口说明
  - 在 `.agents/skills/gframework-boot/SKILL.md` 增加 orchestration-heavy 场景切换说明
  - 在 `.agents/skills/gframework-batch-boot/SKILL.md` 增加与新 skill 的边界说明
  - 新建 `.agents/skills/gframework-multi-agent-batch/`
  - 在 `ai-plan/public/README.md` 补入 `ai-plan-governance` active topic
  - 新建 `ai-plan/public/ai-plan-governance/` 的 tracking / trace 入口
- 关键约束决策：
  - 主 Agent 负责 critical path、stop condition、`ai-plan`、validation、review、final integration
  - worker 只能拿到显式且互不冲突的 ownership slice
  - 继续派发下一波前，主 Agent 必须重算 reviewability、branch diff 与 context budget
- 当前收尾目标：
  - 运行校验
  - 把验证结果回填到 active tracking / trace
  - 以 Conventional Commit 提交本轮治理变更

### 验证里程碑

- `python3 scripts/license-header.py --check --paths AGENTS.md ai-plan/public/README.md ai-plan/public/ai-plan-governance/todos/ai-plan-governance-tracking.md ai-plan/public/ai-plan-governance/traces/ai-plan-governance-trace.md`
  - 结果：通过
- `git diff --check`
  - 结果：通过
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release`
  - 结果：通过（`0 warning / 0 error`）

### 下一步

1. 提交本轮多 Agent 协作治理变更
2. 未来若有真实的 orchestration-heavy 任务进入该 worktree，优先直接用新 skill 驱动并据实补充下一恢复点
