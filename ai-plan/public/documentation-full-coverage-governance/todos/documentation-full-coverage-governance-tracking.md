# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-019`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
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
- `2026-04-23` `bun run build`（工作目录：`docs/`）
  - 结果：通过；仅保留既有 VitePress 大 chunk warning，无构建失败。

## 下一步

1. 提交并推送本地对 `docs/zh-CN/godot/setting.md` 与 `docs/zh-CN/godot/storage.md` 的 Markdown 泛型写法修正，
   然后重新抓取 PR `#272` 确认 Greptile open thread 是否已在新 head commit 上消失。
2. 如果 PR `#272` 的 `Title check` 仍需要消除，到 GitHub 上把标题改成更具体的文档治理描述。
3. 若后续分支继续调整 `Game` persistence runtime、README 或公共 API，优先复核 `docs/zh-CN/game/data.md`、
   `storage.md`、`serialization.md`、`setting.md` 与 landing page 是否仍保持同一套职责边界。
4. 若后续分支继续调整 `Godot` generator 接法，优先复核 `GFramework.Godot.SourceGenerators/README.md`、
   `docs/zh-CN/tutorials/godot-integration.md` 与相关专题页是否仍保持一致。
