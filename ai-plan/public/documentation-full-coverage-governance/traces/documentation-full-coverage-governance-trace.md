# Documentation Full Coverage Governance Trace

## 2026-04-28

### 当前恢复点：RP-048

- 本轮按 `$gframework-pr-review` 抓取当前 PR `#299`，确认 latest head review 当前只剩 `1` 条 `CodeRabbit` open thread 与 `1` 条 nitpick；两者都指向 active tracking 文档本身，`Greptile` 与 `Gemini Code Assist` 当前无 open thread，测试汇总为 `2159 passed`，另有 `Title check` inconclusive。
- 本地复核后确认：此前针对 `docs/zh-CN/abstractions/index.md`、`docs/zh-CN/core/lifecycle.md` 与相关教程 / 排障页的 review follow-up 已不再是当前 remote latest-head review 的剩余阻塞项。
- 当前仍需收口的只剩两件事：为 `RP-048` 补齐明确的“下一步”段落，以及把 `RP-045` 到 `RP-047` 的逐命令时间线从 active trace 下沉到归档文件。

### 当前决策（RP-048）

- 本轮限定只修改 `ai-plan/public/documentation-full-coverage-governance` 下的 tracking / trace 文档，不再扩展到已经收口的公开文档页面。
- active trace 只保留当前恢复点、验证结论、下一步与归档指针；`RP-041` 到 `RP-048` 的阶段细节转入专门的 trace archive，逐命令验证继续保留在 validation history archive。
- 不把 `Title check` 当成仓库文件修复项；本轮完成后只需要在提交推送后重新抓取 PR review，确认 remote 线程状态是否清空。

### 当前验证（RP-048）

- PR review 抓取：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#299` 处于 `OPEN`，latest head review 有 `1` 条 `CodeRabbit` open thread 与 `1` 条 nitpick，`Greptile` / `Gemini Code Assist` 当前无 open thread，测试汇总为 `2159 passed`，仅剩 `Title check` inconclusive。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；active tracking 收口与时间线归档瘦身后站点仍可构建，仅保留既有大 chunk warning。
- 详细时间线归档：
  - `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-041-to-rp-048-2026-04-28.md`
- 详细验证归档：
  - `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-rp-041-to-rp-048-2026-04-28.md`

### 下一步（RP-048）

1. 提交本轮 active tracking 收口改动，并将提交推送到 PR `#299`。
2. 推送后重新抓取 `$gframework-pr-review`，确认 latest-head review 是否只剩 `Title check` 或已全部清空。
3. 若仍有新的文档 review 线程，继续按 latest-head review 精确收口，不恢复关键词驱动的机械扩批。

## 2026-04-27

### 已归档历史（RP-041 到 RP-047）

- `RP-045` 到 `RP-047` 的 batch boot 逐阶段时间线、branch diff 计量与 review follow-up 决策，已迁入专门的 trace archive，避免 active trace 继续保留逐命令历史。
- 对应的页面校验、README 链接校验与站点构建命令，继续保留在 validation history archive 中，供后续追溯。
- 归档路径：
  - `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-041-to-rp-048-2026-04-28.md`
  - `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-rp-041-to-rp-048-2026-04-28.md`

### 当前恢复点：RP-044

- 本轮从 `$gframework-pr-review` 重新进入，继续沿用显式 `--git-dir` / `--work-tree` 绑定确认当前分支仍为 `docs/sdk-update-documentation`，并通过 `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json` 抓取当前 PR `#296`。
- 抓取结果显示 latest reviewed commit 为 `5778782df05e22dd24dc95189dd768458afb8537`，共有 `4` 条 open thread：`GFramework.Game.SourceGenerators/README.md` 的表头仍带路径视角、`GFramework.Game/README.md` 有重复 `storage.md` 链接、`docs/zh-CN/tutorials/godot-integration.md` 与 `docs/zh-CN/godot/extensions.md` 还有 reader-facing 措辞收口空间。
- 本地逐条复核后确认这 `4` 条都仍成立，但都属于低风险文档收口；唯一 failed check `Title check` 只是 PR 标题元数据提示，不属于仓库文件内修复范围。

