# AI-Plan Governance 跟踪

## 目标

保持仓库级 AI 工作流规则、公开恢复入口与多 Agent 协作约定在 `AGENTS.md`、`.agents/skills/**` 与
`ai-plan/public/**` 之间一致，并让 `boot` 能稳定恢复这类治理主题。

## 当前恢复点

- 恢复点编号：`AI-PLAN-GOVERNANCE-RP-002`
- 当前阶段：`Phase 2`
- 当前焦点：
  - 已补齐 `ai-plan/public/README.md` 的 active-topic 暴露，使 `feat/cqrs-optimization` 映射到
    `ai-plan-governance` 时，`boot` 能落到真实的 public tracking / trace 入口。
  - 已在 `AGENTS.md` 增补主 Agent 协调多 worker wave 时的强约束，明确 critical path、`ai-plan`、
    review、validation 与 final integration 归主 Agent 所有。
  - 已新增 `.agents/skills/gframework-multi-agent-batch/`，把“主 Agent 负责派发、核对、更新 `ai-plan`、
    验收并决定是否继续下一波”的流程沉淀为可复用 skill。
  - 已轻量更新 `gframework-boot`、`gframework-batch-boot` 与 `.agents/skills/README.md`，把该模式接入既有
    boot / batch 入口而不重复定义规则。
  - 当前变更面仍限定在 `AGENTS.md`、`.agents/skills/**` 与 `ai-plan/public/**`，没有扩散到运行时代码或产品
    模块。

## 当前状态摘要

- 已落地“`AGENTS.md` 负责强治理约束，skill 负责可执行流程”的混合方案。
- 已修复本 worktree 的一个恢复入口缺口：topic 映射存在，但 active-topic 列表和 public recovery 文件此前缺失。
- 当前主题已经具备后续继续演进 AI workflow 规则所需的 public tracking / trace 基线。

## 当前活跃事实

- 当前分支：`feat/cqrs-optimization`
- `AGENTS.md` 的 `Repository Boot Skill` 现已与仓库实际目录对齐，使用 `.agents/skills/gframework-boot/`
  而不是失效的 `.codex/skills/gframework-boot/` 路径。
- `AGENTS.md` 新增 `Multi-Agent Coordination Rules`，用于约束主 Agent 的职责边界、worker ownership、
  acceptance gate 与 stop condition。
- `.agents/skills/gframework-multi-agent-batch/` 现已包含：
  - `SKILL.md`
  - `agents/openai.yaml`
- `.agents/skills/gframework-boot/SKILL.md` 与 `.agents/skills/gframework-batch-boot/SKILL.md` 现已明确在
  orchestration-heavy 场景下切换或让位给 `gframework-multi-agent-batch`。
- `.agents/skills/README.md` 已把 `gframework-multi-agent-batch` 纳入公开入口说明。
- `ai-plan/public/ai-plan-governance/` 已建立 public tracking / trace 入口，可供后续治理任务继续复用。

## 当前风险

- 漂移风险：若后续只改 `AGENTS.md` 或只改 skill，主 Agent 职责定义可能再次分叉。
- 入口重叠风险：若 `gframework-batch-boot` 与 `gframework-multi-agent-batch` 的边界被继续模糊，公开入口会变得难以选择。
- 恢复噪音风险：若该 topic 后续把每一轮治理细节都堆进 active 文件，`boot` 的默认恢复效率会下降。

## 验证说明

- `python3 scripts/license-header.py --check --paths AGENTS.md ai-plan/public/README.md ai-plan/public/ai-plan-governance/todos/ai-plan-governance-tracking.md ai-plan/public/ai-plan-governance/traces/ai-plan-governance-trace.md`
  - 结果：通过
  - 备注：本轮受 license-header 规则约束的 public 文档文件均已具备 Apache-2.0 头
- `git diff --check`
  - 结果：通过
  - 备注：本轮治理变更未引入 trailing whitespace 或 patch 格式问题
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；满足仓库“完成任务前至少通过一条 build validation”的要求

## 下一步

1. 后续如再次出现“主 Agent 持续派发并验收多个 worker”的任务，优先直接用真实任务验证 `gframework-multi-agent-batch`
   的可恢复性与边界清晰度
2. 若该 skill 的 stop condition、ownership 或 `ai-plan` 记录格式在实战中出现歧义，再回到本 topic 做下一轮治理收口
