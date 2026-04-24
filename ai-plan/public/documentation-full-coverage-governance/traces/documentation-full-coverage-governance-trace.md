# Documentation Full Coverage Governance Trace

## 2026-04-24

### 当前恢复点：RP-024

- 根据用户反馈，将本轮目标重定义为“清理公开文档中的治理盘点式内容，并把同类约束补进仓库规范与 doc-refresh skill”。
- 用户随后补充明确使用 `$gframework-batch-boot 75`，因此继续沿用 `origin/main` 作为固定基线，并把 `75` changed
  files 作为主停止条件。
- 本轮执行的修复：
  - 在 `AGENTS.md`、`.agents/skills/_shared/DOCUMENTATION_STANDARDS.md` 与
    `.agents/skills/gframework-doc-refresh/SKILL.md` 中新增公开文档边界规则，禁止把 inventory、覆盖基线、
    恢复点、review backlog 和治理批次写入 `README.md` 与 `docs/**`
  - 将 `docs/zh-CN/core/index.md`、`core/cqrs.md`、`game/index.md`、
    `abstractions/core-abstractions.md`、`abstractions/game-abstractions.md`、`ecs/index.md`、
    `ecs/arch.md`、`abstractions/ecs-arch-abstractions.md` 的 XML 覆盖 / inventory 段落改写成读者导向的源码阅读入口
  - 继续收口 `api-reference/index.md`、`contributor/development-environment.md` 与
    `source-generators/*.md` 中的内部术语，例如 `landing page`、`验证基线`、`目标类型基线`
  - 为 `docs/zh-CN/contributor/development-environment.md` 补齐 frontmatter，使其满足当前文档规范

### 当前决策（RP-024）

- 公开文档只承载采用路径、阅读入口、模块边界和可验证示例；治理盘点、覆盖状态和恢复点一律留在 `ai-plan/**`。
- 当 XML 治理结果需要体现在公开文档里时，只输出“优先看哪些类型 / 命名空间 / 契约以及为什么”，不输出计数、日期或状态表。
- `$gframework-batch-boot 75` 的基线采用 `origin/main`（`2de57f5`，`2026-04-23T23:03:40+08:00`）。
- 由于当前 `HEAD` 仍与 `origin/main` 对齐，分支级 diff 暂时仍为 `0`；提交前工作树待提交范围为 `16` 个文件、
  `224` changed lines，因此本轮仍远低于 `75` 文件阈值。

### 当前验证（RP-024）

- 同类治理内容巡检：
  - `rg -n 'XML Inventory|XML 覆盖基线|XML 状态|基线状态|盘点|治理优先级|审计入口|覆盖基线|恢复点|验证基线|目标类型基线|目标字段基线|类型审计|契约审计|源码 / API' docs/zh-CN README.md -g '*.md'`
  - 结果：公开页已无同类命中；剩余 `inventory` 命中仅来自正常代码示例变量名。
- skill 自检：
  - `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Core`
  - `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Game`
  - 结果：均通过；代表模块的 README / docs 入口映射仍有效。
- 全量 docs 校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN`
  - 结果：失败；暴露 `53` 个仓库既有历史问题（缺少 frontmatter、坏链、未标语言代码块），不属于本轮改动。
- focused validator：
  - 逐个校验本轮触达的 `13` 个公开文档页面
  - 结果：全部通过。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；仅保留既有大 chunk warning。

### 下一步

1. 继续执行 `$gframework-batch-boot 75` 时，优先排查少量公开页里的内部工程术语残留、标题锚点和站内链接热点。
2. 若后续需要大范围补 frontmatter / code fence language，应单独开一个新的低风险文档治理批次，而不是混入模块语义刷新。

## 2026-04-23

### 当前恢复点：RP-023

- 按当前使用反馈继续执行 `documentation-full-coverage-governance` 下的 skill 文档治理。
- 本轮目标定义为“继续沿用上一批的 GitHub 外链策略，收口专题页里的裸路径 README 入口”。
- 本轮执行的修复：
  - 将 `docs/zh-CN/core/cqrs.md` 与 `ecs/arch.md` 的仓库 README 入口改为 GitHub `main` blob 外链
  - 将 `docs/zh-CN/abstractions/ecs-arch-abstractions.md`、`game/scene.md`、`game/ui.md` 的回跳 README 入口改为可点击链接
  - 将 `docs/zh-CN/source-generators/priority-generator.md`、`context-aware-generator.md`、
    `bind-node-signal-generator.md`、`godot-project-generator.md`、`get-node-generator.md`、
    `auto-register-exported-collections-generator.md` 的推荐阅读 README 入口改为可点击链接
  - 同步更新 active tracking / trace，记录第二批导航治理与新的恢复点

### 当前决策（RP-023）

- 继续使用 `origin/main` 作为 `$gframework-batch-boot 75` 的固定基线，并以“分支累计 diff 文件数”作为主状态指标。
- 对文档治理类批次，优先选择“导航可达性 / 渲染一致性”这类不改变产品语义的低风险切片。
- 在 docs 页面里出现仓库内 README 路径时，优先使用可点击的相对链接，而不是裸路径代码片段。
- 当 docs 页需要跳转到 `docs/` 外部的 README 时，使用 GitHub `main` 分支 blob 外链，而不是跨出 `docs/` 根目录的相对路径。
- 第二批继续沿用同一外链策略，避免在同一 docs surface 中混用“裸路径 / 相对死链 / GitHub 外链”三套入口风格。

### 当前验证（RP-023）

- 导航热点巡检：
  - `rg -n '`GFramework\\.[^`]+/README\\.md`|`docs/zh-CN/[^`]+\\.md`|仓库根 `README\\.md`' docs/zh-CN -g '*.md'`
  - 结果：命中 landing / API 导航页中的裸路径仓库入口，已按本轮批次收口 7 个页面。
- 第二批专题页巡检：
  - `rg -n '`GFramework\\.[^`]+/README\\.md`|仓库根 `README\\.md`' docs/zh-CN -g '*.md'`
  - 结果：命中 `core/cqrs.md`、`ecs/arch.md`、`abstractions/ecs-arch-abstractions.md`、`game/scene.md`、
    `game/ui.md` 与 6 个 `source-generators/*.md` 专题页，均已修复。
- 构建校验：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；将仓库 README 跳转改为 GitHub `main` blob 外链后，不再触发 VitePress dead link；仅保留既有大 chunk warning。
- 当前阈值状态：
  - `git diff --name-only origin/main...HEAD | wc -l` => `24`
  - `git diff --numstat origin/main...HEAD` 汇总 => `264` changed lines
  - 结论：尚未接近 `75` 文件阈值，但剩余命中主要是正文语义性提及，当前批次在低风险模板化导航治理上可先收口。

### 归档摘要（RP-022）

- 为 `.agents/skills/gframework-batch-boot/SKILL.md` 与 `.agents/skills/README.md` 补齐数字速记 stop condition 语义。
- 明确 `$gframework-batch-boot 75` / `75 2000` 默认绑定 `origin/main` 累计 diff 口径。
- 完成第一批 landing / API 导航页 README 外链治理，并通过 `docs/` 站点构建。

### 归档指针

- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`

### 下一步

1. 提交并推送本地修正后，再次抓取 PR `#272`，确认 Greptile open thread 是否已在新 head commit 上消失。
2. 若继续执行文档治理批处理，优先排查剩余的非导航型裸路径引用、标题锚点与站内链接热点，而不是扩成跨模块大波次。
