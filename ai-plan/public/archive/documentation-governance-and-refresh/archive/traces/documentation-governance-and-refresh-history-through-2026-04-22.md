# Documentation Governance And Refresh Trace

## 2026-04-22

### 当前恢复点：RP-017

- 本轮从 PR #268 的最新 review 数据恢复，未发现失败检查；CTRF 报告显示 2139 个测试全部通过
- 本轮复核确认当前 PR 的 latest-head open thread 同时来自 `coderabbitai[bot]` 与 `greptile-apps[bot]`
- 已本地修复仍然成立的 review：
  - `docs/zh-CN/game/scene.md` 把“推荐目录与文件约定（项目侧）”降为“最小接入路径”下的子节
  - `docs/zh-CN/game/ui.md` 为“最小接入路径”补充导语，并修复同级标题错位
  - `.agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh` 改成 opening / closing fence 状态机
  - `.agents/skills/_shared/module-config.sh` 补齐缺失模块映射，并让未映射模块返回非零退出码
- `gframework-pr-review` 已从文案和输出模型两侧补齐多 reviewer 支持：当前 JSON 会单独给出 `review_agents`
  以及 `open_thread_counts_by_user`，文本输出会显式列出 CodeRabbit / Greptile
- `fetch_current_pr_review.py` 的本地函数 docstring 覆盖率已补到 `44/44`
- 已闭环 RP-001 到 RP-008 的执行细节已归档到
  `ai-plan/public/documentation-governance-and-refresh/archive/traces/documentation-governance-and-refresh-rp-001-through-rp-008.md`
- 本轮按 `gframework-doc-refresh` 的模块扫描结果，重写了 `Godot.SourceGenerators` 的 3 个高风险专题页：
  - `godot-project-generator.md`
  - `get-node-generator.md`
  - `bind-node-signal-generator.md`
- 新页面统一收口到“包关系、最小接入路径、真实生成语义、生命周期边界、诊断约束”，不再沿用旧教程式长篇 API 罗列
- 本轮额外复核了 `ai-libs/CoreGrid` 的真实采用方式，确认 `[GetNode]` / `[BindNodeSignal]` 组合使用时应先注入节点再绑定事件
- 本轮继续收口 `auto-register-exported-collections-generator.md`，补齐 frontmatter，并把“导出集合”纠正为“实例可读集合成员 + registry 成员 + 单参数实例方法”的真实契约
- 本轮已重写 `docs/zh-CN/tutorials/godot-integration.md`，把内容收口为“包关系、`project.godot` 接线、`[GetNode]` /
  `[BindNodeSignal]` 协作顺序、运行时扩展边界、迁移提醒”，不再把旧 Godot API 列表当事实来源
- `docs/zh-CN/tutorials/index.md` 的 Godot 教程入口摘要已同步改成当前采用路径，避免入口页继续把教程描述成对象池 / 性能优化总览
- 本轮已重写 `docs/zh-CN/godot/index.md`，改成“模块定位、包关系、最小接入路径、关键入口、当前边界”的 landing page 结构，
  明确把 `[GetNode]`、`[BindNodeSignal]`、`AutoLoads`、`InputActions` 收口到 `GFramework.Godot.SourceGenerators`
- 本轮已重写 `docs/zh-CN/godot/architecture.md`，改成“锚点生命周期、`InstallGodotModule(...)` 执行顺序、`IGodotModule`
  契约边界”的结构，不再沿用旧版 `.Wait()` 和自动阶段广播叙述
- 本轮已重写 `docs/zh-CN/godot/scene.md`，把内容收口为“公开入口、factory 真实行为、项目侧 router/root wiring、
  `ISceneBehaviorProvider` 与 `[AutoScene]` 的真实关系、当前边界”，不再继续虚构 `GodotSceneRouter`
- 本轮已重写 `docs/zh-CN/godot/ui.md`，把内容收口为“公开入口、layer behavior 语义、项目侧 router/root wiring、
  `IUiPageBehaviorProvider` 与 `[AutoUiPage]` 的真实关系、输入与暂停边界”，不再继续虚构 `GodotUiRouter`
- 本轮额外确认 Godot Scene / UI 的关键差异：`GodotSceneFactory` 在 provider 缺失时会回退到 `SceneBehaviorFactory`，
  而 `GodotUiFactory` 仍会在缺失 `IUiPageBehaviorProvider` 时直接抛异常；这已写入两页文档，避免继续把两者描述成同一种接入模型
- 本轮已重写 `docs/zh-CN/godot/signal.md`，把内容收口为“当前公开入口、动态绑定最小接入路径、与 `[BindNodeSignal]`
  的分工、当前边界”，明确当前入口是 `Signal(...)` 而不是旧 `CreateSignalBuilder(...)`
- 本轮已重写 `docs/zh-CN/godot/extensions.md`，把内容收口为“真实扩展分组、`NodeExtensions` 实际成员、`UnRegisterWhenNodeExitTree(...)`
  生命周期边界、当前边界”，不再继续宣称存在覆盖所有 Godot 场景的万能扩展层
- 本轮复核 `ai-libs/CoreGrid` 的动态绑定用法后，明确把 fluent API 定位为“动态对象 / 动态 signal 的运行时连接”，而把静态控件绑定继续归到
  `[BindNodeSignal]` 生成器链路
