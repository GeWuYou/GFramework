# Documentation Full Coverage Governance Trace History Through RP-016

以下内容从 active trace 中迁出，用于保留 `DOCUMENTATION-FULL-COVERAGE-GOV-RP-001` 到
`DOCUMENTATION-FULL-COVERAGE-GOV-RP-016` 的阶段时间线、关键决策与主要验证结果。默认 `boot` 只需要读取
active trace 中的最新恢复点；若需要追溯旧阶段的执行顺序，再回到本归档文件。

## 2026-04-22

### `RP-001`

- 新建 active topic `documentation-full-coverage-governance`。
- 在 `ai-plan/public/README.md` 中将当前 worktree 绑定到该 topic。
- 盘点可消费模块与内部支撑模块的边界，作为后续 README / docs 治理基线。

### `RP-002`

- 完成 `Core` / `Core.Abstractions` README、landing page 与类型族级 XML inventory 的第一轮收口。
- 运行 `docs` 站点构建与局部文档校验，结果通过。

### `RP-003`

- 完成 `Ecs.Arch` / `Ecs.Arch.Abstractions` 文档刷新。
- 确认 `UseArch(...)` 必须早于 `Initialize()` 的采用顺序，并将该约束写回文档。
- 运行 `docs` 站点构建，结果通过。

### `RP-004`

- 完成 `Cqrs` family landing、generator topic 与 API 参考入口刷新。
- 为 `GFramework.Cqrs` 与 `GFramework.Cqrs.SourceGenerators` 缺失的内部类型补齐 XML 注释。
- `GFramework.Cqrs.SourceGenerators` Release build 通过。
- `GFramework.Cqrs` Release build 仍受环境级 fallback package folder 问题阻塞，记录为已知非回归风险。

## 2026-04-23

### `RP-005`

- 完成 `Game` family README、landing page、抽象页与类型族级 XML inventory 刷新。
- 文档校验与 `docs` 站点构建通过。

### `RP-006`

- 更新 `AGENTS.md` 的 WSL Git 回退顺序：
  - 优先显式 `--git-dir` / `--work-tree` 绑定。
  - `git.exe` 仅在当前会话可执行时作为 fallback。
- `docs` 站点构建通过。

### `RP-007`

- 完成 `Game` family validation-only 巡检。
- 确认 `config-system.md`、`scene.md`、`ui.md` 与 `source-generators/index.md` 当前没有新的采用漂移。

### `RP-008` 到 `RP-013`

- 消化 PR #271 的 review follow-up，修正 `gframework-pr-review` 脚本与 skill 中的 Git 策略。
- 将 `Godot` family 的核心恢复摘要迁回 active topic。
- 重写 `GFramework.Godot/README.md` 与 `GFramework.Godot.SourceGenerators/README.md`。
- 更新根 `README.md`、`docs/zh-CN/source-generators/index.md` 与 `docs/zh-CN/api-reference/index.md` 的
  `Godot` 入口与 owner 描述。
- 针对 `Godot` docs surface 执行 validation-only 巡检，确认当前 landing / topic / tutorial / API reference
  与 README 保持一致。

### `RP-014`

- 重写 `docs/zh-CN/godot/storage.md` 与 `docs/zh-CN/godot/setting.md`。
- 运行 `scan_module_evidence.py Godot`、相关文档校验与 `docs` 站点构建，结果通过。

### `RP-015`

- 再次执行 `Godot` docs surface validation-only 巡检。
- 确认 `storage.md`、`setting.md`、landing、README、tutorial 与 API reference 没有新的回漂。

### `RP-016`

- 重写 `docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md`。
- 运行 `scan_module_evidence.py Game`、相关文档校验与 `docs` 站点构建，结果通过。
- 当前 `Game` persistence docs surface 已回到与源码、README 和 `PersistenceTests` 一致的责任边界叙述。

## 主要验证汇总

- `cd docs && bun run build`
  - 多轮执行通过；仅保留既有 VitePress 大 chunk warning，无构建失败。
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh ...`
  - 针对本 topic 涉及的 landing / topic / abstractions 页面多轮执行通过。
- `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Godot`
  - 通过。
- `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Game`
  - 通过。

## 归档关联

- 状态归档：`ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- 早期详细验证历史：`ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