### 当前决策（RP-044）

- 接受 latest-head review 中仍成立的 `4` 条文档修正，不扩展到 review 未指向的其它页面，避免在当前接近 branch-size stop condition 的阶段继续增大 review 面。
- 对 README 表格和导航问题，只做 reader-facing 命名与去重；对教程与 Godot 页面，只做措辞收口，不改变现有采用路径与示例结构。
- 在同一轮里同步更新 active topic tracking / trace，并在提交前运行最小页面校验、README 链接校验与站点构建。

### 当前验证（RP-044）

- PR review 抓取：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#296` 处于 `OPEN`，latest head review 共有 `4` 条 open thread，测试汇总为 `2156 passed`，仅剩 `Title check` inconclusive。
- README 链接校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Game.SourceGenerators/README.md GFramework.Game/README.md`
  - 结果：通过；本轮 2 个 README 的 reader-facing 表格与导航去重调整后链接目标有效。
- 页面校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/godot-integration.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/extensions.md`
  - 结果：通过；两页 frontmatter、链接与代码块校验均通过。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮 PR `#296` review 收口后的站点仍可构建，仅保留既有大 chunk warning。

### 当前恢复点：RP-043

- 在提交 `docs(reader-facing): 统一站内入口与公开术语` 后重新计算 branch diff，确认当前工作树继续补一批新文件后已到 `46` changed files，已经接近 `$gframework-batch-boot 50` 的停止线。
- 因此本轮最后只接受 10 个还没进入 branch diff 的文件：`tutorials/godot-integration.md`、`game/setting.md`、`game/serialization.md`、`godot/index.md`、`godot/architecture.md`、`godot/storage.md`、`godot/logging.md`、`godot/setting.md`、`godot/extensions.md`、`core/architecture.md`。
- 这批文件统一收口的是同一类问题：把 `旧文档`、`ai-libs`、`.Wait()`、`family` 之类维护 / 内部口吻改成当前采用指导，不扩新结构、不重写示例体系。

### 当前决策（RP-043）

- 当前 stop condition 已接近阈值，因此这批验证通过后立即停止继续扩批，避免 branch diff 超过 `50` files 或让 review 面退化。
- 提交后本轮默认结束；后续若继续，应从 PR review 或剩余未触达的细页重新开一轮，而不是在同一轮里继续堆文件数。

### 当前验证（RP-043）

- 单页校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/godot-integration.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/setting.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/serialization.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/index.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/architecture.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/storage.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/logging.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/setting.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/extensions.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/architecture.md`
  - 结果：通过；本轮 10 个新文件的 frontmatter、链接与代码块校验全部通过。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；接近阈值前的最后一批文案收口后站点仍可构建，仅保留既有大 chunk warning。

### 当前恢复点：RP-042

- 用户明确要求在当前阈值内循环推进，并允许使用 subagent 降低主线程上下文压力；因此本轮在主线程保留实现与验证，把热点识别委派给 3 个 explorer。
- 接受的 subagent 结论主要有三类：
  - 入口页最划算的改法是统一 reader-facing 骨架，而不是继续保留治理说明或负向 framing。
  - 若站内已有栏目页与专题页，GitHub blob README 不应继续作为公开文档主导航。
  - `GFramework.Game` / `Game.Abstractions` / `Godot` 等 README 仍有 `ai-libs`、`family`、`seam`、`ReadMe.md` 等对外不友好的措辞，适合在同一轮里收口。
- 基于这些结论，本轮连续落地 3 组低风险切片：入口页 reader-facing 改写、README / Godot 页去内部口吻、剩余 GitHub blob README 外链改回站内入口。

### 当前决策（RP-042）

- 继续保持 critical path 本地执行，不让 subagent 直接改文件；subagent 只负责热点排序与问题归类。
- stop condition 继续沿用 `origin/main` + `50` changed files；当前工作树相对 baseline 的 tracked diff 已到 `36` files / `500` changed lines，意味着还能再做一小批，但应先提交当前稳定批次。
- 当前批次不扩展到新栏目、新导航层或大段内容重写，只做 reader-facing 入口、术语和站内导航连通性收口。

### 当前验证（RP-042）

- README 链接校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Game/README.md GFramework.Game.Abstractions/README.md GFramework.Godot/README.md GFramework.Cqrs.Abstractions/README.md GFramework.Ecs.Arch/README.md`
  - 结果：通过；本轮 5 个 README 的 reader-facing 改写后链接目标有效。
