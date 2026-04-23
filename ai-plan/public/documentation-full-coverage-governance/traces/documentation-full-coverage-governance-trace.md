# Documentation Full Coverage Governance Trace

## 2026-04-23

### 当前恢复点：RP-019

- 使用 `$gframework-pr-review` 重新复核当前分支 PR `#272`。
- GitHub latest-head review 当前暴露 1 条新的 Greptile open thread：
  `docs/zh-CN/godot/setting.md:75` 在 inline code 中写成
  `SettingsModel&lt;ISettingsDataRepository&gt;`。
- 本地核对当前文档渲染语义后，确认 CommonMark / VitePress 不会在 code span 内解码 HTML entity，
  该评论成立。
- 对当前 PR 已变更的 Godot 文档做同类扫描后，又在 `docs/zh-CN/godot/storage.md:102` 发现
  `SaveRepository&lt;TSaveData&gt;` 的同型问题。
- 本轮执行的修复：
  - 将 `docs/zh-CN/godot/setting.md` 的 `SettingsModel&lt;ISettingsDataRepository&gt;` 改为
    `SettingsModel<ISettingsDataRepository>`
  - 将 `docs/zh-CN/godot/storage.md` 的 `SaveRepository&lt;TSaveData&gt;` 改为
    `SaveRepository<TSaveData>`
  - 同步更新 active tracking / trace，记录该 PR review follow-up 与新的恢复点

### 当前决策（RP-019）

- PR review 结果以 GitHub latest-head open threads 为准；即便 active tracking 曾记录“无 open thread”，也必须按新抓取结果回写。
- 对 Markdown inline code 中的 C# 泛型示例，必须直接写真实的 `<T>` 语法，不能在反引号内部再写
  `&lt;` / `&gt;`，否则 VitePress 会把 entity 当作字面量展示。
- 当 latest-head review 命中某个文档表述问题时，应顺手扫描同一批 PR 已改动文档中的同类模式，避免只消掉单条 thread 却把相同渲染缺陷留在相邻页面。
- 当前本地修复完成后，下一次 GitHub 侧复核需要基于新提交/新 head commit，而不是旧的 PR review 快照。

### 当前验证（RP-019）

- PR review 抓取：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#272` 处于 `OPEN`，latest head commit 存在 1 条 Greptile open thread，定位到
    `docs/zh-CN/godot/setting.md:75` 的 inline code HTML entity 渲染问题。
- 同类模式巡检：
  - `rg -n '`[^`]*&lt;[^`]*`|`[^`]*&gt;[^`]*`' GFramework.Godot.SourceGenerators/README.md GFramework.Godot/README.md README.md docs/zh-CN/api-reference/index.md docs/zh-CN/game/data.md docs/zh-CN/game/serialization.md docs/zh-CN/game/setting.md docs/zh-CN/game/storage.md docs/zh-CN/godot/setting.md docs/zh-CN/godot/storage.md docs/zh-CN/source-generators/index.md`
  - 结果：命中 `docs/zh-CN/godot/setting.md:75` 与 `docs/zh-CN/godot/storage.md:102` 两处同类写法，均已修正。
- 构建校验：
  - `bun run build`（工作目录：`docs/`）
  - 结果：通过；仅保留既有 VitePress 大 chunk warning，无构建失败。

### 归档摘要（RP-018）

- 使用 `$gframework-pr-review` 重新复核当前分支 PR `#272`。
- latest-head review 命中 `GFramework.Godot.SourceGenerators/README.md:135` 的错误命名空间引用，并已在本地修正。
- README 校验与 `docs/` 站点构建通过，待新提交推送后回 GitHub 侧确认 open thread 消失。

### 归档指针

- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`

### 下一步

1. 提交并推送本地修正后，再次抓取 PR `#272`，确认 Greptile open thread 是否已在新 head commit 上消失。
2. 如果 PR `#272` 的 `Title check` 仍需要处理，到 GitHub 上把标题改成更具体的文档治理描述。
