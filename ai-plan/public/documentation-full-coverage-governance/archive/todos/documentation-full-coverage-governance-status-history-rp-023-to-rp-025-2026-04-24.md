# Documentation Full Coverage Governance Status History RP-023 To RP-025

以下内容从 active tracking 中迁出，用于保留 `DOCUMENTATION-FULL-COVERAGE-GOV-RP-023` 到
`DOCUMENTATION-FULL-COVERAGE-GOV-RP-025` 的批处理状态、验证细节与恢复背景。默认 `boot` 只需要读取 active
tracking 中的最新摘要；若需要追溯 `2026-04-23` 到 `2026-04-24` 的治理细节，再回到本归档文件。

## 迁出的状态摘要

- `2026-04-23` 基于 PR `#272` 的 review follow-up 已完成：
  - 为 `docs/zh-CN/game/data.md` 补充 `UnifiedSettingsDataRepository` 的统一文件布局示例
  - 为 `GFramework.Godot.SourceGenerators/README.md` 补充手写 `_Ready()` / `_ExitTree()` 时显式调用生成方法的最小样例
  - 将过长的 active tracking / trace 瘦身，并把历史摘要迁回 `archive/`
- `2026-04-23` 使用 `$gframework-pr-review` 重新抓取 PR `#272` 后，确认 latest-head review 当前仍有 1 条
  Greptile open thread，定位到 `docs/zh-CN/godot/setting.md:75` 的 inline code 误写成
  `SettingsModel&lt;ISettingsDataRepository&gt;`。
- 结合当前 PR 已改动的 `docs/zh-CN/godot/storage.md` 做同类巡检后，确认 `SaveRepository&lt;TSaveData&gt;`
  也会在 VitePress code span 中按字面量渲染；两处现已在本地统一改为真实泛型写法。
- `2026-04-23` 以 `origin/main`（`aa879d2`，`2026-04-23T17:51:41+08:00`）为批处理基线，对
  `README.md`、`GFramework.*` 与 `docs/zh-CN/**` 执行同类模式巡检，确认剩余热点仅位于
  `docs/zh-CN/core/functional.md` 与 `docs/zh-CN/tutorials/functional-programming.md` 共 8 处。
- 上述 8 处 inline code 中的 `Option&lt;T&gt;`、`Result&lt;T&gt;`、`Nullable&lt;T&gt;` 已统一改为真实
  泛型写法，避免在 VitePress 中显示字面量 HTML entity。
- `2026-04-23` 根据本轮使用反馈，已为 `.agents/skills/gframework-batch-boot/SKILL.md` 与
  `.agents/skills/README.md` 补充数字速记阈值语义：
  - `$gframework-batch-boot 75` 默认表示“当前分支全部提交相对远程 `origin/main` 接近 75 个分支 diff 文件时停止”
  - `$gframework-batch-boot 75 2000` 默认表示“当前分支全部提交相对远程 `origin/main` 接近 75 个文件或 2000 行变更时停止”
  - `75 | 2000` 仅作为可理解的 OR 写法保留，不再作为推荐写法，以避免与 shell pipe 混淆
- `2026-04-23` 以 `origin/main`（`aa879d2`，`2026-04-23T17:51:41+08:00`）为批处理基线，对
  `docs/zh-CN/getting-started/index.md`、`core/index.md`、`game/index.md`、`source-generators/index.md`、
  `api-reference/index.md`、`abstractions/core-abstractions.md`、`abstractions/game-abstractions.md`
  做导航可达性修复，把仓库 README / 根 README 裸路径统一改为指向 GitHub `main` 分支的可点击链接。
- 该批次不改变文档语义，只收口 docs 站点中的入口可达性；适合继续作为小步快跑的低风险治理模式。
- `2026-04-23` 在同一基线下继续收口第二批专题页导航热点，已将 `core/cqrs.md`、`ecs/arch.md`、
  `abstractions/ecs-arch-abstractions.md`、`game/scene.md`、`game/ui.md` 和 6 个
  `source-generators/*.md` 专题页里的 README 裸路径统一改为 GitHub `main` blob 外链。
- 截至提交 `8a11720`（`2026-04-23T21:01:28+08:00`），当前分支相对 `origin/main`（`aa879d2`）的累计 diff
  为 `24` 个文件、`264` 行，仍低于 `$gframework-batch-boot 75` 的停止阈值；但剩余命中已主要是正文语义性提及，不再适合作为同类批处理。
- 当前剩余的托管侧信号是 GitHub `Title check` 对 PR 标题过泛的 inconclusive 提示；这属于 PR 元数据，不是本地
  文件缺陷。