- 本轮已重写 `docs/zh-CN/godot/logging.md`，把内容收口为“当前 provider / factory / logger 结构、最小接入路径、
  Godot 控制台输出语义、`[Log]` 协作边界、当前限制”，不再把直接改全局 provider 或 `AbstractGodotModule` 写成默认采用路径
- 本轮额外复核 `GFramework.Godot/Logging/*.cs`、`GFramework.Core.Abstractions/Logging/LoggerFactoryResolver.cs`、
  `GFramework.Core/Logging/CachedLoggerFactory.cs` 与 `ai-libs/CoreGrid/global/GameEntryPoint.cs`，确认当前推荐接法应以
  `ArchitectureConfiguration.LoggerProperties.LoggerFactoryProvider` 为主，而不是先写 `LoggerFactoryResolver.Provider = ...`

### 当前决策

- active trace 只保留当前恢复点、关键事实、验证和下一步；完成阶段继续进入 `archive/traces/`
- `scene.md` 与 `ui.md` 的集成说明除目录布局外，也要保证标题层级能真实反映采用路径语义
- `gframework-pr-review` 继续以 latest-head unresolved thread 为主信号，同时显式声明支持的 AI reviewer 名单，避免 skill
  声明与实际抓取能力再次漂移
- `Godot.SourceGenerators` 专题页继续采用“源码 / 测试 / README 优先，`ai-libs/` 只补消费者 wiring”的证据顺序
- `BindNodeSignal` 页面明确记录“当前不自动生成 `_Ready()` / `_ExitTree()`”，避免继续把它写成自动生命周期织入器
- `auto-register-exported-collections` 页面明确区分“运行时 null 时跳过注册”和“配置错误时编译期报错”，避免旧文档把两类边界混为一谈
- `godot-integration.md` 已重新成为可用的采用路径入口；后续 Godot 文档收口应优先处理 `godot/index.md` 和 `godot/architecture.md`
- `godot/index.md` 与 `godot/architecture.md` 现在都必须维持“运行时包与生成器包分边界”的写法，不能再把场景注入和项目元数据生成写回
  `GFramework.Godot` 运行时契约
- `scene.md` 已明确记录“项目侧 router + Godot factory/registry/root”这一分工，后续不要再把 router 包装回
  `GFramework.Godot` 运行时
- `ui.md` 已明确记录 `Page` 必须走 `PushAsync` / `ReplaceAsync`，`Show(..., UiLayer.Page)` 在当前实现中会抛异常；
  后续不要再把所有 UI 入口重新写回统一 `Show(...)`
- `signal.md` 已明确为 `Signal(...)` / `SignalBuilder` 的轻量 fluent 包装说明页，不再继续混入生成器职责
- `extensions.md` 已明确限制在 `GodotPathExtensions`、`NodeExtensions`、`SignalFluentExtensions` 与 `UnRegisterExtension`
  这四组当前存在的扩展
- `logging.md` 已完成收口；下一轮优先级转为评估当前 Godot 栏目恢复点是否可以迁入 `archive/`，并保留 PR review follow-up 入口

### 验证

- `python3 -B .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --branch docs/sdk-update-documentation --format json --json-output /tmp/current-pr-review.json`
- `python3 -B .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --branch docs/sdk-update-documentation --section open-threads`
- `python3 -B -c "import ast, pathlib; path=pathlib.Path('.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py'); tree=ast.parse(path.read_text(encoding='utf-8')); funcs=[node for node in ast.walk(tree) if isinstance(node,(ast.FunctionDef, ast.AsyncFunctionDef))]; documented=sum(1 for node in funcs if ast.get_docstring(node)); print(f'functions={len(funcs)} documented={documented} coverage={documented/len(funcs):.2%}')"`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh docs/zh-CN/game/scene.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh docs/zh-CN/game/ui.md`
- `bash -lc 'source .agents/skills/_shared/module-config.sh && get_readme_paths Core.SourceGenerators.Abstractions && if get_readme_paths Not.Real.Module; then exit 1; else echo unmapped-ok; fi'`
- `cd docs && bun run build`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/godot-project-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/get-node-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/bind-node-signal-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/auto-register-exported-collections-generator.md`
- `cd docs && bun run build`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/godot-integration.md`
- `cd docs && bun run build`
- `rg -n "GetNodeX|CreateSignalBuilder|GodotGameArchitecture|AbstractGodotModule|InstallGodotModule\(|GFramework\\.Godot\\.Pool" docs/zh-CN/godot docs/zh-CN/tutorials -S`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/architecture.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/scene.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/extensions.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/logging.md`
- `rg -n "GodotSceneRouter|GodotUiRouter|CreateSignalBuilder|GetNodeX|InstallGodotModule\(" docs/zh-CN/godot -S`
- `cd docs && bun run build`

### 下一步

1. 评估当前 Godot 栏目页面集是否已足够稳定，决定是否把当前恢复点收口并迁入 `archive/`
2. 如暂不归档，先把 active tracking / trace 进一步压缩到归档决策、当前风险与 PR 跟进入口
3. 下一次推送后重新执行 `$gframework-pr-review`，确认 PR #268 的 CodeRabbit / Greptile open thread 是否关闭或减少
