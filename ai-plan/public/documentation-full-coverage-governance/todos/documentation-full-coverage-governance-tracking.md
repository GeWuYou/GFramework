# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免历史上的阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-009`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
  - 记录 `2026-04-23` 再次执行 `$gframework-pr-review` 的结论，并把 closed PR 的陈旧 open-thread 信号标记为已本地核销
  - 评估是否需要把 `Godot` family 的关键 XML inventory 摘要迁回 active topic
  - 继续抽查 README / landing page / API reference 之间的 cross-link 是否出现新的漂移

## 当前状态摘要

- 已归档的 `documentation-governance-and-refresh` 仅保留为历史证据，不再作为默认 `boot` 入口
- 本轮已消化的 PR #271 review follow-up：
  - 为 `.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py` 补齐 WSL worktree 下的显式 Linux Git 绑定，避免 `git.exe` 在当前会话触发 `Exec format error`
  - 同步更新 `.agents/skills/gframework-pr-review/SKILL.md`，改为与 `AGENTS.md` 一致的 Git 策略，并把命令示例统一到 `.agents/...` 路径
  - 为 `docs/zh-CN/source-generators/cqrs-handler-registry-generator.md` 补充 marker 类型放置与命名约定说明
  - 从 `docs/zh-CN/abstractions/ecs-arch-abstractions.md` 删除误放的 source-generator 内部模块提醒，并微调 `docs/zh-CN/ecs/index.md` 的边界说明语序
  - 为 `ai-plan/public/archive/documentation-governance-and-refresh/traces/documentation-governance-and-refresh-trace.md` 的归档验证补写结果态
  - 将 RP-001 至 RP-007 的详细验证历史迁入 `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
  - `2026-04-23` 再次执行 `$gframework-pr-review` 后，确认 PR `#271` 已关闭，latest reviewed commit `df91d3706ba9db71737e803ef2f40f4841ecbbf1` 仍显示 `2` 条 open thread，但两条都对应已在当前 HEAD 满足的 `ai-plan` 变更，属于 closed PR 上未自动收敛的陈旧线程信号
- 本轮已确认的消费属性结论：
  - `GFramework.Ecs.Arch.Abstractions`：可打包直接消费模块，需要 README 和文档入口
  - `GFramework.Core.SourceGenerators.Abstractions`：`IsPackable=false`，按内部支撑模块处理
  - `GFramework.Godot.SourceGenerators.Abstractions`：`IsPackable=false`，按内部支撑模块处理
  - `GFramework.SourceGenerators.Common`：`IsPackable=false`，按内部支撑模块处理
