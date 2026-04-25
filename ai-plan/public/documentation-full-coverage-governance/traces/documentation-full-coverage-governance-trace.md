# Documentation Full Coverage Governance Trace

## 2026-04-25

### 当前恢复点：RP-036

- 本轮从 `$gframework-pr-review` 重新进入，目标不再是扩批，而是核对 PR `#290` latest-head review 仍未关闭的 reader-facing 文档问题。
- 使用 `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json` 抓取后确认：PR `#290` 最新 reviewed commit 为 `54b8e5770af9ab3c8a86a396ffa4794fe4bb5181`，CodeRabbit 与 Greptile 各有 `1` 条 open thread，失败检查为 `0`，测试汇总仍为 `2156 passed`。
- 本轮把远端 review 与本地工作树逐项比对后，只接受仍然成立的 5 个 reader-facing 问题：`source-generators` 侧栏 3 个标签与目标标题不一致、`api-reference` 侧栏重复暴露跨栏目入口、`Core` / `Ecs.Arch` / `Game` README 仍保留 XML 覆盖基线字段。
- 当前未提交批次限定在 `docs/.vitepress/config.mts`、3 个模块 README，以及 active tracking / trace；没有继续扩展到其他未被 review 指向的文档文件。

### 当前决策（RP-036）

- 对 PR review 的处理改成“只修当前 latest-head review 仍成立的问题”，不再延续前一轮的批量普查节奏。
- `api-reference` 侧栏不再承载跨栏目目录跳转；跨模块导航继续保留在 `docs/zh-CN/api-reference/index.md` 的正文里，避免侧栏在跳出栏目后发生上下文切换。
- `source-generators` 侧栏项统一与目标文档的 H1 / frontmatter `title` 对齐，避免同一页面在导航、标题与搜索索引里出现多套命名。
- 模块 README 的 XML 阅读表只保留读者有用的“代表类型 / 阅读重点”，把覆盖计数、日期和 `已覆盖` 之类治理痕迹全部留在 `ai-plan/**`。

### 当前验证（RP-036）

- PR review 抓取：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过；PR `#290` 处于 `OPEN`，latest head review 还有 `2` 条 open thread，测试汇总为 `2156 passed`。

- README / 链接校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Core/README.md GFramework.Ecs.Arch/README.md GFramework.Game/README.md`
  - 结果：通过；本轮 3 个 README 调整后链接目标仍然有效。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；`docs/.vitepress/config.mts` 的侧栏调整后站点仍可构建，仅保留既有大 chunk warning。

### 归档指针

- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-rp-023-to-rp-025-2026-04-24.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-023-to-rp-025-2026-04-24.md`

### 下一步

1. 完成 `bun run build` 与 README 链接校验后，提交当前 PR `#290` review 收口批次。
2. 提交后再次运行 `$gframework-pr-review`，确认 CodeRabbit / Greptile 的 open thread 是否已关闭。
3. 若仍有 review 残留，再按 latest-head review 精确收口，不恢复到前一轮的广覆盖批处理模式。
