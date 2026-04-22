# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免历史上的阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-006`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
  - 将当前 WSL 会话验证通过的 Git 回退策略写回 `AGENTS.md`
  - 明确显式 `--git-dir` / `--work-tree` 绑定在本仓库中的优先级高于 `git.exe`
  - 避免后续 `boot` / 恢复任务重复踩到 `git.exe` 无法执行与 plain `git` worktree 路径翻译错误

## 当前状态摘要

- 已归档的 `documentation-governance-and-refresh` 仅保留为历史证据，不再作为默认 `boot` 入口
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
  - 缓解措施：后续巡检优先抽查 `Godot` 入口页是否仍与归档结论一致，必要时把关键 XML inventory 摘要迁回 active topic
- 新功能分支若修改 README / docs / 公共 API 却不挂文档 topic，仍可能回漂
  - 缓解措施：将本 topic 作为长期 active topic 保留，并在后续巡检中记录回漂来源
- VitePress 页面不能直接链接到 `docs/` 目录之外的模块 `README.md`
  - 缓解措施：站内页面用模块路径文本或站内 API 入口表达，仓库级 README 仍保留仓库文件链接
- `GFramework.Cqrs` 在当前 WSL / dotnet 环境下，本地 build 仍会读取失效的 fallback package folder 配置，导致无法完成该项目的标准编译验证
  - 缓解措施：本轮先以 `GFramework.Cqrs.SourceGenerators` 编译通过和 docs site build 通过作为有效验证，并在后续环境治理或构建脚本清理时单独处理 `RestoreFallbackFolders` / 资产文件问题
- 当前 WSL 会话中 `git.exe` 虽然可解析，但不能执行
  - 缓解措施：把显式 `--git-dir` / `--work-tree` 绑定上升为仓库默认回退策略，并仅把 `git.exe` 保留为可执行时的次级 fallback

## 验证说明

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions/index.md`
- 结果：通过
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions/ecs-arch-abstractions.md`
- 结果：通过
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/api-reference/index.md`
- 结果：通过
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/index.md`
- 结果：通过
- 备注：`2026-04-22` 在补充 Core XML inventory 后重新验证
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions/core-abstractions.md`
- 结果：通过
- 备注：`2026-04-22` 在补充 Core.Abstractions XML inventory 后重新验证
- `cd docs && bun run build`
- 结果：通过
- 备注：`2026-04-22` 重新构建通过；仅保留 VitePress 大 chunk warning，无构建失败
- `dotnet build GFramework.Ecs.Arch.Abstractions/GFramework.Ecs.Arch.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`
- 结果：通过
- 备注：`0 Warning(s) / 0 Error(s)`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`
- 结果：通过
- 备注：`0 Warning(s) / 0 Error(s)`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/ecs/index.md`
- 结果：通过
- 备注：`2026-04-22` 在重写 ECS landing 后重新验证
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/ecs/arch.md`
- 结果：通过
- 备注：`2026-04-22` 在重写 Arch ECS 专题页后重新验证
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions/ecs-arch-abstractions.md`
- 结果：通过
- 备注：`2026-04-22` 在补充抽象页 XML inventory 后重新验证
- `cd docs && bun run build`
- 结果：通过
- 备注：`2026-04-22` 在 Ecs 波次重写后重新构建通过；仅保留 VitePress 大 chunk warning，无构建失败
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/cqrs.md`
- 结果：通过
- 备注：`2026-04-22` 在重写 `Cqrs` family landing 后重新验证
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`
- 结果：通过
- 备注：`2026-04-22` 在新增 `Cqrs.SourceGenerators` 专题页后验证通过
- `python3` 轻量 XML inventory 扫描
- 结果：通过
- 备注：`2026-04-22` 确认 `GFramework.Cqrs` 的 `Internal/` 为 `14/14`、`GFramework.Cqrs.SourceGenerators/Cqrs/` 为 `3/3`、`GFramework.Cqrs.Abstractions/Cqrs/` 为 `20/20`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -p:RestoreFallbackFolders=`
- 结果：通过
- 备注：保留既有 `NU1900` 与 `MA0051` warnings；无新增编译错误
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
- 结果：失败
- 备注：当前环境会命中失效的 Windows fallback package folder，并在多目标 inner build 阶段触发 `MSB4276` / `MSB4018`；失败原因已记录为环境阻塞，不属于本轮文档改动回归
- `cd docs && bun run build`
- 结果：通过
- 备注：`2026-04-22` 在 `Cqrs` 波次文档刷新后重新构建通过；仅保留 VitePress 大 chunk warning，无构建失败
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/abstractions/game-abstractions.md`
- 结果：通过
- 备注：`2026-04-23` 在重写 `Game.Abstractions` 页面后验证通过
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/index.md`
- 结果：通过
- 备注：`2026-04-23` 在补充 frontmatter 与 XML inventory 后重新验证
- `python3` 轻量 XML inventory 扫描
- 结果：通过
- 备注：`2026-04-23` 确认 `GFramework.Game` 为 `56/56`、`GFramework.Game.Abstractions` 为 `80/80`、`GFramework.Game.SourceGenerators` 为 `2/2`
- `cd docs && bun run build`
- 结果：通过
- 备注：`2026-04-23` 在 `Game` 波次文档刷新后重新构建通过；仅保留 VitePress 大 chunk warning，无构建失败
- `cd docs && bun run build`
- 结果：通过
- 备注：`2026-04-23` 在更新 `AGENTS.md` 的 WSL Git 优先级后重新构建通过；仅保留 VitePress 大 chunk warning，无构建失败

## 下一步

1. 进入巡检周期，优先抽查 `docs/zh-CN/game/config-system.md`、`scene.md`、`ui.md` 与 `docs/zh-CN/source-generators/index.md` 的交叉引用是否仍与当前 `Game` family README 对齐
2. 评估是否需要把 `Godot` family 的关键 XML inventory 摘要迁回 active topic，避免长期治理只依赖 archive 恢复
3. 在后续环境治理任务中单独处理 `GFramework.Cqrs` 本地 build 的 fallback package folder 阻塞，避免影响后续代码类验证
