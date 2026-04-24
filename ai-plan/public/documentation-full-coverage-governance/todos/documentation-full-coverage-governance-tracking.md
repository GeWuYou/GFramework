# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-031`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
  - 继续按 `$gframework-batch-boot 75` 的 `origin/main` 分支 diff 阈值做小批量文档治理；当前 baseline 已回到 `origin/main`，本批只继续处理新的低风险 reader-facing 缺口
  - 保持 `README.md` 与 `docs/**` 公开页面只承载读者需要的采用信息，不再混入 XML inventory、覆盖基线、恢复点或治理批次说明
  - 继续优先处理低风险 metadata 缺口、坏链、README 文档入口对齐、Reader-friendly 链接标签与 Markdown 结构问题，避免跨模块语义改写
  - 保持 `Game` persistence docs surface 与当前 `README`、源码、`PersistenceTests` 使用同一套 owner / adoption path 叙述
  - 保持 `GFramework.Godot.SourceGenerators/README.md` 与 `docs/zh-CN/tutorials/godot-integration.md` 在生命周期接法上的一致性
  - 保持 active tracking / trace 只承载当前恢复入口，把阶段细节留在 `archive/`

## 当前状态摘要

- `Core`、`Ecs.Arch`、`Cqrs`、`Game`、`Godot` 五个模块族当前都已有 README / landing / topic / API 参考层级的已验证入口。
- `2026-04-24` 当前本地 `docs/sdk-update-documentation` 与 `origin/main` 同步到 `4c2994e`，相对 `$gframework-batch-boot 75` 的 baseline 当前为 `0 / 75` 个 changed files；后续批次可以继续，但仍应保持小 write set。
- `2026-04-24` 使用 `$gframework-pr-review` 抓取当前 PR `#284` 后，确认 latest head commit
  `77540c07f0890cc05b10a849722c87b8bed8f561` 仍有 `3` 条 CodeRabbit 与 `1` 条 Greptile open thread；本轮仅继续收口本地复核后仍成立的 reader-facing 文档入口与 active tracking 精简问题。
- 本轮 PR follow-up 仅收口仍然成立的 review 项：
  - 将过长的 active tracking / trace 瘦身，并把 `RP-023` 到 `RP-025` 的细节迁入 `archive/`
  - 将 `docs/zh-CN/core/context.md` 的标题本地化为中文读者友好的写法
  - 统一 `docs/zh-CN/troubleshooting.md` 中 `/zh-CN/core/architecture` 与 `/zh-CN/faq` 的 `.md` 链接写法
- 本批次将根 `README.md` 中两个仍直接暴露文件路径的内部支撑模块入口改为 reader-friendly 链接标签，避免目录表格继续把路径本身当成入口名称。
- 本批次继续将 `Core`、`Game`、`Source Generators` 和三篇 `Abstractions` 落地页的纯英文 `title` / H1 改为中文读者友好的入口标题，减少首页与侧边栏扫描成本。
- 本批次继续将 `core/architecture.md`、`command.md`、`events.md`、`logging.md`、`property.md`、`query.md` 的纯英文 `title` / H1 本地化为中英对照入口标题，保持 Core 子栏目扫描体验一致。
- 当前批次完成后，纯英文 `title` 扫描只剩 `docs/zh-CN/core/cqrs.md` 的 `CQRS` 与 `docs/zh-CN/index.md` 的 `GFramework`；它们分别属于通用缩写与品牌名，不再作为本轮优先本地化对象。
- 本批次补齐了 `docs/zh-CN/index.md` 的 `description`，以及 `docs/zh-CN/tutorials/basic/01-07.md` 的 `title` / `description`，让首页和基础教程章节页拥有完整 frontmatter metadata。
- 本批次统一将教程、最佳实践、Core、Godot 页面里缺显式扩展名的站内 Markdown 链接补齐为 `.md` 或 `index.md`，避免目录链接、绝对路径旧写法与 VitePress 构建解析分叉。
- 本批次把模块 README、仓库根 README、`docs/index.md` 及多组中文落地页里直接暴露文件路径的入口调整为读者友好的可点击标签，同时补齐语言落地页 metadata 与 README 指向。
- `Game` persistence docs surface 当前以 `docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md`
  作为最小巡检集合；若后续 README、runtime public API 或 `PersistenceTests` 变动，应优先复核这一组页面。
- `Godot` runtime 与 generator 入口当前以 `GFramework.Godot/README.md`、
  `GFramework.Godot.SourceGenerators/README.md`、`docs/zh-CN/godot/index.md`、
  `docs/zh-CN/source-generators/index.md`、`docs/zh-CN/tutorials/godot-integration.md` 维持统一 owner / adoption path。
