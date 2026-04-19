# AI-Plan 治理跟踪

## 目标

继续收口 `ai-plan/` 的目录语义、启动入口与归档策略，避免多 worktree 并行时的公共恢复文档持续膨胀并拖慢
`boot` 的上下文定位。

- 为 `ai-plan/` 建立明确的目录分层
- 区分“可提交共享状态”与“工作树私有状态”
- 明确禁止写入敏感数据、绝对路径和机器本地信息
- 让 `AGENTS.md`、`ai-plan/README.md` 与 boot skill 使用同一套目录语义
- 让 `boot` 能通过公共索引快速定位当前 worktree 的活跃主题
- 为阶段完成和主题完成两类归档建立稳定规则

## 当前恢复点

- 恢复点编号：`AI-PLAN-GOV-RP-005`
- 当前阶段：`Phase 3`
- 当前焦点：
  - 将"主题内 `archive/` 已存在"升级为"active todo/trace 过长时必须归档已完成且已验证阶段"的显式规则
  - 让 active `todos/` / `traces/` 只保留当前恢复点、活跃事实、活跃风险、下一步与 archive 指针
  - 将 `ai-plan-governance`、`ai-first-config-system` 与 `cqrs-rewrite` 的历史阶段从默认启动入口移出
  - 将当前工作树遗留的 `local-plan` 示例迁入 `ai-plan/public/<topic>/`，验证治理规则对多个新 topic
    迁移同样成立

### 已知风险

- 归档遗漏：已完成且已验证的阶段未及时归档，导致 active 入口文件持续膨胀
  - 缓解措施：只要某个 active 主题积累了多个已完成且已验证阶段，就在同一变更里将其细节迁入该主题自己的 `archive/`
- 入口回膨胀：后续新任务直接追加到 active 入口，而不是先归档历史
  - 缓解措施：每次变更前先检查当前 active 入口行数，超过合理范围时优先归档已完成内容
- 跨文档语义漂移：tracking / trace / README 三个入口对同一主题的状态描述不一致
  - 缓解措施：修改任一文档时同步检查其他入口，确保恢复点编号、阶段名称和下一步描述保持一致

## 已完成

- 已为活跃主题建立并使用主题内归档目录：
  - `ai-plan/public/ai-plan-governance/archive/todos/`
  - `ai-plan/public/ai-plan-governance/archive/traces/`
  - `ai-plan/public/ai-first-config-system/archive/todos/`
  - `ai-plan/public/ai-first-config-system/archive/traces/`
  - `ai-plan/public/cqrs-rewrite/archive/todos/`
  - `ai-plan/public/cqrs-rewrite/archive/traces/`
- 已将以下历史内容移出默认 boot 路径：
  - `ai-plan-governance` 的 RP-002 至 RP-004 历史
  - `ai-first-config-system` 截至 `2026-04-17` 的详细跟踪与执行 trace
  - `cqrs-rewrite` 截至 `RP-043` 的详细跟踪与执行 trace
- 已将当前工作树遗留的 analyzer warning reduction 恢复文档从 `local-plan/` 迁入：
  - `ai-plan/public/analyzer-warning-reduction/todos/`
  - `ai-plan/public/analyzer-warning-reduction/traces/`
  - `ai-plan/public/analyzer-warning-reduction/archive/todos/`
  - `ai-plan/public/analyzer-warning-reduction/archive/traces/`
- 已将当前工作树遗留的 documentation governance and refresh 恢复文档从 `local-plan/` 迁入：
  - `ai-plan/public/documentation-governance-and-refresh/todos/`
  - `ai-plan/public/documentation-governance-and-refresh/traces/`
  - `ai-plan/public/documentation-governance-and-refresh/archive/todos/`
  - `ai-plan/public/documentation-governance-and-refresh/archive/traces/`
- 已同步更新 `ai-plan/public/README.md`，将分支 `fix/analyzer-warning-reduction-batch` 映射到新 topic
- 已同步更新 `ai-plan/public/README.md`，将分支 `docs/sdk-update-documentation` 映射到
  `documentation-governance-and-refresh`
- 已同步更新 `AGENTS.md`、`ai-plan/README.md` 与 `gframework-boot`，明确 active 文档不是追加式日志，已完成且已验证阶段必须归档

## 验证

- `find ai-plan/public -maxdepth 5 -type f | sort`
  - 结果：通过
  - 备注：活跃主题、主题内归档文件与主题级归档都已按新目录语义落位
- `wc -l ai-plan/public/ai-plan-governance/todos/ai-plan-governance-tracking.md ai-plan/public/ai-plan-governance/traces/ai-plan-governance-trace.md ai-plan/public/ai-first-config-system/todos/ai-first-config-system-tracking.md ai-plan/public/ai-first-config-system/traces/ai-first-config-system-trace.md ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md ai-plan/public/analyzer-warning-reduction/todos/analyzer-warning-reduction-tracking.md ai-plan/public/analyzer-warning-reduction/traces/analyzer-warning-reduction-trace.md ai-plan/public/documentation-governance-and-refresh/todos/documentation-governance-and-refresh-tracking.md ai-plan/public/documentation-governance-and-refresh/traces/documentation-governance-and-refresh-trace.md`
  - 结果：通过
  - 备注：10 个 active 入口文件当前合计 `508` 行，仍保持为按 topic 精简后的恢复入口，而非追加式历史日志
- `find ai-plan/public/analyzer-warning-reduction -maxdepth 3 -type f | sort`
  - 结果：通过
  - 备注：新 topic 已按 `todos/`、`traces/` 与主题内 `archive/` 目录语义落位
- `find ai-plan/public/documentation-governance-and-refresh -maxdepth 3 -type f | sort`
  - 结果：通过
  - 备注：文档治理 topic 已按 `todos/`、`traces/` 与主题内 `archive/` 目录语义落位
- `test ! -e local-plan`
  - 结果：通过
  - 备注：当前工作树根目录已不再保留 legacy `local-plan/`
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`
  - 结果：通过
  - 备注：`GFramework.Cqrs.Abstractions` 与 `GFramework.Core.Abstractions` 构建通过，`0 warning / 0 error`

## Archive Index

- 治理历史跟踪归档：[ai-plan-governance-history-rp002-rp004.md](../archive/todos/ai-plan-governance-history-rp002-rp004.md)
- 治理历史 trace 归档：[ai-plan-governance-history-rp002-rp004.md](../archive/traces/ai-plan-governance-history-rp002-rp004.md)

## 下一步

1. 继续扫描是否还有遗留的 `local-plan` 或其他非 `ai-plan` 的 durable recovery 文档目录
2. 后续只要某个 active 主题积累了多个已完成且已验证阶段，就在同一变更里将其细节迁入该主题自己的 `archive/`
3. 若某个主题整体完成，再将整个主题目录移入 `ai-plan/public/archive/<topic>/`
