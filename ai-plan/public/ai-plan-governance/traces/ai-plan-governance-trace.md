# AI-Plan Governance 追踪

## 2026-11-06 · AI-PLAN-GOVERNANCE-RP-003

- 用户确认删除启动、批处理与多 agent 流程包装，保留三个专用 skills；默认 PR 后交接。
- 已核对仓库基线 `659d4a61`，使用独立任务分支，保持主 checkout 为 main。
- 原 RP-002 活跃 tracking/trace 移入 topic 归档，保留历史决策与当时验证。
- 治理覆盖 AGENTS.md、skill 入口/菜单/索引、无消费者辅助资源及当前恢复入口。
- Issue 输出仍包含失效启动建议，已保留旧 JSON 字段和模式标签，将 recommended_skill 改为 null；新增澄清和续作回归场景。
- 验证完成：三个 skill 结构/菜单/引用、六个模块输入、15 项相关 Python 测试、许可证、命名与模块边界检查通过。
- 适配器 schema、六个能力 dry-run、任务工具链下 doctor 与 harness check 通过。
- Core.Abstractions Release 构建（包含 Cqrs.Abstractions 依赖）通过，0 warning / 0 error。
- RP-002 归档与原文件字节一致；未运行完整 .NET 测试、solution 构建或文档站构建；未声称菜单显示或未来 agent 行为验收。
- 下一步：提交并发布 GitHub PR，执行一次自动合并设置与回读，保留任务工作树交接。

历史阶段见 [RP-002 追踪归档](../archive/traces/ai-plan-governance-trace-rp002.md)。
