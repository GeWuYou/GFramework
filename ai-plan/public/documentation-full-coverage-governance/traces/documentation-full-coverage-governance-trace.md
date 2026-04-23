# Documentation Full Coverage Governance Trace

## 2026-04-23

### 当前恢复点：RP-017

- 使用 `$gframework-pr-review` 复核当前分支 PR `#272`。
- 本地核对后确认三条 review follow-up 仍然成立：
  - active tracking / trace 过长，已经偏离“恢复入口”用途
  - `docs/zh-CN/game/data.md` 缺少 `UnifiedSettingsDataRepository` 的统一文件布局示例
  - `GFramework.Godot.SourceGenerators/README.md` 缺少手写 `_Ready()` / `_ExitTree()` 时显式调用生成方法的最小样例
- 本轮执行的修复：
  - 为 `docs/zh-CN/game/data.md` 补充统一设置文件目录树示例
  - 为 `GFramework.Godot.SourceGenerators/README.md` 补充最小生命周期代码片段
  - 将 active tracking / trace 瘦身，并把 `RP-001` 到 `RP-016` 的阶段细节迁入 `archive/`

### 当前决策（RP-017）

- active recovery artifact 只保留当前恢复点、当前事实、风险、验证结果与下一步；旧阶段细节统一转移到 archive。
- `Game` persistence docs surface 继续以 `data.md`、`storage.md`、`serialization.md`、`setting.md` 作为最小巡检集合。
- `GFramework.Godot.SourceGenerators/README.md` 的生命周期接法说明应直接复用与 tutorial 一致的最小样例，避免 README 与教程再次分叉。

### 当前验证（RP-017）

- PR review 抓取：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过；PR `#272` 处于 `OPEN`，latest head commit 没有未解决 open thread，当前只剩 `Title check` 的 PR 标题提示。
- 文档校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/data.md`
  - 结果：通过。
- README 校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Godot.SourceGenerators/README.md`
  - 结果：通过。
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh GFramework.Godot.SourceGenerators/README.md`
  - 结果：通过。
- 构建校验：
  - `cd docs && bun run build`
  - 结果：通过；仅保留既有 VitePress 大 chunk warning，无构建失败。

### 归档指针

- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`

### 下一步

1. 如果 PR `#272` 的 `Title check` 仍需要处理，到 GitHub 上把标题改成更具体的文档治理描述。
2. 若后续分支继续调整 `Game` persistence runtime 或 README，优先复核 `docs/zh-CN/game/data.md`、
   `storage.md`、`serialization.md`、`setting.md` 与 landing page 是否仍保持一致。
3. 若后续分支继续调整 `Godot` generator 接法，优先复核 `GFramework.Godot.SourceGenerators/README.md`、
   `docs/zh-CN/tutorials/godot-integration.md` 与相关专题页是否仍保持一致。
