# Documentation Governance And Refresh 跟踪

## 目标

继续以“文档必须可追溯到源码、测试与真实接入方式”为原则，维护 `GFramework` 的仓库入口、模块入口与
`docs/zh-CN` 采用链路，避免 README、专题页与教程再次偏离当前实现。

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-GOVERNANCE-REFRESH-RP-018`
- 当前阶段：`Phase 3`
- 当前焦点：
  - Godot 栏目本轮重写页已完成稳定性复核，当前可把详细实施历史迁入 `archive/`
  - active tracking / trace 已收口为默认 `boot` 可直接恢复的最小入口
  - 下一次推送后需要重新执行 PR #268 review 跟进，确认 CodeRabbit / Greptile open thread 是否继续收敛

## 当前状态摘要

- `docs/zh-CN/godot/` 当前高优先级页面集已完成源码优先收口：
  `index.md`、`architecture.md`、`scene.md`、`ui.md`、`signal.md`、`extensions.md`、`logging.md`
- `docs/zh-CN/tutorials/godot-integration.md` 已与当前包关系、生成器协作顺序和运行时边界对齐
- 本轮再次执行页面级校验与 `docs` 站点构建，结果均通过
- 当前主题保持 active，但 active 文档已不再承载大段历史，只保留恢复所需事实与下一步

## 当前活跃事实

- 当前分支 `docs/sdk-update-documentation` 仍映射到 `documentation-governance-and-refresh`
- 当前 worktree 下未发现 `ai-plan/private/` 恢复目录，本主题以 public artifacts 作为唯一恢复入口
- 本轮新增归档：
  - `ai-plan/public/documentation-governance-and-refresh/archive/todos/documentation-governance-and-refresh-history-through-2026-04-22.md`
  - `ai-plan/public/documentation-governance-and-refresh/archive/traces/documentation-governance-and-refresh-history-through-2026-04-22.md`
- 2026-04-22 之前的长篇历史已存在于 2026-04-18 与 RP-001 through RP-008 的归档文件中
- 当前待处理事项已经从“继续重写 Godot 页面”切换为“等待下一次推送后的 PR review follow-up”

## 当前风险

- review 跟进遗漏风险：如果下一次推送后未重新抓取 PR #268 的 latest-head open threads，可能会错过新一轮
  CodeRabbit / Greptile 意见
  - 缓解措施：推送后优先执行 `$gframework-pr-review`，以 unresolved thread 数量变化作为下一步入口
- active 文档再次膨胀风险：如果后续把细节实施记录继续直接追加到 active 文件，会重新削弱 `boot` 的恢复效率
  - 缓解措施：阶段性结果先写入 `archive/`，active 只保留恢复点、活跃事实、风险、验证和下一步
- 文档回漂风险：若后续修改 README 或专题页时跳过源码 / 测试核对，可能把旧教程里的叙述重新写回当前页面
  - 缓解措施：继续维持“源码 / 测试 / `*.csproj` 优先，`ai-libs/` 只补 adoption path”的证据顺序

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

1. 下一次推送后执行 `$gframework-pr-review`，确认 PR #268 的 CodeRabbit / Greptile unresolved threads 是否继续减少
2. 如果 review 已收敛且没有新增文档偏差，再评估是否将整个 topic 迁入 `ai-plan/public/archive/`