- `2026-04-23` 到 `2026-04-24` 的批次细节、验证日志与旧恢复建议已迁入：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-rp-023-to-rp-025-2026-04-24.md`

## 当前风险

- 当前 `Core` / `Core.Abstractions`、`Ecs.Arch`、`Cqrs`、`Game` 的 XML 治理证据仍主要来自类型与入口级阅读，不等于成员级契约全审计；这类治理状态只应保留在 `ai-plan/**`，不应再暴露到公开文档。
- `GFramework.Cqrs` 在当前 WSL / dotnet 环境下仍会读取失效的 fallback package folder，并在标准 build 中触发
  `MSB4276` / `MSB4018`；这是已知环境阻塞，不属于本轮文档回归。
- 当前 WSL 会话里 `git.exe` 可解析但不能执行，应继续使用显式 `--git-dir` / `--work-tree` 绑定作为默认 Git 策略。
- PR `#282` 的 `Title check` 仍可能提示标题过泛；这是 GitHub PR 元数据问题，不属于本地文件缺陷。

## 归档指针

- 详细验证历史（`RP-001` 到 `RP-007`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- 阶段状态归档（`RP-001` 到 `RP-016`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- 阶段状态归档（`RP-023` 到 `RP-025`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-rp-023-to-rp-025-2026-04-24.md`
- 时间线归档（`RP-001` 到 `RP-016`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`
- 时间线归档（`RP-023` 到 `RP-025`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-023-to-rp-025-2026-04-24.md`

## 最新验证

- `2026-04-24` `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#284` 处于 `OPEN`，latest head commit `77540c07f0890cc05b10a849722c87b8bed8f561` 有 `3` 条 CodeRabbit 与 `1` 条 Greptile open thread，测试汇总为 `2156 passed`，仅剩 `Title check` 的 inconclusive PR 元数据提示。
- `2026-04-24` `rg -n --pcre2 '\\]\\(/zh-CN/[^)]+(?<!\\.md)\\)' docs/zh-CN/troubleshooting.md`
  - 结果：当前无命中；`/zh-CN/core/architecture` 与 `/zh-CN/faq` 已统一补成显式 `.md` 链接。
- `2026-04-24` `bun run build`（工作目录：`docs/`）
  - 结果：通过；文档标题本地化、站内链接修正与 `ai-plan` 归档瘦身落地后站点仍可正常构建，仅保留既有大 chunk warning。
- `2026-04-24` `python3 - <<'PY' ...`（扫描 `docs/zh-CN/**/*.md` frontmatter 是否缺 `title` / `description`）
  - 结果：通过；当前带 frontmatter 的 `docs/zh-CN` 页面已无 `title` / `description` 缺口。
- `2026-04-24` `bun run build`（工作目录：`docs/`）
  - 结果：通过；首页与基础教程 metadata 补齐后站点仍可正常构建，仅保留既有大 chunk warning。
- `2026-04-24` `python3 - <<'PY' ...`（扫描 `docs/zh-CN/**/*.md` 中以 `./`、`../`、`/zh-CN/` 开头且未带扩展名的 Markdown 链接）
  - 结果：通过；当前 `docs/zh-CN` 站内 Markdown 链接已无缺失扩展名的命中。
- `2026-04-24` `bun run build`（工作目录：`docs/`）
  - 结果：通过；`25` 个页面的站内链接补齐为显式 `.md` / `index.md` 后站点仍可正常构建，仅保留既有大 chunk warning。
- `2026-04-24` `bun run build`（工作目录：`docs/`）
  - 结果：通过；模块 README、中文落地页 reader-facing 文档入口对齐，以及 `docs/index.md` metadata 调整后站点仍可正常构建，仅保留既有大 chunk warning。
- `2026-04-24` `python3 - <<'PY' ...`（扫描 `docs/zh-CN/**/*.md` 中纯英文 `title`）
  - 结果：通过；经过三轮标题本地化后，仅剩 `CQRS` 与 `GFramework` 两个品牌/缩写型标题。
- `2026-04-24` `bun run build`（工作目录：`docs/`）
  - 结果：通过；根 `README.md` reader-friendly 链接标签修正与 `docs/zh-CN` 多页标题本地化落地后站点仍可正常构建，仅保留既有大 chunk warning。

## 下一步

1. 当前基线已回到 `origin/main`，本轮变更仍是小 write set；后续若继续执行 `$gframework-batch-boot 75`，仍优先选择 `5` 到 `10` 个文件以内的小批次。
2. 若继续处理 reader-facing 文档问题，优先筛查剩余 README / landing page 中是否还有路径式链接标签或不必要的内部治理措辞；纯英文标题方面仅剩品牌名与缩写，不再是当前高优先级切片。
3. 若后续分支继续调整 `Game` persistence runtime、README 或公共 API，优先复核 `docs/zh-CN/game/data.md`、
   `storage.md`、`serialization.md`、`setting.md` 与 landing page 是否仍保持同一套职责边界。
4. 若后续分支继续调整 `Godot` generator 接法，优先复核 `GFramework.Godot.SourceGenerators/README.md`、
   `docs/zh-CN/tutorials/godot-integration.md` 与相关专题页是否仍保持一致。
