# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免历史上的阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-016`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
  - 保持 `Game` family 的 persistence docs surface 与当前 `README`、源码、`PersistenceTests` 使用同一套 owner / adoption path 叙述
  - 将 `data.md`、`storage.md`、`serialization.md`、`setting.md` 视为 `Game` family 当前需要一起巡检的核心页面集，而不是分散的旧 API 手册页
  - 重点观察 `DataRepository` / `UnifiedSettingsDataRepository` / `SaveRepository<TSaveData>`、`FileStorage` / `ScopedStorage` 与 `SettingsModel<TRepository>` 的职责边界是否再次回漂
  - 在 `Game` runtime public API 或 README 再次变动前，优先做 targeted 巡检，不重复改写已稳定的 landing page

## 当前状态摘要

- 已归档的 `documentation-governance-and-refresh` 仅保留为历史证据，不再作为默认 `boot` 入口
- `2026-04-23` 的 `Game` persistence docs wave 新增结论：
  - `docs/zh-CN/game/storage.md` 之前仍停留在旧版“通用存储 API 手册”写法，没有反映 `FileStorage` / `ScopedStorage` 与上层 repository 的分工，也没有强调当前同步 API 只是异步阻塞包装
  - `docs/zh-CN/game/data.md` 之前缺少 `DataRepository`、`UnifiedSettingsDataRepository` 与 `SaveRepository<TSaveData>` 三层分工，以及 `PersistenceTests` 已覆盖的备份 / 批量事件 / 存档迁移语义
  - `docs/zh-CN/game/serialization.md` 之前仍沿用“业务层手工 Serialize 再写回 storage”的旧示例，没有反映当前 `FileStorage` 已直接复用注入的 `ISerializer`
  - `docs/zh-CN/game/setting.md` 虽然已回到 `ISettingsModel` / `RegisterApplicator(...)` 口径，但缺少 frontmatter，且还没有和新的 `Game` persistence docs surface 使用同一套结构
- `2026-04-23` 的 `Game` persistence docs wave 治理动作：
  - 重写 `docs/zh-CN/game/storage.md`，将其改为 `FileStorage` / `ScopedStorage` 的职责、路径语义、作用域复用与 repository 边界页
  - 重写 `docs/zh-CN/game/data.md`，补齐 `DataRepository`、`UnifiedSettingsDataRepository`、`SaveRepository<TSaveData>` 与 `DataRepositoryOptions` / `SaveConfiguration` 的当前契约
  - 重写 `docs/zh-CN/game/serialization.md`，收敛到 `JsonSerializer` 的配置生命周期、运行时类型序列化与和 storage / repository 的分工
  - 重写 `docs/zh-CN/game/setting.md`，使其与 `SettingsModel<TRepository>`、`SettingsSystem`、迁移缓存和统一设置仓库的当前实现保持一致
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
- 本轮已确认的 `Godot` family 恢复摘要：
  - `docs/zh-CN/godot/index.md`、`architecture.md`、`scene.md`、`ui.md`、`storage.md`、`setting.md`、`signal.md`、`extensions.md`、`logging.md` 与 `docs/zh-CN/tutorials/godot-integration.md` 是当前需要保留的核心页面集
  - `GFramework.Godot.SourceGenerators` 继续作为 `[GetNode]`、`[BindNodeSignal]`、`AutoLoads` 与 `InputActions` 的 owner；`GFramework.Godot.SourceGenerators.Abstractions` 仍按内部支撑模块处理
  - `Godot` Scene / UI 采用边界已经稳定：当前没有 `GodotSceneRouter` 或 `GodotUiRouter`；`GodotSceneFactory` 在 provider 缺失时会回退到 `SceneBehaviorFactory`，而 `GodotUiFactory` 仍要求 `IUiPageBehaviorProvider`
- `2026-04-23` 的 validation-only 巡检新增结论：
  - 根 `README.md`、`docs/zh-CN/godot/index.md`、`docs/zh-CN/tutorials/godot-integration.md`、`docs/zh-CN/source-generators/index.md` 与 `docs/zh-CN/api-reference/index.md` 当前仍保持同一套 `Godot` owner / adoption path 叙述，没有发现新的入口漂移
  - `scan_module_evidence.py Godot` 显示 `docs/zh-CN/godot/storage.md` 与 `setting.md` 仍属于 `Godot` runtime docs surface，应并入 active topic 的最小恢复摘要，避免后续 `boot` 漏掉当前 landing page 的关键入口
