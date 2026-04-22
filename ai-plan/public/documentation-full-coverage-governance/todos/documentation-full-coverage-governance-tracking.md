# Documentation Full Coverage Governance 跟踪

## 目标

建立一个长期 active topic，持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API
参考链路，避免历史上的阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 明确哪些目录是可直接消费模块，哪些只是内部支撑模块
- 把 XML 文档缺口纳入治理范围，而不是只刷新 Markdown

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-002`
- 当前阶段：`Phase 2 - Ecs Runtime Docs Refresh`
- 当前焦点：
  - 重写 `docs/zh-CN/ecs/index.md` 与 `docs/zh-CN/ecs/arch.md`
  - 为 `Ecs.Arch` / `Ecs.Arch.Abstractions` 建立 runtime / abstractions / XML 阅读链路
  - 继续把模块族 inventory 从“入口存在”推进到“可审计的 XML / README / landing 对照表”

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

## Inventory（第一版）

| 模块族 | 当前状态 | 当前证据 | 下一动作 |
| --- | --- | --- | --- |
| `Core` / `Core.Abstractions` | `README / landing / 类型族级 XML inventory 已收口，成员级审计待补齐` | 根 README、模块 README、`docs/zh-CN/core/**`、`docs/zh-CN/abstractions/core-abstractions.md` 已对齐当前目录与类型族基线 | 进入巡检；如有新 API 变更，再追加成员级 XML 审计 |
| `Cqrs` / `Cqrs.Abstractions` / `Cqrs.SourceGenerators` | `待重写` | README 已存在；站内入口目前分散在 `docs/zh-CN/core/cqrs.md` 与 `docs/zh-CN/source-generators/**` | 在 `Cqrs` 波次里补 dedicated landing / API map 审计 |
| `Game` / `Game.Abstractions` / `Game.SourceGenerators` | `已验证` | 根 README、模块 README、`docs/zh-CN/game/**` 和 abstractions 页已存在 | 后续波次补 XML / 教程链路审计 |
| `Godot` / `Godot.SourceGenerators` | `已验证` | 上一轮归档 topic 已完成核心 landing / topic / tutorial 校验 | 进入巡检周期，重点看回漂 |
| `Ecs.Arch` / `Ecs.Arch.Abstractions` | `待重写` | runtime README 已存在；`Ecs.Arch.Abstractions` README 与抽象页已在本轮补齐；`docs/zh-CN/ecs/index.md` / `arch.md` 仍偏旧 | 在 `Ecs` 波次重写 runtime docs 并补 XML map |
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

- `docs/zh-CN/ecs/index.md` 与 `docs/zh-CN/ecs/arch.md` 仍保留较多早期表述，本轮只先完成 inventory 与入口补齐
  - 缓解措施：把 `Ecs.Arch` 排进前五波次，并把 runtime docs 重写列为该波次的必做项
- 当前 `Core` / `Core.Abstractions` 只完成了类型族级 XML 基线，不等于成员级契约全审计
  - 缓解措施：后续只在共享抽象或高风险生命周期接口发生改动时补成员级细审，不在本轮扩张范围
- 其他模块族尚未全部建立同粒度的 XML inventory
  - 缓解措施：按 `Ecs`、`Cqrs`、`Game` 的波次顺序继续推广同一模板
- 新功能分支若修改 README / docs / 公共 API 却不挂文档 topic，仍可能回漂
  - 缓解措施：将本 topic 作为长期 active topic 保留，并在后续巡检中记录回漂来源
- VitePress 页面不能直接链接到 `docs/` 目录之外的模块 `README.md`
  - 缓解措施：站内页面用模块路径文本或站内 API 入口表达，仓库级 README 仍保留仓库文件链接

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

## 下一步

1. 进入 `Ecs` 波次，按源码重写 `docs/zh-CN/ecs/index.md` 与 `docs/zh-CN/ecs/arch.md`
2. 为 `Ecs.Arch` / `Ecs.Arch.Abstractions` 建立与 `Core` 同粒度的 XML / README / landing inventory
3. 继续为每个模块族补“README / landing / tutorials / API reference / XML”对照表，持续清零 `P0` / `P1`
