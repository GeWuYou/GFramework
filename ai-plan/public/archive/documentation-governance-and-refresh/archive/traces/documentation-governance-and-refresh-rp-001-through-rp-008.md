# Documentation Governance And Refresh Trace Archive RP-001 Through RP-008

> This archive preserves closed recovery-point history that no longer needs to stay in the default boot trace.
> The active trace should point here instead of repeating these completed stages.

## RP-001 Local-Plan Migration

- 迁移 `local-plan/` 中的 durable recovery state 到
  `ai-plan/public/documentation-governance-and-refresh/`
- 建立 `todos/`、`traces/`、`archive/todos/` 与 `archive/traces/`
- 在 `ai-plan/public/README.md` 中建立
  `docs/sdk-update-documentation` 到 `documentation-governance-and-refresh` 的映射
- 同步记录 `ai-plan-governance` 主题的迁移结论

## RP-002 Column Landing Pages

- 复核 `docs/zh-CN/core/index.md`、`game/index.md` 与 `source-generators/index.md`
- 对照模块 README 与包拆分关系，重写三个栏目 landing page
- 修正 VitePress dead-link 检查中指向 `docs/` 目录外 README 的链接方式
- 验证：`cd docs && bun run build`

## RP-003 Core Topic Pages

- 核对并重写 `architecture.md`、`context.md`、`lifecycle.md`、`command.md`、`query.md` 与 `cqrs.md`
- 移除旧 `Init()`、属性式 `CommandBus` / `QueryBus`、旧 `Input` 赋值式示例和已移除的
  `RegisterMediatorBehavior` 说明
- 将旧 command / query 体系说明收口为兼容路径，并把新功能推荐迁到 `GFramework.Cqrs`
- 验证：`cd docs && bun run build`

## RP-004 PR Review Script Follow-Up

- 修复 `gframework-pr-review` 把空 `APPROVED` review body 误选为 CodeRabbit review body 的解析路径
- 改为在同一提交上优先选择最新非空 CodeRabbit review body
- 补齐 `docs/zh-CN/core/index.md` 中 `Godot` 与 `Source Generators` 栏目入口链接
- 修正 active trace 重复标题，消除 `MD024/no-duplicate-heading` 噪音
- 验证：
  - `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json`
  - `cd docs && bun run build`

## RP-005 Remaining Core High-Risk Topics

- 核对 `events.md`、`property.md`、`state-management.md`、`coroutine.md` 与 `logging.md`
- 重写 `events.md`、`property.md` 与 `logging.md`
- 明确 `BindableProperty<T>.Comparer` 按闭合泛型共享，不是实例级配置
- 确认 `state-management.md` 与 `coroutine.md` 当前仍可保留
- 验证：`cd docs && bun run build`

## RP-006 Game Scene And UI Topics

- 核对 `docs/zh-CN/game/scene.md` 与 `docs/zh-CN/game/ui.md`
- 重写场景路由文档，明确 `ISceneFactory`、`ISceneRoot`、项目侧 router 与过渡处理器的职责边界
- 重写 UI 文档，明确 Page 栈、层级 UI、输入仲裁、World 阻断与暂停语义
- 验证：`cd docs && bun run build`

## RP-007 Core Source Generator Topics

- 核对 `context-aware-generator.md` 与 `priority-generator.md`
- 重写 `[ContextAware]` 文档，说明当前生成成员、provider/实例缓存语义与 `ContextAwareBase` 边界
- 重写 `[Priority]` 文档，说明只生成 `IPrioritized`，排序效果取决于调用方是否走 priority-aware API
- 验证：`cd docs && bun run build`

## RP-008 Unified Documentation Refresh Skill

- 删除旧 `vitepress-*` 公开 skill 定义，建立统一 `.agents/skills/gframework-doc-refresh/`
- 新增 `.agents/skills/_shared/module-map.json`，按源码模块而不是文档类型驱动刷新
- 重写共享文档标准，固定证据顺序：源码 / XML docs / `*.csproj`、测试、README、当前 docs、`ai-libs/`、归档文档
- 新增 `scan_module_evidence.py`，支持模块别名归一化、docs 栏目歧义检测和证据面扫描
- 更新 `.agents/skills/README.md`，将统一入口作为推荐工作流
- 验证：
  - `python3 -B .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Core`
  - `python3 -B .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Godot.SourceGenerators`
  - `python3 -B .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Cqrs`
  - `python3 -B .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py source-generators --json`
