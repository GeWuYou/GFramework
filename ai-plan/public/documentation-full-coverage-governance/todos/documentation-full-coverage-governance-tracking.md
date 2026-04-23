# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-023`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
  - 保持 landing page / API 导航页中的仓库 README 入口可点击，避免读者在 docs 站点里遇到裸路径文本
  - 继续按 `origin/main` 分支 diff 阈值做小批量文档治理，优先处理低风险导航 / 渲染热点
  - 保持 `Game` persistence docs surface 与当前 `README`、源码、`PersistenceTests` 使用同一套 owner / adoption path 叙述
  - 保持 `GFramework.Godot.SourceGenerators/README.md` 与 `docs/zh-CN/tutorials/godot-integration.md` 在生命周期接法上的一致性
  - 保持 active tracking / trace 只承载当前恢复入口，把阶段细节留在 `archive/`

## 当前状态摘要

- `Core`、`Ecs.Arch`、`Cqrs`、`Game`、`Godot` 五个模块族当前都已有 README / landing / topic / API 参考层级的已验证入口。
- `Game` persistence docs surface 当前以 `docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md`
  作为最小巡检集合；若后续 README、runtime public API 或 `PersistenceTests` 变动，应优先复核这一组页面。
- `Godot` runtime 与 generator 入口当前以 `GFramework.Godot/README.md`、
  `GFramework.Godot.SourceGenerators/README.md`、`docs/zh-CN/godot/index.md`、
  `docs/zh-CN/source-generators/index.md`、`docs/zh-CN/tutorials/godot-integration.md` 维持统一 owner / adoption path。
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

## 当前风险

- 当前 `Core` / `Core.Abstractions`、`Ecs.Arch`、`Cqrs`、`Game` 的 XML 治理仍以“类型声明级基线”为主，不等于成员级契约全审计。
- `GFramework.Cqrs` 在当前 WSL / dotnet 环境下仍会读取失效的 fallback package folder，并在标准 build 中触发
  `MSB4276` / `MSB4018`；这是已知环境阻塞，不属于本轮文档回归。
- 当前 WSL 会话里 `git.exe` 可解析但不能执行，应继续使用显式 `--git-dir` / `--work-tree` 绑定作为默认 Git 策略。

## 归档指针

- 详细验证历史（`RP-001` 到 `RP-007`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- 阶段状态归档（`RP-001` 到 `RP-016`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- 时间线归档（`RP-001` 到 `RP-016`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`

## 最新验证

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

## 下一步

1. 若继续执行文档治理批处理，优先改做标题锚点、站内链接和少量非导航型裸路径引用的逐页复核，而不是继续按统一模板机械替换。
2. 若后续继续扩展批处理 skill，可考虑再补充显式单位写法，例如 `75 files 2000 lines`，但当前默认速记已足够覆盖
   常见分支阈值场景。
3. 若后续分支继续调整 `Game` persistence runtime、README 或公共 API，优先复核 `docs/zh-CN/game/data.md`、
   `storage.md`、`serialization.md`、`setting.md` 与 landing page 是否仍保持同一套职责边界。
4. 若后续分支继续调整 `Godot` generator 接法，优先复核 `GFramework.Godot.SourceGenerators/README.md`、
   `docs/zh-CN/tutorials/godot-integration.md` 与相关专题页是否仍保持一致。
