# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-048`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
  - 按 `$gframework-pr-review` 复核 PR `#299` 的 latest-head review，并收口当前仍成立的 active tracking 问题：补齐 `RP-048` 下一步，以及把 `RP-045` 到 `RP-047` 的逐命令历史迁入归档
- 本轮通过 `$gframework-batch-boot 50` 重新进入后确认 `HEAD == origin/main`，当前已提交 branch diff 为 `0` files / `0` lines，因此可以从新的低风险文档批次重新累计阈值
- 当前已完成 4 个低风险批次，并在本轮额外完成 1 次 review-driven 收口：入口页 reader-facing 标题统一、内部参考路径去暴露，以及 `Core` / `Game` / `Godot` / `source-generators` 多个页面中“旧文档式对比”提示的直接契约化改写
- 第 2 个已提交批次结束时，branch diff 相对 `origin/main` 为 `13` files / `124` lines；本轮最后一批提交后仍会明显低于 `50` 文件 stop condition
- 本轮已确认 `Architecture` 只暴露 `OnInitialize()`，`AbstractArchitecture` 通过 `InstallModules()` 暴露模块注册入口，而组件级 `OnInit()` 仍然是当前有效生命周期
- 当前建议在本轮停止：active tracking 收口完成并推送后，只需重新抓取 PR `#299`，确认剩余项是否只剩 GitHub 侧 `Title check` 元数据提示

## 当前状态摘要

- `Core`、`Ecs.Arch`、`Cqrs`、`Game`、`Godot` 五个模块族当前都已有 README / landing / topic / API 参考层级的已验证入口。
- `2026-04-28` 已重新抓取 PR `#299` 并复核 latest-head review：remote 当前只剩 `1` 条 `CodeRabbit` open thread 与 `1` 条 nitpick，且都指向 active tracking 文档；`Greptile` / `Gemini Code Assist` 当前无 open thread，测试汇总为 `2159 passed`，`Title check` 仍是 PR 元数据问题。
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
- PR `#299` 在 remote latest-head review 中仍显示 `1` 条 open thread 与 `1` 条 nitpick，直到本轮 `ai-plan` 改动推送后才会反映关闭状态；`Title check` 仍需要直接修改 GitHub 上的 PR 标题。

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
- 时间线归档（`RP-041` 到 `RP-048`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-041-to-rp-048-2026-04-28.md`
- 验证历史归档（`RP-041` 到 `RP-048`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-rp-041-to-rp-048-2026-04-28.md`

## 最新验证

- `2026-04-28` `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#299` 处于 `OPEN`，latest head review 有 `1` 条 `CodeRabbit` open thread 与 `1` 条 nitpick，`Greptile` / `Gemini Code Assist` 当前无 open thread，测试汇总为 `2159 passed`，仅剩 `Title check` inconclusive。
- `2026-04-28` `bun run build`（工作目录：`docs/`）
  - 结果：通过；active tracking 收口与时间线归档瘦身后站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-27` 到 `2026-04-28` 的详细逐命令验证历史已迁入：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-rp-041-to-rp-048-2026-04-28.md`

## 下一步

1. 推送本轮提交后，优先重新抓取 `$gframework-pr-review`，确认 PR `#299` 仅剩标题元数据提示或已全部清空。
2. 若 remote review 清空后仍继续文档治理，优先人工复核尚未触达的 `Game` persistence、Godot runtime 细页与少量残余迁移边界表述，而不是继续按关键词机械扩批。
3. 若后续分支继续调整 `Game` persistence runtime、README 或公共 API，优先复核 `docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md` 与 landing page 是否仍保持同一套职责边界。
4. 若后续分支继续调整 `Godot` generator 接法，优先复核 `GFramework.Godot.SourceGenerators/README.md`、`docs/zh-CN/tutorials/godot-integration.md` 与相关专题页是否仍保持一致。
