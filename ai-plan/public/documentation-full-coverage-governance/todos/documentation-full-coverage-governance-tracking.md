# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-047`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
  - 继续以最新 `origin/main`（`7cfdd2c`，`2026-04-27 16:59:57 +08:00`）作为 baseline，当前批处理 stop condition 仍是 branch diff vs baseline 接近 `50` changed files
- 本轮通过 `$gframework-batch-boot 50` 重新进入后确认 `HEAD == origin/main`，当前已提交 branch diff 为 `0` files / `0` lines，因此可以从新的低风险文档批次重新累计阈值
- 当前已完成 4 个低风险批次：入口页 reader-facing 标题统一、内部参考路径去暴露，以及 `Core` / `Game` / `Godot` / `source-generators` 多个页面中“旧文档式对比”提示的直接契约化改写
- 第 2 个已提交批次结束时，branch diff 相对 `origin/main` 为 `13` files / `124` lines；本轮最后一批提交后仍会明显低于 `50` 文件 stop condition
- 当前建议在本轮停止：剩余命中更多是运行时迁移边界或模块 README 之间的正常交叉引用，不再属于同一类可机械批处理的低风险 reader-facing 收口

## 当前状态摘要

- `Core`、`Ecs.Arch`、`Cqrs`、`Game`、`Godot` 五个模块族当前都已有 README / landing / topic / API 参考层级的已验证入口。
- `2026-04-25` 已重新抓取 PR `#290` 并确认：latest reviewed commit 为 `54b8e5770af9ab3c8a86a396ffa4794fe4bb5181`，open thread 聚焦在 `docs/.vitepress/config.mts` 的侧栏重复 / 标签不一致，以及 `GFramework.Core`、`GFramework.Ecs.Arch`、`GFramework.Game` README 的 reader-facing 表格残留治理字段。
- `2026-04-25` `docs/.vitepress/config.mts` 已保留 `source-generators` 栏目自有子页导航，但不再让 `api-reference` 侧栏重复跳回 `core`、`game`、`godot`、`ecs` 等独立栏目入口。
- `2026-04-25` `GFramework.Core/README.md`、`GFramework.Ecs.Arch/README.md`、`GFramework.Game/README.md` 当前把 XML 阅读表统一收敛为“代表类型 + 阅读重点”，不再暴露日期、覆盖计数或 `已覆盖` 这类治理式字段。
- `2026-04-25` `docs/zh-CN/contributing.md` 中最后一个嵌套 fenced 示例已改写为转义围栏文本，现有 `validate-code-blocks.sh` 不再报告第 `631` 行警告。
- `2026-04-25` 全量 `docs/zh-CN` 验证已无剩余代码块语言警告；前一轮触达的 `tutorials`、`best-practices`、`troubleshooting`、`godot/resource` 等栏目结果保持有效。
- `2026-04-25` `docs/zh-CN/source-generators/index.md` 已按 PR `#292` review 调整“共享支撑模块”段落句式，避免“对读者更重要的判断是”这类拗口表达。
- `2026-04-25` `tools/gframework-config-tool/README.md` 已新增 `Documentation` 章节，直接链接到 `docs/zh-CN/game/config-tool.md` 与 `config-system.md`，让工具 README 能回到完整中文接入文档。
- `2026-04-26` `tools/gframework-config-tool/README.md` 已补 `Quick Start`，把安装扩展、配置 `configPath` / `schemasPath`、打开 Explorer、先跑校验、再进入表单 / 批量编辑的最小接入路径串起来，并把 `Validation Coverage` 的 `stable config-schema subset` 统一为 `current schema subset`。
- `2026-04-27` `docs/zh-CN/getting-started/installation.md` 已补齐当前公开选包矩阵，新增 `Core.Abstractions`、`Game.Abstractions`、`Ecs.Arch`、`Ecs.Arch.Abstractions` 的 reader-facing 安装说明，并把 `Godot` 常见问题里的旧版 `>= 4.5` 提示收敛到当前 `4.6.2` 基线。
- `2026-04-27` `GFramework.Core.Abstractions/README.md`、`GFramework.Game.Abstractions/README.md`、`GFramework.Game.SourceGenerators/README.md`、`GFramework.Ecs.Arch.Abstractions/README.md` 当前都已把 XML 阅读入口改写为“代表类型 + 阅读重点”，不再暴露覆盖计数、日期或 `已覆盖` 这类治理字段。
- `2026-04-27` `docs/zh-CN/game/config-system.md` 与 `docs/zh-CN/tutorials/basic/index.md` 已把维护者 / 指挥式措辞改成中性的采用建议与阅读入口，避免公开页面继续暴露内部决策口吻。
- `2026-04-27` `docs/zh-CN/getting-started/index.md`、`core/index.md`、`game/index.md`、`api-reference/index.md`、`source-generators/index.md` 已统一收敛为“适用场景 / 起步路线 / 继续阅读”式 reader-facing 入口，不再把 GitHub blob README 或治理说明当作主导航。
- `2026-04-27` 新一轮 batch boot 第 1 批次已进一步收口 `docs/zh-CN/source-generators/index.md`、`game/index.md`、`api-reference/index.md`、`godot/setting.md`、`abstractions/index.md` 的标题与导航口吻，去掉 `family`、自我指涉标题、原始 `README.md` 文件名提示和“先理解…”式栏目标题。
- `2026-04-27` 新一轮 batch boot 第 2 批次已把 `docs/zh-CN/game/ui.md`、`godot/signal.md`、`source-generators/godot-project-generator.md`、`get-node-generator.md`、`bind-node-signal-generator.md`、`auto-register-exported-collections-generator.md` 中直接暴露 `ai-libs/CoreGrid` 的路径型说明改成项目侧常见实现说明。
- `2026-04-27` 新一轮 batch boot 第 3、4 批次已把 `core/query.md`、`core/command.md`、`core/context.md`、`core/lifecycle.md`、`game/scene.md`、`game/ui.md`、`godot/ui.md`、`godot/scene.md`、`source-generators/priority-generator.md`、`context-aware-generator.md` 中依赖“旧文档/旧入口”对比的句式改成直接陈述当前契约与推荐入口。
- `2026-04-27` `GFramework.Game/README.md`、`GFramework.Game.Abstractions/README.md`、`GFramework.Godot/README.md`、`GFramework.Cqrs.Abstractions/README.md`、`GFramework.Ecs.Arch/README.md` 已收口 `ai-libs`、`family`、`seam`、`ReadMe.md` 等内部化或文件名式表述。
- `2026-04-27` `docs/zh-CN` 当前已清空所有指向 `github.com/GeWuYou/GFramework/blob/main/.../README.md` 的公开外链，相关入口统一回到站内栏目页、专题页或 API 导航。
- `2026-04-27` `docs/zh-CN/tutorials/godot-integration.md`、`game/setting.md`、`game/serialization.md`、`godot/index.md`、`godot/architecture.md`、`godot/storage.md`、`godot/logging.md`、`godot/setting.md`、`godot/extensions.md`、`core/architecture.md` 已把 `旧文档` / `ai-libs` / `.Wait()` / `family` 这类维护与内部语气改写成当前采用说明。
- `2026-04-27` 已重新抓取 PR `#296` 并逐条复核 latest-head review：`GFramework.Game.SourceGenerators/README.md` 的 XML 阅读表已改成语义标签，`GFramework.Game/README.md` 已删除重复的 `storage.md` 入口，`docs/zh-CN/tutorials/godot-integration.md` 与 `docs/zh-CN/godot/extensions.md` 已收口仍成立的 reader-facing 措辞问题。
- `2026-04-25` 当前批次已补齐 meta-package / 安装面：`GFramework.csproj` 不再保留占位描述，`README.md`、`docs/zh-CN/index.md`、`docs/zh-CN/getting-started/installation.md` 当前明确说明聚合元包只聚合 `Core` + `Game`，并把安装入口更新到当前 `net8.0/net9.0/net10.0` 与 Godot `4.6.2` 基线。
- `2026-04-25` `docs/zh-CN/game/config-tool.md` 已新增为 reader-facing 工具页，`docs/zh-CN/game/index.md`、`config-system.md`、`docs/.vitepress/config.mts` 与 `tools/gframework-config-tool/README.md` 当前把 VS Code 配置工具纳入 `Game` 配置工作流入口。
- `2026-04-25` source-generators 栏目已修正 4 处真实契约问题：`GetNode` 显式路径 / `Lookup` 语义、枚举生成器实际开关、`Context Get` 集合注入边界，以及 `GFramework.SourceGenerators.Common` / `*.SourceGenerators.Abstractions` 的共享支撑层说明。
- `2026-04-25` `GFramework.SourceGenerators.Common/README.md`、`GFramework.Core.SourceGenerators.Abstractions/README.md`、`GFramework.Godot.SourceGenerators.Abstractions/README.md` 已补齐本地目录说明，根 README 的“内部支撑模块”表可以直接跳到对应目录说明。
- `Game` persistence docs surface 当前以 `docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md`
  作为最小巡检集合；若后续 README、runtime public API 或 `PersistenceTests` 变动，应优先复核这一组页面。
