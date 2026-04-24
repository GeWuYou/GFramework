# Documentation Full Coverage Governance Trace

## 2026-04-24

### 当前恢复点：RP-031

- 当前批次聚焦新的低风险 reader-facing README 缺口，只处理根 `README.md` 的路径式链接标签和对应的 active tracking / trace 基线更新。
- 以 `origin/main`（`4c2994e`，`2026-04-24 17:57:23 +0800`）为 `$gframework-batch-boot 75` baseline；当前本地 `docs/sdk-update-documentation` 与该基线同步，branch cumulative diff 起始值为 `0 / 75`。
- 本批次随后扩展到 `6` 个中文 landing / abstraction 页面标题本地化，总 write set 仍保持在 reader-facing 文案层。

### 当前决策（RP-031）

- 当当前分支重新与 `origin/main` 对齐后，`$gframework-batch-boot 75` 可以继续推进，但仍只接受低风险、读者可见、易验证的小批次。
- 公开 README 的表格入口不应继续把文件路径本身暴露成链接标签；入口名称应直接告诉读者要进入哪个模块 README。
- active tracking / trace 里的 stop-condition 指标必须反映当前真实基线，不再沿用已经过时的 `58 / 75` 历史值。

### 当前验证（RP-031）

- 基线确认：
  - `git --git-dir=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework/.git/worktrees/GFramework-update-documentation --work-tree=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework-WorkTree/GFramework-update-documentation for-each-ref --format='%(refname:short) %(objectname:short) %(committerdate:iso8601)' refs/heads/main refs/remotes/origin/main refs/heads/docs/sdk-update-documentation HEAD`
  - 结果：通过；`docs/sdk-update-documentation` 与 `origin/main` 当前同为 `4c2994e`，本地 `main` 仍停留在 `84b40a2`，因此本轮按 skill 规则选择更新的 `origin/main` 作为 baseline。
- 当前 stop-condition metric：
  - `git ... diff --name-only origin/main...HEAD | wc -l`
  - 结果：通过；当前 branch cumulative diff 为 `0 / 75`。
- 热点扫描：
  - `rg -n '\\[[^]]*(?:README\\.md|docs/[^]]+|GFramework\\.[^]]+/README\\.md|/zh-CN/[^]]+)\\]\\((?:README\\.md|docs/[^)]+|GFramework\\.[^)]+/README\\.md|/zh-CN/[^)]+)\\)' README.md docs GFramework.*/README.md`
  - 结果：通过；当前仅在根 `README.md` 的内部支撑模块表格中命中 `2` 处路径式链接标签，适合作为本批次 reader-facing 修正切片。
- 英文标题扫描：
  - `python3 - <<'PY' ...`（扫描 `docs/zh-CN/**/*.md` 中纯英文 `title` / H1）
  - 结果：通过；当前 landing / abstraction 页仍有一组纯英文入口标题，其中 `core/index.md`、`game/index.md`、`source-generators/index.md` 与 `abstractions/*.md` 的 `title` / H1 适合作为同批次标题本地化切片。
- 第二轮标题本地化：
  - 同一轮扫描显示 `docs/zh-CN/core/architecture.md`、`command.md`、`events.md`、`logging.md`、`property.md`、`query.md` 仍保留纯英文 `title` / H1；这些页面只需做中英对照标题调整，不涉及正文结构或链接变更，适合作为后续同批次切片。
- 停批判断：
  - 再次扫描后，剩余纯英文 `title` 只剩 `docs/zh-CN/core/cqrs.md` 的 `CQRS` 与 `docs/zh-CN/index.md` 的 `GFramework`；它们属于缩写或品牌名，不再作为当前 reader-facing 本地化批次的优先对象。
- 构建验证：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；根 `README.md` 链接标签修正与 `docs/zh-CN` 标题本地化后站点仍可构建，仅保留既有大 chunk warning。

### 归档指针

- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-rp-023-to-rp-025-2026-04-24.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-023-to-rp-025-2026-04-24.md`

### 下一步

1. 提交当前批次，保留根 `README.md` 入口标签修正、`docs/zh-CN` 标题本地化和 `ai-plan` 恢复点同步更新。
2. 若继续下一轮 `$gframework-batch-boot 75`，优先重新扫描剩余 README / landing page 的路径式链接标签和内部治理措辞，而不是继续本地化品牌名或缩写标题。