- `2026-04-24` 根据用户反馈完成一轮“公开文档边界”治理，并继续按 `$gframework-batch-boot 75` 向前推进：
  - 在 `AGENTS.md`、`.agents/skills/_shared/DOCUMENTATION_STANDARDS.md` 与
    `.agents/skills/gframework-doc-refresh/SKILL.md` 中新增硬约束，明确禁止把 inventory、覆盖基线、恢复点、
    review backlog、治理批次等 contributor-only 内容写进 `README.md` 与 `docs/**`
  - 将 `docs/zh-CN/core/index.md`、`core/cqrs.md`、`game/index.md`、
    `abstractions/core-abstractions.md`、`abstractions/game-abstractions.md`、`ecs/index.md`、
    `ecs/arch.md`、`abstractions/ecs-arch-abstractions.md` 中的 XML 覆盖表述改写为面向读者的“源码阅读入口”
  - 顺手收口 `api-reference/index.md`、`contributor/development-environment.md` 与
    `source-generators/*.md` 中的内部口吻用语，例如 `landing page`、`验证基线`、`目标类型基线`
  - focused validator 已覆盖本轮触达的 `13` 个公开文档页面并全部通过；站点构建 `cd docs && bun run build`
    已通过，仅保留既有大 chunk warning
- 当前分支 `HEAD` 仍与 `origin/main`（`2de57f5`，`2026-04-23T23:03:40+08:00`）对齐；在提交本轮工作前，
  工作树待提交范围为 `16` 个文件、`224` changed lines，距离 `$gframework-batch-boot 75` 的停止阈值仍很远。
- `2026-04-24` 继续在同一 stop condition 下执行第二个低风险批次，集中修复 `docs/zh-CN/core/*.md` 的历史 frontmatter 缺口：
  - 已为 `architecture.md`、`async-initialization.md`、`command.md`、`configuration.md`、`context.md`、
    `environment.md`、`events.md`、`extensions.md`、`functional.md`、`ioc.md`、`localization.md`、
    `logging.md`、`model.md`、`pause.md`、`pool.md`、`property.md`、`query.md`、`rule.md`、
    `state-management.md`、`system.md`、`utility.md` 补齐 frontmatter
  - 顺手修复 `core/ioc.md` 的 `xref:System.Threading.ReaderWriterLockSlim` 坏链，以及
    `core/state-management.md` 中 4 处缺少 `.md` 后缀的站内链接
  - 当前 `docs/zh-CN/core/*.md` 已全部具备 frontmatter；focused validator 对这 `21` 个页面全部通过，`bun run build`
    再次通过，仅保留既有大 chunk warning
- 截至当前未提交工作树，`HEAD` 相对 `origin/main` 的累计 branch diff 仍为 `18` 个文件；新增待提交批次为 `21` 个文件、
  `126` changed lines，合并后仍显著低于 `$gframework-batch-boot 75` 的停止阈值。
- `2026-04-24` 继续在同一 stop condition 下执行第三个低风险批次，集中清理 `docs/zh-CN` 其余“完全缺 frontmatter”的页面：
  - 已为 `best-practices/*.md` 中缺口页、`contributing.md`、`faq.md`、`game/config-system.md`、
    `getting-started/*.md`、`godot/coroutine.md`、6 个 `source-generators/*.md`、`troubleshooting.md`、
    `tutorials/advanced-patterns.md`、`tutorials/basic/index.md` 与 `tutorials/index.md` 补齐 frontmatter
  - 在同批次内修复 `best-practices/multiplayer.md` 的未闭合代码块、`source-generators/*.md` 中缺少 `.md`
    后缀的相对链接，以及 `troubleshooting.md` 里 3 处目录索引死链
  - 当前 `docs/zh-CN` 已不存在“完全缺 frontmatter”的页面；剩余 metadata 热点只剩
    `docs/zh-CN/index.md` 与 `docs/zh-CN/tutorials/basic/01-07.md` 共 `8` 个“已有 frontmatter 但缺 title /
    description”的页面
  - 本批次落地前，当前分支相对 `origin/main` 的累计 branch diff 为 `39` 个文件；连同本轮工作和 tracking / trace
    更新后，预计提交后累计 diff 约为 `63` 个文件，仍低于 `$gframework-batch-boot 75` 的停止阈值

## 迁出的验证记录

