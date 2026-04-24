# Documentation Full Coverage Governance Trace

## 2026-04-24

### 当前恢复点：RP-028

- 以 `origin/main`（`a8447a6`，`2026-04-24T12:53:39+08:00`）为 `$gframework-batch-boot 75` baseline，确认 `RP-027` 提交后的当前分支累计 diff 为 `10` 个文件。
- 选择“站内 Markdown 链接显式扩展名统一”作为本批次低风险切片，集中处理教程、最佳实践、Core 与 Godot 页面中的目录链接和缺 `.md` 的旧写法。
- 本批次修改了 `25` 个 `docs/zh-CN` 页面，把 `./`、`../`、`/zh-CN/` 开头的站内 Markdown 链接统一补齐为显式 `.md` 或 `index.md`。

### 当前决策（RP-028）

- 当 branch diff 仍显著低于 `75` 文件阈值时，继续优先消化低风险链接规范化和 README / landing page 对齐问题，不在同一批次混入高语义重写。
- 构建失败优先视为机械链接转换回归，先修复批量规则副作用再继续，而不是带着失效链接进入后续批次。
- active `ai-plan` 入口继续保持轻量，只记录当前恢复点、batch metric、验证结果与下一批候选项。

### 当前验证（RP-028）

- 站内链接巡检：
  - `python3 - <<'PY' ...`（扫描 `docs/zh-CN/**/*.md` 中以 `./`、`../`、`/zh-CN/` 开头且未带扩展名的 Markdown 链接）
  - 结果：通过；当前 `docs/zh-CN` 站内 Markdown 链接已无缺失扩展名的命中。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；统一 `25` 个页面的站内链接后站点仍可正常构建，仅保留既有大 chunk warning。
- 当前 stop-condition metric：
  - 本批次 write set 为 `27` 个文件（`25` 个文档页面 + `2` 个 `ai-plan` 入口）；本批次提交后分支 diff 为 `29 / 75` 个 changed files。

### 归档指针

- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-rp-023-to-rp-025-2026-04-24.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-023-to-rp-025-2026-04-24.md`

### 下一步

1. 若继续执行 `$gframework-batch-boot 75`，优先盘点模块 `README.md`、`docs/index.md` 与文档落地页之间仍未对齐的文档入口、BOM 与旧链接写法。
2. 推送本批次 commit 后，再次执行 `$gframework-pr-review`，确认 PR `#282` 的 unresolved review threads 是否已在新 head commit 上消失。