- 教程 / Godot 页面校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/index.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/scene.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md`
  - 结果：通过；受影响页面的 frontmatter、链接与代码块校验通过。
- 入口与专题页校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/index.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/index.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/api-reference/index.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/getting-started/quick-start.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/cqrs.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/scene.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/ui.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/ecs/arch.md`
  - 结果：通过；入口页和相关推荐入口改写后页面校验通过。
- 栏目级校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators`
  - 结果：通过；抽象层与生成器栏目改回站内入口后栏目校验通过。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮 3 组 reader-facing 文档批次后站点仍可构建，仅保留既有大 chunk warning。

### 当前恢复点：RP-041

- 通过 `$gframework-batch-boot 50` 重新进入后，先按仓库规则读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、`ai-plan/public/README.md` 与 active topic tracking / trace，并继续使用显式 `--git-dir` / `--work-tree` 绑定确认当前分支仍为 `docs/sdk-update-documentation`。
- 使用显式 Git 绑定确认最新 baseline 为 `origin/main` `617e0bf`（`2026-04-26 12:17:15 +08:00`），当前 committed branch diff vs baseline 为 `0` files，因此本轮继续选择低风险、reader-facing 文档切片。
- 本轮收敛出的 3 组切片分别是：`installation.md` 的选包矩阵与旧版 Godot 提示、公开 README 的 XML 阅读入口去治理化，以及 `config-system` / 基础教程入口中的维护者口吻改写。

### 当前决策（RP-041）

- 不扩展到导航结构或新专题页，只在现有入口上修正 reader-facing 采用路径与表述一致性。
- 对公开 README 中的 XML 阅读入口，统一改成“代表类型 + 阅读重点”，不再暴露覆盖计数、日期或 `已覆盖` 这类治理字段。
- stop condition 继续沿用 `origin/main` + `50` changed files；本轮工作树相对 baseline 的 tracked diff 为 `9` files / `191` changed lines，仍明显低于阈值。

### 当前验证（RP-041）

- README 链接校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Core.Abstractions/README.md GFramework.Game.Abstractions/README.md GFramework.Game.SourceGenerators/README.md GFramework.Ecs.Arch.Abstractions/README.md`
  - 结果：通过；本轮 4 个 README 的 reader-facing 改写后链接目标有效。
- 入门栏目校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/getting-started`
  - 结果：通过；`installation.md` 更新后 `getting-started` 栏目 frontmatter、链接与代码块校验通过。
- 配置系统页校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/config-system.md`
  - 结果：通过；工具形态建议改写后页面校验通过。
- 基础教程栏目校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/basic`
  - 结果：通过；入口页阅读路径改写后栏目校验通过。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮文档批次后站点仍可构建，仅保留既有大 chunk warning。

## 2026-04-26

### 当前恢复点：RP-040

- 本轮继续从 `$gframework-pr-review` 恢复，沿用显式 `--git-dir` / `--work-tree` 绑定确认当前分支仍为 `docs/sdk-update-documentation`，并重新抓取 PR `#292` 的 latest-head review。
- 使用 `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json` 抓取后确认：PR `#292` 最新 reviewed commit 为 `d3d62cf4541063c46458f88eea0f5acd1b4503f9`，failed checks 为 `0`，测试汇总仍为 `2156 passed`；剩余 `2` 条 CodeRabbit open thread 都落在 `tools/gframework-config-tool/README.md`。
- 本地逐项复核后确认：缺少 `docs/zh-CN` 链接的评论已经过期，因为 README 当前已有 `Documentation` 章节；仍成立的是补最小接入路径，以及统一 `stable config-schema subset` / `current schema subset` 术语。