- `2026-04-24` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN`
  - 结果：失败；暴露出仓库既有的 `53` 个历史文档问题（大量缺少 frontmatter、既有坏链与未标语言代码块），不由本轮改动引入，因此本轮改用 focused validator 证明任务级结果。
- `2026-04-24` `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Core`
  - 结果：通过；确认 `Core` 模块的 README、landing、topic 与 fallback docs 入口仍可解析。
- `2026-04-24` `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Game`
  - 结果：通过；确认 `Game` 模块的 README、landing、topic 与 fallback docs 入口仍可解析。
- `2026-04-24` focused validator（逐个校验本轮触达页面）
  - 结果：通过；`docs/zh-CN/abstractions/core-abstractions.md`、
    `abstractions/ecs-arch-abstractions.md`、`abstractions/game-abstractions.md`、
    `api-reference/index.md`、`contributor/development-environment.md`、`core/cqrs.md`、`core/index.md`、
    `ecs/arch.md`、`ecs/index.md`、`game/index.md`、
    `source-generators/bind-node-signal-generator.md`、
    `source-generators/cqrs-handler-registry-generator.md`、
    `source-generators/get-node-generator.md` 的 frontmatter / links / code blocks 全部通过。
- `2026-04-24` `bun run build`（工作目录：`docs/`）
  - 结果：通过；仅保留既有大 chunk warning。
- `2026-04-24` `python3 - <<'PY' ...`（检查 `docs/zh-CN/core/*.md` frontmatter）
  - 结果：通过；`docs/zh-CN/core/` 当前所有 Markdown 页面均已带 frontmatter。
- `2026-04-24` focused validator（逐个校验 `docs/zh-CN/core/*.md` 的 `21` 个页面）
  - 结果：通过；过程中暴露并已修复 `core/ioc.md` 的 `ReaderWriterLockSlim` 坏链与
    `core/state-management.md` 的 4 处站内坏链；剩余仅为既有代码块语言 warning，不影响任务级通过。
- `2026-04-24` `bun run build`（工作目录：`docs/`，第二次）
  - 结果：通过；frontmatter 与坏链修复后站点仍可正常构建，仅保留既有大 chunk warning。
- `2026-04-24` focused validator（逐个校验第三批触达的 `22` 个页面）
  - 结果：通过；frontmatter、真实坏链与未闭合代码块问题均已修复，剩余仅为 `best-practices/architecture-patterns.md`、
    `best-practices/index.md`、`contributing.md`、`troubleshooting.md` 与 `tutorials/index.md` 的既有代码块语言 warning。
- `2026-04-24` `bun run build`（工作目录：`docs/`，第三次）
  - 结果：通过；第三批 frontmatter 与链接修复后站点仍可正常构建，仅保留既有大 chunk warning。
- `2026-04-23` `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#272` 处于 `OPEN`，latest head commit 存在 1 条 Greptile open thread，定位到
    `docs/zh-CN/godot/setting.md:75` 的 inline code HTML entity 渲染问题。
- `2026-04-23` `rg -n '`[^`]*&lt;[^`]*`|`[^`]*&gt;[^`]*`' GFramework.Godot.SourceGenerators/README.md GFramework.Godot/README.md README.md docs/zh-CN/api-reference/index.md docs/zh-CN/game/data.md docs/zh-CN/game/serialization.md docs/zh-CN/game/setting.md docs/zh-CN/game/storage.md docs/zh-CN/godot/setting.md docs/zh-CN/godot/storage.md docs/zh-CN/source-generators/index.md`
  - 结果：命中 `docs/zh-CN/godot/setting.md:75` 与 `docs/zh-CN/godot/storage.md:102` 两处同类写法，均已修正。
- `2026-04-23` `rg -n '`[^`]*&lt;[^`]*`|`[^`]*&gt;[^`]*`' README.md GFramework.* docs/zh-CN -g '*.md'`
  - 结果：命中 `docs/zh-CN/core/functional.md` 与 `docs/zh-CN/tutorials/functional-programming.md` 共 8 处，已全部修正。
- `2026-04-23` `sed -n '1,260p' .agents/skills/gframework-batch-boot/SKILL.md` 与 `sed -n '1,220p' .agents/skills/README.md`
  - 结果：确认原文仅描述自然语言 stop condition，没有定义数字速记或多阈值 OR 语义；现已补齐。
- `2026-04-23` `rg -n '`GFramework\\.[^`]+/README\\.md`|`docs/zh-CN/[^`]+\\.md`|仓库根 `README\\.md`' docs/zh-CN -g '*.md'`
  - 结果：确认 landing / API 导航页仍有一批裸路径仓库入口；本轮已先修复 `getting-started`、`core`、`game`、
    `source-generators`、`api-reference` 与两个 abstractions 页面。
- `2026-04-23` `rg -n '`GFramework\\.[^`]+/README\\.md`|仓库根 `README\\.md`' docs/zh-CN -g '*.md'`
  - 结果：定位第二批专题页导航热点，已修复 `core/cqrs.md`、`ecs/arch.md`、`abstractions/ecs-arch-abstractions.md`、
    `game/scene.md`、`game/ui.md` 以及 6 个 `source-generators/*.md` 页面。
- `2026-04-23` `bun run build`（工作目录：`docs/`）
  - 结果：通过；仓库 README 外链改为 GitHub `main` blob 后，不再触发 VitePress dead link；仅保留既有大 chunk warning。

## 迁出的下一步

1. 若继续执行 `$gframework-batch-boot 75`，优先处理 `docs/zh-CN/index.md` 与 `tutorials/basic/01-07.md` 这 `8`
   个“已有 frontmatter 但缺 `title` / `description`”的 metadata 缺口。
2. 若后续继续扩展批处理 skill，可考虑再补充显式单位写法，例如 `75 files 2000 lines`，但当前默认速记已足够覆盖
   常见分支阈值场景。
3. 若后续分支继续调整 `Game` persistence runtime、README 或公共 API，优先复核 `docs/zh-CN/game/data.md`、
   `storage.md`、`serialization.md`、`setting.md` 与 landing page 是否仍保持同一套职责边界。
4. 若后续分支继续调整 `Godot` generator 接法，优先复核 `GFramework.Godot.SourceGenerators/README.md`、
   `docs/zh-CN/tutorials/godot-integration.md` 与相关专题页是否仍保持一致。