- 本轮已完成的治理动作：
  - 新建 `GFramework.Ecs.Arch.Abstractions/README.md`
  - 在根 `README.md` 中补齐 `GFramework.Ecs.Arch.Abstractions` 入口，并声明内部支撑模块 owner
  - 为抽象接口栏目补齐 `Ecs.Arch.Abstractions` 页面与 sidebar 入口
  - 将 `docs/zh-CN/api-reference/index.md` 重写为模块到 XML / README / 教程的阅读链路入口
  - 为 `GFramework.Core/README.md` 补齐 `Services`、`Configuration`、`Environment`、`Pool`、`Rule`、`Time` 等当前目录映射
  - 为 `GFramework.Core.Abstractions/README.md` 补齐契约族地图与 XML 阅读重点
  - 将 `docs/zh-CN/abstractions/core-abstractions.md` 从过时的接口摘录页重写为契约边界 / 包关系 / 最小接入路径页面
  - 为 `docs/zh-CN/core/index.md` 补齐 frontmatter、能力域导航和 API / XML 阅读入口
  - 为 `GFramework.Core/README.md`、`GFramework.Core.Abstractions/README.md` 补齐类型族级 XML 覆盖基线入口
  - 为 `docs/zh-CN/core/index.md`、`docs/zh-CN/abstractions/core-abstractions.md` 增加“类型族 -> XML 覆盖状态 -> 代表类型”的 inventory
  - 基于顶层目录轻量盘点确认：`Core` / `Core.Abstractions` 当前公开 / 内部类型声明都已带 XML 注释，成员级审计留待后续波次
  - 重写 `docs/zh-CN/ecs/index.md`，收敛当前 ECS family 的包边界、采用顺序和 XML inventory
  - 重写 `docs/zh-CN/ecs/arch.md`，明确 `UseArch(...)` 需早于 `Initialize()` 的真实接入时机
  - 刷新 `GFramework.Ecs.Arch/README.md`，使运行时 README 与源码 / 测试一致
  - 为 `GFramework.Ecs.Arch.Abstractions/README.md` 与 `docs/zh-CN/abstractions/ecs-arch-abstractions.md` 补齐类型族级 XML inventory
  - 重写 `docs/zh-CN/core/cqrs.md`，将其收敛为 `Cqrs` family landing，并补齐运行时 / 契约层 / 生成器的 XML inventory
  - 新建 `docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`，为 `Cqrs.SourceGenerators` 补齐站内专题入口
  - 更新 `docs/zh-CN/source-generators/index.md`、`docs/zh-CN/api-reference/index.md` 与 VitePress sidebar，使 `Cqrs` family 的 generator 入口可导航
  - 为 `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 与 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 中缺失的内部类型补齐 XML 注释，使本轮轻量 inventory 达到声明级闭环
  - 为 `GFramework.Game/README.md`、`GFramework.Game.Abstractions/README.md`、`GFramework.Game.SourceGenerators/README.md` 补齐 `Game` family 的类型族级 XML inventory
  - 为 `docs/zh-CN/game/index.md` 补齐 frontmatter，并增加 `Game` / `Game.Abstractions` / `Game.SourceGenerators` 的 XML 覆盖基线入口
  - 将 `docs/zh-CN/abstractions/game-abstractions.md` 从失真的旧接口摘录页重写为契约边界 / 包关系 / 最小接入路径页面
  - 基于顶层目录轻量盘点确认：`GFramework.Game` 为 `56/56`、`GFramework.Game.Abstractions` 为 `80/80`、`GFramework.Game.SourceGenerators` 为 `2/2`，当前公开 / 内部类型声明都已带 XML 注释
  - 更新 `AGENTS.md` 的 WSL Git 策略，将显式 `--git-dir` / `--work-tree` 绑定提升为高于 `git.exe` 的默认优先级
  - 记录当前环境偏差：本会话 `git.exe` 可解析但执行会触发 `Exec format error`，而 plain Linux `git` 会命中 worktree 路径翻译错误，需要显式仓库绑定
  - 完成 `Game` family 巡检，确认 `docs/zh-CN/game/config-system.md`、`scene.md`、`ui.md` 与 `docs/zh-CN/source-generators/index.md` 的核心采用说明、包关系与交叉引用仍与当前源码 / README 一致，没有发现需要立刻修正的回漂

## Inventory（第一版）

| 模块族 | 当前状态 | 当前证据 | 下一动作 |
| --- | --- | --- | --- |
| `Core` / `Core.Abstractions` | `README / landing / 类型族级 XML inventory 已收口，成员级审计待补齐` | 根 README、模块 README、`docs/zh-CN/core/**`、`docs/zh-CN/abstractions/core-abstractions.md` 已对齐当前目录与类型族基线 | 进入巡检；如有新 API 变更，再追加成员级 XML 审计 |
| `Cqrs` / `Cqrs.Abstractions` / `Cqrs.SourceGenerators` | `README / landing / generator topic / 类型族级 XML inventory 已收口，成员级审计待补齐` | `GFramework.Cqrs/README.md`、`GFramework.Cqrs.Abstractions/README.md`、`GFramework.Cqrs.SourceGenerators/README.md`、`docs/zh-CN/core/cqrs.md`、`docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`、`docs/zh-CN/api-reference/index.md` 已对齐当前源码与测试 | 转入巡检；下一波切到 `Game` family 的 XML / 教程链路审计 |
| `Game` / `Game.Abstractions` / `Game.SourceGenerators` | `README / landing / abstractions / 类型族级 XML inventory 已收口，成员级审计待补齐` | `GFramework.Game/README.md`、`GFramework.Game.Abstractions/README.md`、`GFramework.Game.SourceGenerators/README.md`、`docs/zh-CN/game/index.md`、`docs/zh-CN/abstractions/game-abstractions.md` 已对齐当前源码与目录基线 | 转入巡检；优先抽查 `config-system`、`scene`、`ui` 与 `source-generators` 交叉链路是否回漂 |
| `Godot` / `Godot.SourceGenerators` | `已验证` | 上一轮归档 topic 已完成核心 landing / topic / tutorial 校验 | 进入巡检周期，重点看回漂 |
| `Ecs.Arch` / `Ecs.Arch.Abstractions` | `README / landing / abstractions / 类型族级 XML inventory 已收口，成员级审计待补齐` | `GFramework.Ecs.Arch/README.md`、`GFramework.Ecs.Arch.Abstractions/README.md`、`docs/zh-CN/ecs/**`、`docs/zh-CN/abstractions/ecs-arch-abstractions.md` 已对齐当前源码与测试 | 转入巡检；后续仅在运行时公共 API 变动时补成员级 XML 细审 |
| `SourceGenerators.Common` 与 `*.SourceGenerators.Abstractions` | `已判定为内部支撑` | `*.csproj` 明确 `IsPackable=false` | 由所属模块 README 与生成器栏目说明 owner，不建独立采用页 |

## 缺口分级

- `P0`
  - 错误采用路径、错误包关系、错误 API / 生命周期语义
  - 站点导航死链、空 landing page、明显错误的模块 owner
- `P1`
  - 直接消费模块缺 README 或缺对应 docs 入口
  - README / docs 示例与源码实现不一致
  - 教程仍引用已经过时的默认接线方式
- `P2`
  - 结构重复、交叉链接不足、API 参考链路过薄
  - 站内页面存在事实正确但组织方式不利于定位的内容

## 当前风险

- 当前 `Core` / `Core.Abstractions` 只完成了类型族级 XML 基线，不等于成员级契约全审计
  - 缓解措施：后续只在共享抽象或高风险生命周期接口发生改动时补成员级细审，不在本轮扩张范围
- `Godot` family 的治理结论主要留在已归档 topic 中，active topic 当前只保留摘要
  - 缓解措施：下一恢复点优先判断是否要把关键 XML inventory 摘要迁回 active topic，避免后续 boot 仍过度依赖 archive
- 新功能分支若修改 README / docs / 公共 API 却不挂文档 topic，仍可能回漂
  - 缓解措施：将本 topic 作为长期 active topic 保留，并在后续巡检中记录回漂来源
- VitePress 页面不能直接链接到 `docs/` 目录之外的模块 `README.md`
  - 缓解措施：站内页面用模块路径文本或站内 API 入口表达，仓库级 README 仍保留仓库文件链接
- `GFramework.Cqrs` 在当前 WSL / dotnet 环境下，本地 build 仍会读取失效的 fallback package folder 配置，导致无法完成该项目的标准编译验证
  - 缓解措施：本轮先以 `GFramework.Cqrs.SourceGenerators` 编译通过和 docs site build 通过作为有效验证，并在后续环境治理或构建脚本清理时单独处理 `RestoreFallbackFolders` / 资产文件问题
- 当前 WSL 会话中 `git.exe` 虽然可解析，但不能执行
  - 缓解措施：把显式 `--git-dir` / `--work-tree` 绑定上升为仓库默认回退策略，并仅把 `git.exe` 保留为可执行时的次级 fallback

## 验证说明

- 详细验证历史已归档到 `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- 最新 PR review 结论：
  - `2026-04-23` `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过；PR `#271` 已关闭，latest reviewed commit 为 `df91d3706ba9db71737e803ef2f40f4841ecbbf1`，当前 `2` 条 open thread 都是已被本地文件满足的陈旧信号，不再构成本轮阻塞
- 最新构建结论：
  - `2026-04-23` `cd docs && bun run build`
  - 结果：通过；仅保留既有 VitePress 大 chunk warning，无构建失败
- 已完成的针对性校验：
  - `2026-04-23` `python3 -B -c "from pathlib import Path; compile(Path('.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py').read_text(encoding='utf-8'), '.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py', 'exec')"`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/ecs/index.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions/ecs-arch-abstractions.md`：通过

## 下一步

1. 评估是否需要把 `Godot` family 的关键 XML inventory 摘要迁回 active topic，避免长期治理只依赖 archive 恢复
2. 若不迁回，则在 active todo / trace 保留足够的 archive 指针，并继续抽查 README / landing page / API reference 的 cross-link 是否出现新的漂移
3. 当后续分支再修改 README / docs / 公共 API 时，回到对应 module family 追加 targeted 巡检与验证