### 当前决策（RP-040）

- 接受当前 latest-head review 中仍然成立的两项 README 收口：新增 `Quick Start` 最小接入路径，并统一校验支持范围术语。
- 不对已经过期的“缺少中文文档入口链接”线程做额外扩展，只在本地结果里保留“已验证为 stale”的结论，等待后续 PR review 刷新反映最新状态。
- 继续遵守 active topic 的恢复要求，在同一轮里同步更新 tracking / trace，并对直接受影响的工具模块执行最小验证。

### 当前验证（RP-040）

- PR review 抓取：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过；PR `#292` 处于 `OPEN`，latest head review 还有 `2` 条 CodeRabbit open thread，failed checks 为 `0`，测试汇总为 `2156 passed`。
- 工具 README 收口：
  - `tools/gframework-config-tool/README.md`
  - 结果：已新增 `Quick Start` 段落，并把 `Validation Coverage` 术语统一为 `current schema subset`。
- 工具测试：
  - `bun run test`（工作目录：`tools/gframework-config-tool/`）
  - 结果：通过；`122` 个测试全部通过。
- 工具打包：
  - `bun run package:vsix`（工作目录：`tools/gframework-config-tool/`）
  - 结果：通过；成功生成 `gframework-config-tool-0.0.3.vsix`，确认工具模块可完成最小打包验证。

## 2026-04-25

### 当前恢复点：RP-039

- 本轮从 `$gframework-pr-review` 重新进入，先按仓库规则读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、`ai-plan/public/README.md` 与 active topic tracking / trace，并继续使用显式 `--git-dir` / `--work-tree` 绑定确认当前分支为 `docs/sdk-update-documentation`。
- 使用 `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json` 抓取后确认：PR `#292` 最新 reviewed commit 为 `b96565ffa367bade30f44c2d4e8955143fbff85e`，latest head review 仅剩 `2` 条 CodeRabbit open thread，无 failed tests；唯一 failed check 为 `Title check` inconclusive，属于 PR 标题文案元数据提示。
- 本地逐项复核后，两条 review 仍成立且都属于低风险 reader-facing 修正：
  - `docs/zh-CN/source-generators/index.md` 的“共享支撑模块”段落中，句式“对读者更重要的判断是”略拗口。
  - `tools/gframework-config-tool/README.md` 缺少通往 `docs/zh-CN/game/config-tool.md` 的中文接入文档入口。

### 当前决策（RP-039）

- 接受这两条 latest-head review，并限定本轮只做文案可读性与 README 入口补链，不扩展到未被当前 review 指向的其它页面。
- `Title check` 不通过仓库文件修复；保持在本轮结果中显式记录，等待后续通过 GitHub PR 标题更新处理。
- 继续沿用 active topic 的治理要求，在同一变更里同步更新 tracking / trace，保证后续从 PR review 恢复时能直接看到最新 commit 与剩余风险。

### 当前验证（RP-039）

- PR review 抓取：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过；PR `#292` 处于 `OPEN`，latest head review 还有 `2` 条 CodeRabbit open thread，测试汇总为 `2156 passed`，无 failed tests，另有 `Title check` inconclusive。

### 当前恢复点：RP-038

- 用户明确要求从“低效的单次批次”切到“循环跑到接近阈值”，并允许通过 subagent 避免主线程上下文过长；因此本轮把批处理目标从 PR `#290` 的单点收口扩展为“覆盖整个项目功能的 reader-facing 文档补齐”。
- 先在主线程确认 critical path 仍是“选定低风险文档切片并控制 branch-size stop condition”，再委派 3 个 explorer 做只读巡检：
  - source-generator support modules / 文档失真点
  - CQRS 文档覆盖缺口
  - repo-root / tooling / meta-package surface
