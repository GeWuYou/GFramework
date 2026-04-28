# Documentation Full Coverage Governance Validation History (RP-041 to RP-048)

## 2026-04-28 / RP-048 active tracking follow-up

### PR review 抓取

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#299` 处于 `OPEN`，latest head review 有 `1` 条 `CodeRabbit` open thread 与 `1` 条 nitpick，`Greptile` / `Gemini Code Assist` 当前无 open thread，测试汇总为 `2159 passed`，仅剩 `Title check` inconclusive。

### 站点构建

- `bun run build`（工作目录：`docs/`）
  - 结果：通过；active tracking 收口与时间线归档瘦身后站点仍可构建，仅保留既有大 chunk warning。

## 2026-04-28 / RP-048

### PR review 抓取

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#299` 处于 `OPEN`，latest head review 有 `3` 条 `CodeRabbit` open thread，`Greptile` / `Gemini Code Assist` 当前无 open thread，测试汇总为 `2159 passed`，仅剩 `Title check` inconclusive。

### 页面校验

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions/index.md`
  - 结果：通过。
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/lifecycle.md`
  - 结果：通过。
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/state-machine-tutorial.md`
  - 结果：通过。
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/resource-management.md`
  - 结果：通过。
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/save-system.md`
  - 结果：通过。
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/pause-system.md`
  - 结果：通过。
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/large-project-organization.md`
  - 结果：通过。
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/troubleshooting.md`
  - 结果：通过。

### 站点构建

- `bun run build`（工作目录：`docs/`）
  - 结果：通过；站点仍可构建，仅保留既有大 chunk warning。

## 2026-04-27 / RP-045 到 RP-047

### 第 1 批次 reader-facing 入口收口

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/api-reference/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/setting.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions/index.md`
- `bun run build`（工作目录：`docs/`）
  - 结果：通过；第 1 批次 5 个入口页校验与站点构建通过。

### 第 2 批次去内部参考路径暴露

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/ui.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/godot-project-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/get-node-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/bind-node-signal-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/auto-register-exported-collections-generator.md`
- `bun run build`（工作目录：`docs/`）
  - 结果：通过；第 2 批次 6 个公开页面校验与站点构建通过。

### 第 3、4 批次旧入口/旧文档对比收口

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/query.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/command.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/context.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/scene.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/ui.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/priority-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/lifecycle.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/scene.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/context-aware-generator.md`
- `bun run build`（工作目录：`docs/`）
  - 结果：通过；第 3、4 批次 10 个公开页面校验与站点构建通过。

### PR review follow-up 与 README / landing 补充验证

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Game.SourceGenerators/README.md GFramework.Game/README.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/godot-integration.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/extensions.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Core.Abstractions/README.md GFramework.Game.Abstractions/README.md GFramework.Game.SourceGenerators/README.md GFramework.Ecs.Arch.Abstractions/README.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/getting-started`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/config-system.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/basic`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Game/README.md GFramework.Game.Abstractions/README.md GFramework.Godot/README.md GFramework.Cqrs.Abstractions/README.md GFramework.Ecs.Arch/README.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/scene.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/api-reference/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/getting-started/quick-start.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/cqrs.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/scene.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/ui.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/ecs/arch.md`
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
- `bun run build`（工作目录：`docs/`）
  - 结果：通过；相关 README、landing page 与 review follow-up 页面校验通过，站点构建通过。

## 2026-04-25 到 2026-04-26 / carry-over

### 代表性验证命令

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Core/README.md GFramework.Ecs.Arch/README.md GFramework.Game/README.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/troubleshooting.md`
- `dotnet build GFramework.csproj -c Release`
- `bun run build`（工作目录：`docs/`）
- `bun run test`（工作目录：`tools/gframework-config-tool/`）
- `bun run package:vsix`（工作目录：`tools/gframework-config-tool/`）
  - 结果：通过；更早一轮的 README、导航、教程、排障、工具 README 与元包校验结果仍由这些命令覆盖，详细结论可结合 trace 中的 RP-041 到 RP-047 条目与 Git 历史继续追溯。