- `2026-04-23` 的子页巡检新增结论：
  - `docs/zh-CN/godot/storage.md` 之前仍停留在旧版 API 手册写法，缺少 frontmatter、`IStorage` / repository 边界和 `GodotYamlConfigLoader` 的分流说明
  - `docs/zh-CN/godot/setting.md` 之前仍把 `GodotAudioSettings` / `GodotGraphicsSettings` 描述成直接持有设置数据对象的旧构造方式，没有反映当前 `ISettingsModel` + `RegisterApplicator(...)` 接法
- `2026-04-23` 的交叉链接巡检新增结论：
  - `GFramework.Godot/README.md` 仍停留在旧版简略描述，缺少当前包关系、子系统地图、最小接入路径与 `docs/zh-CN` 入口
  - `GFramework.Godot.SourceGenerators/README.md` 虽有示例，但没有覆盖 `AutoScene`、`AutoUiPage`、`AutoRegisterExportedCollections` 等当前生成器分组，也没有把运行时 / 生成器边界说清
  - `docs/zh-CN/api-reference/index.md` 的 `Godot` 行此前只把生成器入口指向泛化的 `source-generators/index.md`，不利于从 API 参考直接落到 `Godot` 专题页
- `2026-04-23` 的后续入口巡检新增结论：
  - 根 `README.md` 的模块地图仍把 `GFramework.Godot.SourceGenerators` 写成“Godot 场景专用源码生成器”，范围过窄，不符合当前 `project.godot` 元数据、节点注入、信号绑定、Scene / UI 包装与导出集合注册的真实职责
  - `docs/zh-CN/source-generators/index.md` 的选包说明此前只提到 `AutoLoad / Input Action` 与节点 / 信号样板，没有把 Scene / UI 包装与导出集合注册辅助纳入同一入口
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
  - 将 `Godot` family 的最小恢复摘要迁回 active topic，保留核心页面集、生成器 owner、Scene / UI 真实边界与归档指针，避免长期治理默认恢复路径继续依赖 archive 明细
  - 重写 `GFramework.Godot/README.md`，补齐包定位、相邻包关系、子系统地图、最小接入路径与站内文档入口，并明确它不拥有生成器职责
  - 重写 `GFramework.Godot.SourceGenerators/README.md`，按 `project.godot` 元数据、节点注入 / 信号绑定、行为包装与批量注册四组能力重建入口
  - 更新 `docs/zh-CN/api-reference/index.md` 的 `Godot` 模块映射，使 API 参考能直接落到 `Godot` 专用生成器专题页，而不是仅回到总览页
  - 修正根 `README.md` 中 `GFramework.Godot.SourceGenerators` 的模块描述，使其与当前生成器职责边界一致
  - 扩充 `docs/zh-CN/source-generators/index.md` 的 Godot 选包说明，把 Scene / UI 包装与导出集合注册辅助纳入入口摘要
  - 重写 `docs/zh-CN/godot/storage.md`，补齐 frontmatter、`GodotFileStorage` 的路径语义、repository 分工与 `GodotYamlConfigLoader` 分流边界
  - 重写 `docs/zh-CN/godot/setting.md`，改回当前 `ISettingsModel` / `RegisterApplicator(...)` 口径，并补上 `LocalizationMap` fallback 与 `CoreGrid` 注册示例
  - `2026-04-23` 再次通过 `$gframework-boot` 恢复当前 worktree 后，按 `Godot` docs surface 执行 validation-only 巡检，确认 `GFramework.Godot/README.md`、`docs/zh-CN/godot/index.md`、`storage.md`、`setting.md`、`docs/zh-CN/source-generators/index.md`、`docs/zh-CN/api-reference/index.md` 与 `docs/zh-CN/tutorials/godot-integration.md` 仍保持一致的 owner / adoption path 叙述，没有出现自 `RP-014` 之后的新漂移

## Inventory（第一版）