- 接受的 explorer 结论：
  - `CQRS` 当前不需要扩独立栏目；最小有用修复是补 `docs/zh-CN/core/cqrs.md` 对 `RequestBase`、stream command/query 与协程入口的说明。
  - source-generators 当前最有价值的是修正文档失真，并补清楚 `GFramework.SourceGenerators.Common` 与 `*.SourceGenerators.Abstractions` 的共享支撑层语义。
  - repo-root / tooling 当前最缺的是 meta-package / install surface、VS Code config tool adoption path，以及 repo-visible support module README。
- 由此收敛出 5 组连续低风险批次：
  - meta-package / 安装入口
  - config tool adoption
  - source-generators 真实契约修正
  - CQRS `Request` / stream 覆盖补齐
  - generator support module README

### 当前决策（RP-038）

- 不把 `CQRS` 从 `Core` 导航中抽成新栏目；本轮优先修正 reader-facing 覆盖缺口，而不是引入新的站点结构。
- 对 repo-visible support modules，不扩成新的 docs 栏目，而是在各目录本地补 `README.md` 说明“为什么存在、跟谁一起走、什么时候需要读这里”。
- 对 config tool，不新建顶级 `tooling/` 栏目，而是挂到 `Game` 下，保持它与 `config-system` 的采用路径一致。
- stop condition 仍按 `origin/main` 与 `50` changed files 追踪；本轮提交前工作树已触达 `18` 个文件，仍明显低于阈值。

### 当前验证（RP-038）

- 文档栏目校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/cqrs.md`
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/index.md`
  - 结果：通过；触达页 frontmatter、链接与代码块校验通过。
- README / 链接校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh README.md tools/gframework-config-tool/README.md GFramework.SourceGenerators.Common/README.md GFramework.Core.SourceGenerators.Abstractions/README.md GFramework.Godot.SourceGenerators.Abstractions/README.md`
  - 结果：通过；根 README、config tool README 与新增 support README 的链接目标有效。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；站点可构建，仅保留既有大 chunk warning。
- 元包编译：
  - `dotnet build GFramework.csproj -c Release`
  - 结果：通过；输出 `357` 条既有 analyzer warnings，无新增错误。

### 当前恢复点：RP-037

- 通过 `$gframework-batch-boot 50` 重新进入后，先按技能要求读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、`ai-plan/public/README.md`、active topic tracking / trace，并确认当前 worktree 仍映射到 `documentation-full-coverage-governance`。
- 使用显式 `git --git-dir=<repo>/.git/worktrees/GFramework-update-documentation --work-tree=<worktree-root>` 绑定确认 baseline 采用 `origin/main` `79934f7`（`2026-04-25 16:15:55 +08:00`）；branch diff vs baseline 当前为 `0` files，工作树仅有本批次改动。
- 全量运行 `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN` 后确认 reader-facing 文档仅剩 `docs/zh-CN/contributing.md:631` 这一条既有代码块语言警告，适合作为单文件低风险批次收口。
- 将 `docs/zh-CN/contributing.md` 的 Mermaid 示例从“真实嵌套 triple-backtick”改写为“外层 fenced block + 内层转义围栏文本”，避免当前 `validate-code-blocks.sh` 的简单 `^```` 状态机把内层 closing fence 误判成缺语言标记的新 opening fence。

### 当前决策（RP-037）

- 当前批处理目标收敛为“消除 `contributing.md` 中最后一个剩余代码块语言 warning”，不再继续扩展到别的栏目页。
- 继续沿用 `origin/main` 作为 branch-size stop condition 基线，主指标仍是 `50` changed files；本批次只新增 1 个工作树文件，远未逼近阈值。
- 对这类“文档中展示 Markdown 代码块”的示例，优先选择仓库现有校验脚本可稳定识别的转义文本写法，而不是依赖嵌套 fenced block 的解析细节。

### 当前验证（RP-037）

- 文档单文件校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/contributing.md`
  - 结果：通过；`docs/zh-CN/contributing.md` 不再报告第 `631` 行代码块语言警告。
- 文档全量校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN`
  - 结果：通过；当前 `docs/zh-CN` 的 frontmatter、链接与代码块校验全部通过。
- 站点构建：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；站点仍可构建，仅保留既有大 chunk warning。

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
