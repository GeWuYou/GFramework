# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-050`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
  - 按 `$gframework-batch-boot 50` 继续推进 `documentation-full-coverage-governance`，沿用 `origin/main` 作为 stop-condition 基线，收口 `Game` / `Godot` 细页与少量 README 中残留的 reader-facing 措辞与标签问题
- `2026-04-29` 重新进入时确认当前分支仍为 `docs/sdk-update-documentation`，但 upstream `origin/docs/sdk-update-documentation` 已不存在；因此本轮不再把旧 PR review 线程作为默认恢复入口，而是以本地 diff vs `origin/main` 为主
- `2026-04-29` 上一批已完成 11 个低风险文档文件的收口：去掉 `ai-libs`、`旧文档`、`优先看` / `先看` / `转到` 这类内部或指令式措辞，并把 README 中暴露原始路径的链接标签改成 reader-facing 标题
- `2026-04-29` 本批次继续接受 2 个 explorer 的只读结论：一个负责 `game/data.md`、`game/storage.md`、`godot/ui.md` 的热点排序，一个负责 README reader-facing 标签巡检；主线程只接受低风险措辞问题，不扩展到结构重写
- `2026-04-29` 本批次已收口 `docs/zh-CN/game/data.md`、`game/storage.md`、`godot/ui.md` 与 `GFramework.Cqrs.Abstractions/README.md`、`GFramework.SourceGenerators.Common/README.md` 的文案：把内部证据叙述、外部项目指代、命令式导流、源文件路径列表和 `IsPackable=false` 这类实现术语改成 reader-facing 说明
- 本轮仍保持在新的低风险批次窗口内：当前工作树相对 `origin/main` 为 `18` files / `225` lines，仍明显低于 `50` 文件 stop condition
- 本轮继续沿用已确认的生命周期事实：`Architecture` 只暴露 `OnInitialize()`，`AbstractArchitecture` 通过 `InstallModules()` 暴露模块注册入口，而组件级 `OnInit()` 仍然是当前有效生命周期

## 当前状态摘要

- `Core`、`Ecs.Arch`、`Cqrs`、`Game`、`Godot` 五个模块族当前都已有 README / landing / topic / API 参考层级的已验证入口。
- `2026-04-29` 新一轮 batch boot 第 2 批次已进一步收口 `docs/zh-CN/game/data.md`、`game/storage.md`、`godot/ui.md` 与 `GFramework.Cqrs.Abstractions/README.md`、`GFramework.SourceGenerators.Common/README.md`：移除 “仓库和测试确认”“真实消费者 wiring”“CoreGrid 当前的做法”“优先看” 这类内部或生硬口吻，并把 CQRS 抽象层 README 的源文件列表改成契约类型族说明。
- `2026-04-29` 新一轮 batch boot 已收口 `docs/zh-CN/godot/storage.md`、`godot/setting.md`、`godot/signal.md`、`godot/logging.md`、`godot/index.md`、`game/scene.md`、`core/index.md`、`game/config-system.md`、`ecs/arch.md` 与 `GFramework.Godot/README.md`、`tools/gframework-config-tool/README.md` 的 reader-facing 文案：移除 `ai-libs`、旧文档对比、命令式跳转和原始路径标签。
- `2026-04-29` 当前分支的 upstream `origin/docs/sdk-update-documentation` 已 gone；后续若继续批处理，应继续以 `origin/main` 作为 branch-size stop condition 的 authoritative baseline，而不是默认恢复旧 PR review 状态。
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
- 当前分支 upstream 已 gone；在重新建立 remote branch 或新的 PR 之前，不适合再把旧 PR `#299` 的 review 状态当作默认恢复信号。
- 当前 batch boot 已从 `origin/main` 零 diff 状态重新起步；本轮仍是低风险措辞收口，但下一轮若继续深入 README 子系统地图或大段采用路径重写，review 面会明显扩大。
- `GFramework.Cqrs`、`GFramework.Cqrs.SourceGenerators`、`GFramework.Game.SourceGenerators`、`GFramework.Ecs.Arch` 等 README 仍有若干源文件列表式“子系统地图”段落；这些已经接近结构级改写，不适合作为本轮剩余低风险批次继续机械推进。

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

- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/data.md`
  - 结果：通过；`game/data.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/storage.md`
  - 结果：通过；`game/storage.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`
  - 结果：通过；`godot/ui.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Cqrs.Abstractions/README.md GFramework.SourceGenerators.Common/README.md`
  - 结果：通过；本轮 2 个 README 的 reader-facing 标签调整后目标有效。
- `2026-04-29` `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮第 2 批 reader-facing 收口后站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/storage.md`
  - 结果：通过；`godot/storage.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/setting.md`
  - 结果：通过；`godot/setting.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md`
  - 结果：通过；`godot/signal.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/logging.md`
  - 结果：通过；`godot/logging.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/index.md`
  - 结果：通过；`godot/index.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/scene.md`
  - 结果：通过；`game/scene.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/config-system.md`
  - 结果：通过；`game/config-system.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/index.md`
  - 结果：通过；`core/index.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/ecs/arch.md`
  - 结果：通过；`ecs/arch.md` 的 frontmatter、链接与代码块校验通过。
- `2026-04-29` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Godot/README.md tools/gframework-config-tool/README.md`
  - 结果：通过；本轮 2 个 README 的 reader-facing 链接标签调整后目标有效。
- `2026-04-29` `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮 reader-facing 收口后站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-28` `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过；PR `#299` 处于 `OPEN`，latest head review 有 `1` 条 `CodeRabbit` open thread 与 `1` 条 nitpick，`Greptile` / `Gemini Code Assist` 当前无 open thread，测试汇总为 `2159 passed`，仅剩 `Title check` inconclusive。
- `2026-04-28` `bun run build`（工作目录：`docs/`）
  - 结果：通过；active tracking 收口与时间线归档瘦身后站点仍可构建，仅保留既有大 chunk warning。
- `2026-04-27` 到 `2026-04-28` 的详细逐命令验证历史已迁入：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-rp-041-to-rp-048-2026-04-28.md`

## 下一步

1. 提交本轮第 2 批低风险 reader-facing 文案批次，并在提交后重新计算 branch diff vs `origin/main`，确认仍明显低于 `50` 文件 stop condition。
2. 若继续下一批，优先挑选仍可“局部改句子”的页面或 README 标签问题；`Cqrs`、`Game.SourceGenerators`、`Ecs.Arch` 这类源文件列表式 README 应视作结构级专题，必要时单独开一轮更明确的写作目标。
3. 只有在重新建立 remote branch 或新的 PR 之后，再恢复 `$gframework-pr-review` 作为默认恢复入口；在此之前以本地 diff 与验证结果为准。
4. 若后续分支继续调整 `Game` persistence runtime、README 或公共 API，优先复核 `docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md` 与 landing page 是否仍保持同一套职责边界。