| 模块族 | 当前状态 | 当前证据 | 下一动作 |
| --- | --- | --- | --- |
| `Core` / `Core.Abstractions` | `README / landing / 类型族级 XML inventory 已收口，成员级审计待补齐` | 根 README、模块 README、`docs/zh-CN/core/**`、`docs/zh-CN/abstractions/core-abstractions.md` 已对齐当前目录与类型族基线 | 进入巡检；如有新 API 变更，再追加成员级 XML 审计 |
| `Cqrs` / `Cqrs.Abstractions` / `Cqrs.SourceGenerators` | `README / landing / generator topic / 类型族级 XML inventory 已收口，成员级审计待补齐` | `GFramework.Cqrs/README.md`、`GFramework.Cqrs.Abstractions/README.md`、`GFramework.Cqrs.SourceGenerators/README.md`、`docs/zh-CN/core/cqrs.md`、`docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`、`docs/zh-CN/api-reference/index.md` 已对齐当前源码与测试 | 转入巡检；下一波切到 `Game` family 的 XML / 教程链路审计 |
| `Game` / `Game.Abstractions` / `Game.SourceGenerators` | `README / landing / abstractions / persistence topic pages / 类型族级 XML inventory 已收口，成员级审计待补齐` | `GFramework.Game/README.md`、`GFramework.Game.Abstractions/README.md`、`GFramework.Game.SourceGenerators/README.md`、`docs/zh-CN/game/index.md`、`docs/zh-CN/abstractions/game-abstractions.md`、`docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md` 已对齐当前源码、README 与 `PersistenceTests` | 转入巡检；优先观察后续分支是否再次把 `Game` persistence docs 写回旧 API 手册口径 |
| `Godot` / `Godot.SourceGenerators` | `README / 生成器 README / landing / topic / tutorial / API reference 入口已重新对齐，成员级 XML 审计不在本轮范围` | `GFramework.Godot/README.md`、`GFramework.Godot.SourceGenerators/README.md`、`docs/zh-CN/godot/index.md`、`architecture.md`、`scene.md`、`ui.md`、`storage.md`、`setting.md`、`signal.md`、`extensions.md`、`logging.md`、`docs/zh-CN/tutorials/godot-integration.md` | 进入巡检周期，优先观察后续分支是否再次把 README / API 入口写回过时边界 |
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
- `Godot` family 的详细治理历史仍保留在 archive，active topic 只回填了最小恢复摘要
  - 缓解措施：active topic 记录核心页面集、owner、运行时边界与 archive 指针；只有在需要阶段级历史时再读取归档材料
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
  - 结果：通过；在重写 `docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md` 后再次验证通过，仅保留既有 VitePress 大 chunk warning，无构建失败
- 最新 `Game` persistence docs wave 结论：
  - `2026-04-23` 基于 `GFramework.Game` 源码、`GFramework.Game/README.md`、`JsonSerializerTests`、`SettingsModelTests` 与 `PersistenceTests`
  - 结果：通过；`docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md` 当前已回到同一套 `Game` runtime 持久化采用路径，不再沿用旧版 API 手册叙述
- 最新稳定性巡检结论：
  - `2026-04-23` 重新执行 `Godot` docs surface 巡检
  - 结果：通过；根入口链路保持稳定，并额外发现 `docs/zh-CN/godot/storage.md`、`setting.md` 两页存在旧版叙述残留，当前已按源码口径完成最小修复
- 最新 validation-only 巡检结论：
  - `2026-04-23` 通过 `$gframework-boot` 恢复后重新执行 `Godot` docs surface 巡检
  - 结果：通过；`README / landing / topic / tutorial / API reference` 当前仍保持同一套 `Godot` owner / adoption path 叙述，本轮无需新增文档补丁
- 最新恢复治理结论：
  - `2026-04-23` 重新读取 `ai-plan/public/archive/documentation-governance-and-refresh/**`
  - 结果：通过；确认 `Godot` family 适合把最小恢复摘要迁回 active topic，但不需要把整段归档历史重新放回默认 `boot` 路径
- 已完成的针对性校验：
  - `2026-04-23` `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Godot`：通过
  - `2026-04-23` `python3 -B -c "from pathlib import Path; compile(Path('.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py').read_text(encoding='utf-8'), '.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py', 'exec')"`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/ecs/index.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions/ecs-arch-abstractions.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Godot/README.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh GFramework.Godot/README.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Godot.SourceGenerators/README.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh GFramework.Godot.SourceGenerators/README.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/api-reference/index.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh README.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh README.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/index.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/index.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/godot-integration.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/storage.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/setting.md`：通过
  - `2026-04-23` `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Godot`：通过（boot 后复核）
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/storage.md`：通过（boot 后复核）
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/setting.md`：通过（boot 后复核）
  - `2026-04-23` `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Game`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/data.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/storage.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/serialization.md`：通过
  - `2026-04-23` `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/setting.md`：通过
  - `2026-04-23` `cd docs && bun run build`：通过（本轮 `Game` persistence docs wave 复核；仅保留既有 VitePress 大 chunk warning）

## 下一步

1. 若后续分支继续调整 `GFramework.Game` 的 persistence runtime、README 或公共 API，优先复核 `docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md` 与 landing page 是否仍保持同一套职责边界
2. 当 `Godot` / `Game` family 再次出现交叉入口漂移时，沿用当前 README -> landing -> topic page -> API reference 的最小修复顺序
3. 仅在需要阶段级细节时再读取 `documentation-governance-and-refresh` archive，而不是把 archive 重新当作默认 `boot` 入口
