# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-026`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
  - 收口 PR `#282` 的 latest-head review follow-up，保持 active tracking / trace 只承载当前恢复入口
  - 保持 `README.md` 与 `docs/**` 公开页面只承载读者需要的采用信息，不再混入 XML inventory、覆盖基线、恢复点或治理批次说明
  - 继续按 `$gframework-batch-boot 75` 的 `origin/main` 分支 diff 阈值做小批量文档治理，优先处理低风险 metadata 缺口、坏链与 Markdown 结构问题
  - 保持 `Game` persistence docs surface 与当前 `README`、源码、`PersistenceTests` 使用同一套 owner / adoption path 叙述
  - 保持 `GFramework.Godot.SourceGenerators/README.md` 与 `docs/zh-CN/tutorials/godot-integration.md` 在生命周期接法上的一致性
  - 保持 active tracking / trace 只承载当前恢复入口，把阶段细节留在 `archive/`

## 当前状态摘要

- `Core`、`Ecs.Arch`、`Cqrs`、`Game`、`Godot` 五个模块族当前都已有 README / landing / topic / API 参考层级的已验证入口。
- `2026-04-24` 使用 `$gframework-pr-review` 抓取 PR `#282` 后，确认 latest head commit
  `982249151ecf8acdff3e62e664034bf95dfacd75` 当前仍有 `3` 条 CodeRabbit 与 `1` 条 Greptile open thread；4 条建议均已在本地复核并纳入当前恢复点。
- 本轮 PR follow-up 仅收口仍然成立的 review 项：
  - 将过长的 active tracking / trace 瘦身，并把 `RP-023` 到 `RP-025` 的细节迁入 `archive/`
  - 将 `docs/zh-CN/core/context.md` 的标题本地化为中文读者友好的写法
  - 统一 `docs/zh-CN/troubleshooting.md` 中 `/zh-CN/core/architecture` 与 `/zh-CN/faq` 的 `.md` 链接写法
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

- `2026-04-24` `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#282` 处于 `OPEN`，latest head commit 有 `3` 条 CodeRabbit 与 `1` 条 Greptile open thread，测试汇总为 `2156 passed`，仅剩 `Title check` 的 inconclusive PR 元数据提示。
- `2026-04-24` `rg -n --pcre2 '\\]\\(/zh-CN/[^)]+(?<!\\.md)\\)' docs/zh-CN/troubleshooting.md`
  - 结果：当前无命中；`/zh-CN/core/architecture` 与 `/zh-CN/faq` 已统一补成显式 `.md` 链接。
- `2026-04-24` `bun run build`（工作目录：`docs/`）
  - 结果：通过；文档标题本地化、站内链接修正与 `ai-plan` 归档瘦身落地后站点仍可正常构建，仅保留既有大 chunk warning。

## 下一步

1. 若继续执行 `$gframework-batch-boot 75`，优先处理 `docs/zh-CN/index.md` 与 `tutorials/basic/01-07.md` 这 `8`
   个“已有 frontmatter 但缺 `title` / `description`”的 metadata 缺口。
2. 推送当前 follow-up commit 后，再次执行 `$gframework-pr-review`，确认 PR `#282` 的 unresolved review threads 是否已在新 head commit 上消失。
3. 若后续分支继续调整 `Game` persistence runtime、README 或公共 API，优先复核 `docs/zh-CN/game/data.md`、
   `storage.md`、`serialization.md`、`setting.md` 与 landing page 是否仍保持同一套职责边界。
4. 若后续分支继续调整 `Godot` generator 接法，优先复核 `GFramework.Godot.SourceGenerators/README.md`、
   `docs/zh-CN/tutorials/godot-integration.md` 与相关专题页是否仍保持一致。
