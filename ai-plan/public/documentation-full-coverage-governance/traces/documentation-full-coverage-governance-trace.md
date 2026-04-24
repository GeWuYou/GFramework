# Documentation Full Coverage Governance Trace

## 2026-04-24

### 当前恢复点：RP-026

- 使用 `$gframework-pr-review` 抓取 PR `#282`，确认 latest head commit
  `982249151ecf8acdff3e62e664034bf95dfacd75` 当前仍有 `3` 条 CodeRabbit 与 `1` 条 Greptile open thread。
- 按“只处理 latest-head unresolved threads 中仍成立的问题”的原则，本轮仅收口 4 条本地可复现的 follow-up：
  - 将 `tracking.md` 与 `trace.md` 的活动入口瘦身，并把 `RP-023` 到 `RP-025` 的细节迁入新 archive 文件
  - 将 `docs/zh-CN/core/context.md` 的标题与主标题本地化为 `上下文（Context）`
  - 将 `docs/zh-CN/troubleshooting.md` 中 `/zh-CN/core/architecture` 与 `/zh-CN/faq` 统一补成显式 `.md` 链接

### 当前决策（RP-026）

- PR review follow-up 继续遵守“先本地验证，再决定是否修复”；对已经过时或无法在当前分支复现的评论不做追随式修改。
- active `ai-plan` 入口只保留当前恢复点、活动事实、风险、最新验证与下一步；批次细节统一迁入 `archive/`。
- `docs/zh-CN` 页面应优先使用中文标题；同一帮助块中的绝对站内链接应保持一致的显式 `.md` 写法。

### 当前验证（RP-026）

- PR review 抓取：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#282` 处于 `OPEN`，最新 review 线程与测试状态已成功解析，测试汇总为 `2156 passed`。
- 链接巡检：
  - `rg -n --pcre2 '\\]\\(/zh-CN/[^)]+(?<!\\.md)\\)' docs/zh-CN/troubleshooting.md`
  - 结果：当前无命中；`/zh-CN/core/architecture` 与 `/zh-CN/faq` 已统一补成显式 `.md` 链接。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮文档与 `ai-plan` 调整后站点仍可正常构建，仅保留既有大 chunk warning。

### 归档指针

- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-rp-023-to-rp-025-2026-04-24.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-023-to-rp-025-2026-04-24.md`

### 下一步

1. 推送当前 follow-up commit 后，再次执行 `$gframework-pr-review`，确认 PR `#282` 的 unresolved review threads 是否已在新 head commit 上消失。
2. 若继续执行 `$gframework-batch-boot 75`，优先处理 `docs/zh-CN/index.md` 与 `tutorials/basic/01-07.md` 这 `8` 个“已有 frontmatter 但缺 `title` / `description`”的 metadata 缺口。