- `Godot` runtime 与 generator 入口当前以 `GFramework.Godot/README.md`、
  `GFramework.Godot.SourceGenerators/README.md`、`docs/zh-CN/godot/index.md`、
  `docs/zh-CN/source-generators/index.md`、`docs/zh-CN/tutorials/godot-integration.md` 维持统一 owner / adoption path。
- `2026-04-23` 到 `2026-04-24` 的批次细节、验证日志与旧恢复建议已迁入：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-rp-023-to-rp-025-2026-04-24.md`

## 当前风险

- 当前 `Core` / `Core.Abstractions`、`Ecs.Arch`、`Cqrs`、`Game` 的 XML 治理证据仍主要来自类型与入口级阅读，不等于成员级契约全审计；这类治理状态只应保留在 `ai-plan/**`，不应再回流到公开文档。
- `GFramework.Cqrs` 在当前 WSL / dotnet 环境下仍会读取失效的 fallback package folder，并在标准 build 中触发
  `MSB4276` / `MSB4018`；这是已知环境阻塞，不属于本轮文档回归。
- 当前 WSL 会话里 `git.exe` 可解析但不能执行，应继续使用显式 `--git-dir` / `--work-tree` 绑定作为默认 Git 策略。
- `dotnet build GFramework.csproj -c Release` 当前仍会输出仓库既有 analyzer warnings（如 `MA0158`、`MA0051`、`MA0004`）；本轮仅修改文档与 package metadata，不扩展到 warning 清理。
- 当前 batch boot 已从 `origin/main` 零 diff 状态重新起步并完成 4 个低风险文案批次；剩余命中更偏向是否保留迁移说明的编辑判断，不再适合继续按同一批处理模式机械推进。

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

- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/query.md`
  - 结果：通过；Core query 页去旧文档对比表述后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/command.md`
  - 结果：通过；Core command 页改成直接陈述当前输入契约后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/context.md`
  - 结果：通过；Core context 页去旧总线入口对比表述后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/scene.md`
  - 结果：通过；Game scene 页改成直接提示当前语义边界后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`
  - 结果：通过；Godot UI 页默认语义与入口说明收口后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/priority-generator.md`
  - 结果：通过；Priority 生成器页改成直接陈述当前 API 形态后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/lifecycle.md`
  - 结果：通过；Core lifecycle 页旧入口提示收口后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/scene.md`
  - 结果：通过；Godot scene 页默认入口说明收口后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/context-aware-generator.md`
  - 结果：通过；ContextAware 生成器页路径差异说明收口后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮 batch boot 第 3、4 批次的 10 个公开页面 reader-facing 收口后站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/ui.md`
  - 结果：通过；Game UI 页去 `ai-libs/` 路径化说明后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md`
  - 结果：通过；Godot signal 页改成项目侧示例口吻后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/godot-project-generator.md`
  - 结果：通过；Godot project 生成器页去内部参考项目路径后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/get-node-generator.md`
  - 结果：通过；GetNode 生成器页改成通用项目侧用法说明后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/bind-node-signal-generator.md`
  - 结果：通过；BindNodeSignal 生成器页去具体内部项目名后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/auto-register-exported-collections-generator.md`
  - 结果：通过；导出集合自动注册页改成通用入口描述后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮 batch boot 第 2 批次的 6 个公开页面 reader-facing 收口后站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/index.md`
  - 结果：通过；`source-generators` 入口页标题收口后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/index.md`
  - 结果：通过；`Game` 入口页描述调整后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/api-reference/index.md`
  - 结果：通过；API 导航页 reader-facing 标题调整后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/setting.md`
  - 结果：通过；Godot 设置页继续阅读标题收口后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions/index.md`
  - 结果：通过；抽象层入口页去文件名式提示后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮 batch boot 第 1 批次的 5 个入口页 reader-facing 收口后站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Game.SourceGenerators/README.md GFramework.Game/README.md`
  - 结果：通过；本轮 2 个 README 的 reader-facing 表格与导航去重调整后链接目标有效。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/godot-integration.md`
  - 结果：通过；Godot 集成教程的措辞收口后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/extensions.md`
  - 结果：通过；Godot 扩展页去自我指涉表述后页面 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮 PR `#296` review 收口后的站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-27` `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#296` 处于 `OPEN`，latest head review 共有 `4` 条 open thread，其中 `3` 条文档问题与 `1` 条措辞 nitpick 在本地复核后仍成立；测试汇总为 `2156 passed`，仅剩 `Title check` inconclusive。
- `2026-04-25` `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过；PR `#290` 处于 `OPEN`，latest head commit `54b8e5770af9ab3c8a86a396ffa4794fe4bb5181` 有 `2` 条 open thread（CodeRabbit `1`、Greptile `1`），测试汇总为 `2156 passed`，无 failed checks。
- `2026-04-25` `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过；PR `#292` 处于 `OPEN`，latest head commit `b96565ffa367bade30f44c2d4e8955143fbff85e` 有 `2` 条 CodeRabbit open thread，测试汇总为 `2156 passed`，无 failed tests；另有 `Title check` inconclusive，属于 PR 标题元数据问题，不是仓库文件阻塞。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Core/README.md GFramework.Ecs.Arch/README.md GFramework.Game/README.md`
  - 结果：通过；本轮 3 个模块 README 调整后链接目标仍然有效。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Core.Abstractions/README.md GFramework.Game.Abstractions/README.md GFramework.Game.SourceGenerators/README.md GFramework.Ecs.Arch.Abstractions/README.md`
  - 结果：通过；4 个公开模块 README 的 reader-facing 改写后链接目标仍然有效。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/getting-started`
  - 结果：通过；`installation.md` 更新后 `getting-started` 栏目的 frontmatter、链接与代码块校验均通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/config-system.md`
  - 结果：通过；`config-system.md` 的工具形态建议改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/basic`
  - 结果：通过；基础教程入口的阅读路径改写后栏目校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Game/README.md GFramework.Game.Abstractions/README.md GFramework.Godot/README.md GFramework.Cqrs.Abstractions/README.md GFramework.Ecs.Arch/README.md`
  - 结果：通过；本轮 5 个模块 README 的 reader-facing 术语与入口改写后链接目标有效。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/index.md`
  - 结果：通过；教程页受众表述改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`
  - 结果：通过；Godot UI 页的接法示例与 reader-facing 术语改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/scene.md`
  - 结果：通过；Godot 场景页的接法示例与 reader-facing 术语改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md`
  - 结果：通过；信号页切回站内生成器入口后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions`
  - 结果：通过；3 个抽象层页改回站内入口后栏目校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators`
  - 结果：通过；生成器栏目及受影响专题页改回站内入口后栏目校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/index.md`
  - 结果：通过；Core 入口页 reader-facing 改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/index.md`
  - 结果：通过；Game 入口页 reader-facing 改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/api-reference/index.md`
  - 结果：通过；API 入口页导航改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/getting-started/quick-start.md`
  - 结果：通过；快速开始页切回站内安装入口后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/cqrs.md`
  - 结果：通过；CQRS 页继续阅读入口改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/scene.md`
  - 结果：通过；Game 场景页相关推荐改回站内入口后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/ui.md`
  - 结果：通过；Game UI 页相关推荐改回站内入口后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/ecs/arch.md`
  - 结果：通过；ECS Arch 页入口改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/godot-integration.md`
  - 结果：通过；Godot 集成教程的接线口吻改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/setting.md`
  - 结果：通过；设置系统页初始化语义改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/serialization.md`
  - 结果：通过；序列化页生命周期说明改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/index.md`
  - 结果：通过；Godot landing page 的采用说明改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/architecture.md`
  - 结果：通过；Godot 架构页异步初始化口吻改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/storage.md`
  - 结果：通过；Godot 存储页示例口吻改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/logging.md`
  - 结果：通过；Godot 日志页 provider 接线说明改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/setting.md`
  - 结果：通过；Godot 设置页 applicator 接线口吻改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/extensions.md`
  - 结果：通过；Godot 扩展页边界说明改写后页面校验通过。
- `2026-04-27` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/architecture.md`
  - 结果：通过；Core 架构页旧初始化入口改写后页面校验通过。
- `2026-04-27` `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮 README、安装页与公开文案改写后站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-25` `bun run build`（工作目录：`docs/`）
  - 结果：通过；移除 `api-reference` 侧栏重复项并统一 `source-generators` 标签后站点仍可正常构建，仅保留既有大 chunk warning。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh README.md GFramework.Core/README.md GFramework.Core.Abstractions/README.md GFramework.Game/README.md GFramework.Game.Abstractions/README.md GFramework.Game.SourceGenerators/README.md GFramework.Ecs.Arch/README.md GFramework.Ecs.Arch.Abstractions/README.md`
  - 结果：通过；根 README 与本轮触达的模块 README 链接目标有效。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials`
  - 结果：通过；本轮新增触达的 10 个教程页与其余教程页 frontmatter、链接、代码块校验均通过。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/best-practices`
  - 结果：通过；`index.md` 与 `architecture-patterns.md` 的代码块标记补齐后栏目验证通过。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/troubleshooting.md`
  - 结果：通过；错误输出与完整错误信息块补齐为 `text` 后页面验证通过。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/contributing.md`
  - 结果：通过；嵌套 fenced 示例已改写为转义围栏文本，`docs/zh-CN/contributing.md` 不再保留代码块语言警告。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN`
  - 结果：通过；当前 `docs/zh-CN` 全量 frontmatter、链接与代码块校验均通过，不再保留既有代码块语言警告。
- `2026-04-25` `bun run build`（工作目录：`docs/`）
  - 结果：通过；`contributing.md` 的 Mermaid 示例改写后站点仍可正常构建，仅保留既有大 chunk warning。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators`
  - 结果：通过；`source-generators` 栏目触达页 frontmatter、链接与代码块校验均通过。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game`
  - 结果：通过；新增 `config-tool.md` 与 `Game` 栏目触达页 frontmatter、链接与代码块校验均通过。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/cqrs.md`
  - 结果：通过；`CQRS` 页补充 `Request` / stream 变体与协程入口后链接和代码块校验通过。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/index.md`
  - 结果：通过；首页 hero actions 与 feature 文案更新后 frontmatter、代码块校验通过。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh README.md tools/gframework-config-tool/README.md GFramework.SourceGenerators.Common/README.md GFramework.Core.SourceGenerators.Abstractions/README.md GFramework.Godot.SourceGenerators.Abstractions/README.md`
  - 结果：通过；根 README、config tool README 与新增 3 个 support README 的链接目标有效。
- `2026-04-25` `dotnet build GFramework.csproj -c Release`
  - 结果：通过；元包工程与聚合依赖可编译，输出 `357` 条既有 analyzer warnings，无新增错误。
- `2026-04-25` `bun run build`（工作目录：`docs/`）
  - 结果：通过；meta-package / config tool / source-generators / CQRS 多批次文档更新后站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/resource.md`
  - 结果：通过；`Godot` 资源页剩余 bare opening fence 已补齐语言标记。
- `2026-04-25` `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮导航补齐、README reader-facing 改写与教程 / 排障 / 资源页代码块语言标记更新后站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Core/README.md`
  - 结果：通过；README 链接目标有效。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Core.SourceGenerators/README.md`
  - 结果：通过；README 链接目标有效。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Cqrs.SourceGenerators/README.md`
  - 结果：通过；README 链接目标有效。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Ecs.Arch/README.md`
  - 结果：通过；README 链接目标有效。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Game.SourceGenerators/README.md`
  - 结果：通过；README 链接目标有效。
- `2026-04-25` `rg -n '\\[[^\\]]*(README\\.md|\\.md|\\.md/|/zh-CN/[^\\]]*)\\]\\([^)]*\\)' GFramework.Core/README.md GFramework.Core.SourceGenerators/README.md GFramework.Cqrs.SourceGenerators/README.md GFramework.Ecs.Arch/README.md GFramework.Game.SourceGenerators/README.md`
  - 结果：无命中；本轮 5 个 README 已无可见路径式 / 文件名式 Markdown 链接标签残留。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core`
  - 结果：通过；`Core` 栏目本轮触达页面的 frontmatter、链接与代码块校验均通过。
- `2026-04-25` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/basic`
  - 结果：通过；基础教程栏目本轮触达页面的 frontmatter、链接与代码块校验均通过。
- `2026-04-25` `bun run build`（工作目录：`docs/`）
  - 结果：通过；README 标签修正与 `Core` / 基础教程代码块语言标记补齐后站点仍可构建，仅保留既有大 chunk warning。
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
- `2026-04-25` `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Core`
  - 结果：通过；技能仍能正常解析 `Core` 模块证据面，说明新增的 reader-facing 输出约束未破坏模块扫描主流程。
- `2026-04-25` `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#287` 处于 `OPEN`，latest head commit `8209d7a29f35d969fca6258b9817da9b33a203a3` 仅剩
    `1` 条 Greptile open thread，无 failed checks，测试汇总为 `2156 passed`。
- `2026-04-25` `bun run build`（工作目录：`docs/`）
  - 结果：通过；`docs/zh-CN/api-reference/index.md` 的站内入口标签统一为语义化写法后站点仍可正常构建，仅保留既有大 chunk warning。
- `2026-04-26` `bun run test`（工作目录：`tools/gframework-config-tool/`）
  - 结果：通过；`122` 个测试全部通过，说明 README 收口没有影响该工具现有测试面。
- `2026-04-26` `bun run package:vsix`（工作目录：`tools/gframework-config-tool/`）
  - 结果：通过；成功生成 `gframework-config-tool-0.0.3.vsix`，满足本轮工具模块的最小 build validation。

## 下一步

1. 提交当前接近阈值的稳定批次后，优先重新抓取 `$gframework-pr-review` 或在新一轮里按 `46 / 50` 的 branch diff 重新评估是否还适合继续扩批。
2. 若后续还要继续文档治理，优先复核尚未触达的 `Game` persistence、Godot runtime 细页与少量残余 `ai-libs` 口吻，而不是继续扩大同一轮 review 面。
3. 若后续分支继续调整 `Game` persistence runtime、README 或公共 API，优先复核 `docs/zh-CN/game/data.md`、
   `storage.md`、`serialization.md`、`setting.md` 与 landing page 是否仍保持同一套职责边界。
4. 若后续分支继续调整 `Godot` generator 接法，优先复核 `GFramework.Godot.SourceGenerators/README.md`、
   `docs/zh-CN/tutorials/godot-integration.md` 与相关专题页是否仍保持一致。
