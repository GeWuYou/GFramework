# Documentation Governance And Refresh 跟踪

## 目标

继续以“文档必须可追溯到源码、测试与真实接入方式”为原则，收敛 `GFramework` 的仓库入口、模块入口与
`docs/zh-CN` 采用链路，避免未来再次出现 API、安装方式与目录结构失真。

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-GOVERNANCE-REFRESH-RP-017`
- 当前阶段：`Phase 3`
- 当前焦点：
  - 已建立统一公开 skill：`.agents/skills/gframework-doc-refresh/`
  - 文档重构入口已从“按 guide/tutorial/api 类型拆 skill”收口为“按源码模块驱动文档刷新”
  - `docs/zh-CN/godot/index.md` 已改成源码优先的模块 landing page，不再把 `GetNodeX`、`CreateSignalBuilder`、`InstallGodotModule(...)` 写成默认入口
  - `docs/zh-CN/godot/architecture.md` 已改成当前锚点生命周期、模块挂接顺序和接口边界说明，不再沿用旧版 `.Wait()` 叙述
  - `docs/zh-CN/godot/scene.md` 与 `docs/zh-CN/godot/ui.md` 已按当前 factory / registry / root / source-generator wiring 重写完成
  - `docs/zh-CN/godot/signal.md` 已按当前 `Signal(...)` / `SignalBuilder` / `[BindNodeSignal]` 分工重写完成
  - `docs/zh-CN/godot/extensions.md` 已按当前 `GodotPathExtensions`、`NodeExtensions`、`SignalFluentExtensions` 与 `UnRegisterExtension` 重写完成
  - `docs/zh-CN/godot/logging.md` 已按当前 provider / factory / logger 结构、Godot 控制台输出语义与 CoreGrid 架构接线重写完成
  - 下一轮高优先级工作转为评估 Godot 栏目当前 active 恢复点是否可以收口并迁入 archive

## 当前状态摘要

- 文档治理规则已收口到仓库规范，README、站点入口与采用链路不再依赖旧文档自证
- 高优先级模块入口、`core` 关键专题页与 `tutorials/godot-integration.md` 已回到“以源码 / 测试 / README 为准”的状态
- `docs/zh-CN/godot/index.md`、`architecture.md`、`scene.md` 与 `ui.md` 已完成当前实现收口
- 当前主题仍是 active topic，因为 Godot 栏目本轮已完成 `logging.md` 收口，但仍需确认是否可以把当前阶段历史迁入
  `archive/`，并在下一次推送后跟进 PR #268 的 review 线程收敛情况

## 当前活跃事实

- 旧 `local-plan/` 的详细 todo 与 trace 已迁入主题内 `archive/`
- 当前分支 `docs/sdk-update-documentation` 已在 `ai-plan/public/README.md` 建立 topic 映射
- active 跟踪文件只保留当前恢复点、活跃事实、风险与下一步，不再重复保存已完成阶段的长篇历史
- active trace 已把 RP-001 到 RP-008 的闭环历史归档到
  `ai-plan/public/documentation-governance-and-refresh/archive/traces/documentation-governance-and-refresh-rp-001-through-rp-008.md`
- `core`、`game` 与 `source-generators` 三个栏目入口页现在都以模块 README 与当前包拆分为准
- `docs` 站点构建已验证通过，修正了 VitePress 对 `docs/` 目录外相对链接的 dead-link 检查问题
- `core` 关键专题页已移除 `Init()`、属性式 `CommandBus` / `QueryBus`、旧 `Input` 赋值式示例和已移除的
  `RegisterMediatorBehavior` 等过时说明
- `core/index.md` 已把 `Godot` 与 `Source Generators` 栏目入口改成可点击链接，补齐 landing page 导航一致性
- `documentation-governance-and-refresh` active trace 已把重复的 `### 下一步` 标题改成带恢复点标识的唯一标题，消除
  `MD024/no-duplicate-heading` 告警
- `gframework-pr-review` 脚本已修复“空 `APPROVED` review 覆盖非空 CodeRabbit review body”的解析路径，当前分支可重新提取 Nitpick comments
- `gframework-pr-review` 现在显式把 `coderabbitai[bot]` 与 `greptile-apps[bot]` 视为支持的 AI reviewer，并在输出中单独列出
  reviewer 元数据与 latest-head open thread 计数，不再只把 `greptile-apps` 混在通用 thread 列表里
- `.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py` 已为全部函数补齐 docstring；本地 AST 统计为
  `44/44`，文件级 docstring coverage 为 `100%`
- `docs/zh-CN/core/events.md`、`property.md` 与 `logging.md` 已改成“当前角色、最常用入口、边界和迁移建议”的结构，
  不再复刻旧版大而全 API 列表
- `docs/zh-CN/core/property.md` 已明确记录 `BindableProperty<T>.Comparer` 的闭合泛型级共享语义，避免文档继续误导读者把
  `WithComparer(...)` 当成实例级配置
- `docs/zh-CN/core/state-management.md` 与 `coroutine.md` 已按当前 runtime / 测试重新核对，当前内容可继续保留
- `docs/zh-CN/game/scene.md` 已改成“真实公开入口、场景栈语义、factory/root 装配、过渡处理器与守卫扩展点”的结构，
  不再暗示框架自带统一场景注册与完整引擎装配；本轮已补充项目侧目录布局、文件命名、最小 wiring 与兼容说明，并把
  “推荐目录与文件约定（项目侧）” 收口为 “最小接入路径” 下的子节
- `docs/zh-CN/game/ui.md` 已改成“Page 栈、layer UI、输入动作仲裁、World 阻断与暂停语义”的结构，明确 `Show(...)`
  不适用于 `UiLayer.Page`；本轮已补充 router、factory、root、page behavior、params 与 views 的推荐放置约定，并修复
  “最小接入路径” 空节与标题层级错位问题
- 本轮重写后再次执行 `cd docs && bun run build` 通过，当前 `game` 栏目入口与专题页改动没有破坏站点构建
- `docs/zh-CN/source-generators/context-aware-generator.md` 已改成“真实生成成员、provider/实例缓存语义、与 `ContextAwareBase` 的边界、测试接法”的结构，
  不再用旧版简化生成代码替代当前实现
- `docs/zh-CN/source-generators/priority-generator.md` 已改成“生成 `IPrioritized`、priority-aware 检索 API、动态优先级边界与诊断”的结构，
  不再把 `GetAllByPriority<T>()` / `system.Init()` 当作所有场景的默认示例
- 本轮重写后再次执行 `cd docs && bun run build` 通过，当前 `source-generators` 栏目改动没有破坏站点构建
- `docs/zh-CN/source-generators/godot-project-generator.md` 已改成“包关系、最小接入路径、AutoLoad / InputActions 生成语义、`project.godot` 文件约束与诊断边界”的结构，
  明确 `GFrameworkGodotProjectFile` 只能改相对路径、不能改文件名
- `docs/zh-CN/source-generators/get-node-generator.md` 已改成“字段注入职责、路径推断、`Required` / `Lookup` 语义、`_Ready()` 自动补齐边界与冲突诊断”的结构，
  明确只有缺少 `_Ready()` 时才会生成 `OnGetNodeReadyGenerated()`
- `docs/zh-CN/source-generators/bind-node-signal-generator.md` 已改成“CLR event 绑定职责、生命周期接线要求、与 `[GetNode]` 的调用顺序、签名约束与命名冲突”的结构，
  明确当前不会自动生成 `_Ready()` / `_ExitTree()`
- `docs/zh-CN/source-generators/auto-register-exported-collections-generator.md` 已补齐 frontmatter，并改成“成员形状、registry 匹配规则、null-skip 行为、编译期诊断与 CoreGrid 真实采用路径”的结构，
  明确生成器依赖的是实例可读集合成员与可读 registry 成员，不要求成员必须带 `[Export]`
- `docs/zh-CN/tutorials/godot-integration.md` 已改成“包关系、`project.godot` 接线、`[GetNode]` / `[BindNodeSignal]` 协作顺序、运行时扩展边界、迁移提醒”的结构，
  不再把 `GetNodeX`、`CreateSignalBuilder`、`AbstractGodotModule` 默认化叙述为当前推荐路径
- `docs/zh-CN/tutorials/index.md` 中 Godot 教程入口摘要已同步改成“项目级配置 + 生成器协作 + 生命周期边界”，不再继续宣传对象池 / 性能优化式旧范围
- `docs/zh-CN/godot/index.md` 已改成“模块定位、包关系、最小接入路径、关键入口、当前边界”的 landing page 结构，并明确把
  `[GetNode]`、`[BindNodeSignal]`、`AutoLoads`、`InputActions` 归到 `GFramework.Godot.SourceGenerators`
- `docs/zh-CN/godot/architecture.md` 已改成“何时继承 `AbstractArchitecture`、何时使用 `InstallGodotModule(...)`、锚点生命周期、
  `IGodotModule` 契约边界”的结构，不再把 `OnPhase(...)` / `OnArchitecturePhase(...)` 写成稳定自动广播
- 本轮再次执行 `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh` 校验 `godot/index.md` 与
  `godot/architecture.md`，并执行 `cd docs && bun run build`，站点构建继续通过
- `docs/zh-CN/godot/scene.md` 已改成“公开入口、factory 实际行为、项目侧 router/root wiring、`[AutoScene]` 最小接入路径、
  当前边界”的结构，明确当前没有 `GodotSceneRouter`，且 `GodotSceneFactory` 会在 provider 缺失时回退到
  `SceneBehaviorFactory`
- `docs/zh-CN/godot/ui.md` 已改成“公开入口、layer behavior 语义、项目侧 router/root wiring、`[AutoUiPage]` 最小接入路径、
  输入与暂停边界”的结构，明确当前没有 `GodotUiRouter`，且 `GodotUiFactory` 仍强制要求 `IUiPageBehaviorProvider`
- 本轮已执行 `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/scene.md` 与
  `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`，两页聚焦校验通过
- `docs/zh-CN/godot/signal.md` 已改成“当前公开入口、动态绑定最小接入路径、与 `[BindNodeSignal]` 的分工、当前边界”的结构，
  不再沿用旧 `CreateSignalBuilder(...)` / builder-pattern 教程式长篇叙述
- `docs/zh-CN/godot/extensions.md` 已改成“真实扩展分组、Node 辅助成员表、`UnRegisterWhenNodeExitTree(...)` 生命周期边界、
  当前边界”的结构，不再把扩展层写成覆盖所有 Godot 开发动作的万能工具箱
- 本轮已执行 `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md` 与
  `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/extensions.md`，两页聚焦校验通过
- 本轮再次执行 `cd docs && bun run build` 通过，当前 Godot signal / extensions 页面改动没有破坏站点构建
- `docs/zh-CN/godot/logging.md` 已改成“当前公开入口、最小接入路径、Godot 控制台输出语义、`[Log]` 协作边界、当前限制”的结构，
  不再把直接改写 `LoggerFactoryResolver.Provider`、`AbstractGodotModule` 或 Godot 专用日志 API 写成默认接入模型
- 本轮已执行 `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/logging.md` 与
  `cd docs && bun run build`，logging 页面聚焦校验与站点构建继续通过
- `.agents/skills/gframework-doc-refresh/SKILL.md` 已改成标准 YAML frontmatter skill，并明确支持模块输入、证据顺序、输出优先级与验证步骤
- `.agents/skills/gframework-doc-refresh/SKILL.md` 的 `description` 已加引号，修复 `Recommended command:` 中冒号导致的
  invalid YAML skill 加载警告
- `.agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh` 已改成基于 `IN_CODE_BLOCK` 跟踪 opening /
  closing fence，避免把 closing fence 误报成缺少语言标记
- `.agents/skills/_shared/module-config.sh` 的 `get_readme_paths()` 已补齐 `Core.SourceGenerators.Abstractions`、
  `Godot.SourceGenerators.Abstractions`、`Ecs.Arch.Abstractions` 与 `SourceGenerators.Common`，并在未映射模块时返回
  非零退出码
- `.agents/skills/_shared/module-map.json` 已收口为源码模块映射表，覆盖源码目录、测试项目、README、`docs/zh-CN` 栏目与 `ai-libs/` 参考入口
- 旧 `vitepress-api-doc`、`vitepress-batch-api`、`vitepress-doc-generator`、`vitepress-guide`、`vitepress-tutorial`、`vitepress-validate`
  已不再保留为可用公开 skill 定义文件
- `ai-libs/` 已纳入统一 skill 的标准证据链，只作为消费者接入参考，不再替代源码与测试契约

## 当前风险

- 旧专题页示例失真风险：`docs/zh-CN/game/*` 与 `source-generators/*` 中仍可能保留看似合理但与真实实现不一致的示例
  - 缓解措施：`game/scene.md`、`ui.md`、`source-generators/context-aware-generator.md` 与 `priority-generator.md` 已完成收口；
    `godot-project-generator.md`、`get-node-generator.md`、`bind-node-signal-generator.md` 与 `auto-register-exported-collections-generator.md`
    已完成收口；
    继续按源码、测试、`*.csproj` 与 `ai-libs/` 下已验证参考实现核对剩余 Godot 相关页面，不把旧文档当事实来源
- Godot 栏目归档过早风险：虽然 `logging.md` 已完成收口，但如果在推送前就把当前阶段过早归档，后续 review 跟进会缺少
  清晰的 active 恢复入口
  - 缓解措施：先保留当前 topic 为 active；待确认本轮页面集与 PR #268 的 review 跟进节奏后，再决定是否迁入 `archive/`
- 采用路径误导风险：根聚合包与模块边界若再次被写错，会继续误导消费者的包选择
  - 缓解措施：保持“源码与包关系优先”的证据顺序，改动采用说明时同步核对包依赖与生成器 wiring
- 模块映射不全风险：统一 skill 若遗漏模块别名、测试项目或 docs 栏目映射，会让后续扫描阶段直接失焦
  - 缓解措施：以当前 `*.csproj` 族为 canonical module list，统一维护 `.agents/skills/_shared/module-map.json`
- `ai-libs/` 漂移风险：参考项目若滞后于当前实现，可能把过时 wiring 重新带回文档
  - 缓解措施：在 skill 中固定“源码/测试优先，`ai-libs/` 只补 adoption path”的证据顺序
- 旧模板迁移失真风险：旧 `vitepress-*` skill 的模板和规范若原样沿用，可能继续输出过时结构
  - 缓解措施：只迁移可复用骨架，把输出优先级和证据规则重写进统一 skill
- 统一入口过宽风险：若 `gframework-doc-refresh` 的触发描述过宽，可能在模块不明确时误进入文档生成
  - 缓解措施：要求先做模块归一化；遇到栏目别名歧义时只返回建议，不直接生成文档
- Active 入口回膨胀风险：后续若把栏目级重写过程直接追加到 active 文档，会再次拖慢恢复
  - 缓解措施：阶段完成并验证后，继续把细节迁入本 topic 的 `archive/`
- review 跟进遗漏风险：如果 PR review 抓取继续优先选中空 review body，会漏掉 CodeRabbit 的 Nitpick 和
  linter 跟进项
  - 缓解措施：保持当前“最新提交 + 最新非空 CodeRabbit review body”解析策略，并在有疑点时以 API 实抓结果复核
- reviewer 适配漂移风险：若后续新增 AI reviewer 但脚本仍只维护固定 bot 名单，可能再次出现“线程能看见、skill 却未声明覆盖”的偏差
  - 缓解措施：当前已显式支持 `coderabbitai[bot]` 与 `greptile-apps[bot]`；新增 reviewer 时同步更新
    `.agents/skills/gframework-pr-review/SKILL.md`、`agents/openai.yaml` 与抓取脚本常量表

## 活跃文档

- 历史跟踪归档：[documentation-governance-and-refresh-history-through-2026-04-18.md](../archive/todos/documentation-governance-and-refresh-history-through-2026-04-18.md)
- 历史 trace 归档：[documentation-governance-and-refresh-history-through-2026-04-18.md](../archive/traces/documentation-governance-and-refresh-history-through-2026-04-18.md)
- RP-001 到 RP-008 trace 归档：[documentation-governance-and-refresh-rp-001-through-rp-008.md](../archive/traces/documentation-governance-and-refresh-rp-001-through-rp-008.md)

## 验证说明

- 旧 `local-plan/` 的详细实施历史与文档站构建结果已迁入主题内归档
- active 跟踪文件已按 `ai-plan` 治理规则精简为当前恢复入口
- `cd docs && bun run build`
- `python3 -B .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --branch docs/sdk-update-documentation --format json --json-output /tmp/current-pr-review.json`
- `python3 -B .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --branch docs/sdk-update-documentation --section open-threads`
- `python3 -B -c "import ast, pathlib; path=pathlib.Path('.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py'); tree=ast.parse(path.read_text(encoding='utf-8')); funcs=[node for node in ast.walk(tree) if isinstance(node,(ast.FunctionDef, ast.AsyncFunctionDef))]; documented=sum(1 for node in funcs if ast.get_docstring(node)); print(f'functions={len(funcs)} documented={documented} coverage={documented/len(funcs):.2%}')"`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh docs/zh-CN/game/scene.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh docs/zh-CN/game/ui.md`
- `bash -lc 'source .agents/skills/_shared/module-config.sh && get_readme_paths Core.SourceGenerators.Abstractions && if get_readme_paths Not.Real.Module; then exit 1; else echo unmapped-ok; fi'`
- `python3 -c "import pathlib, yaml; text = pathlib.Path('.agents/skills/gframework-doc-refresh/SKILL.md').read_text(); yaml.safe_load(text.split('---', 2)[1]); print('yaml-ok')"`
- `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Core`
- `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Godot.SourceGenerators`
- `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Cqrs`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/godot-project-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/get-node-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/bind-node-signal-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/auto-register-exported-collections-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/godot-integration.md`
- `rg -n "GetNodeX|CreateSignalBuilder|GodotGameArchitecture|AbstractGodotModule|InstallGodotModule\(|GFramework\.Godot\.Pool" docs/zh-CN/godot docs/zh-CN/tutorials -S`
- `cd docs && bun run build`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/architecture.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/scene.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/extensions.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/logging.md`
- `rg -n "GodotSceneRouter|GodotUiRouter|CreateSignalBuilder|GetNodeX|InstallGodotModule\(" docs/zh-CN/godot -S`
- `cd docs && bun run build`

## 下一步

1. 评估当前 Godot 栏目页面集是否已足够稳定，决定是否把本阶段 active 恢复点收口并迁入 `archive/`
2. 如需继续保持 active，优先精简 tracking / trace，只保留归档决策、当前风险与下一次 PR follow-up 入口
3. 下一次推送后重新执行 `$gframework-pr-review`，确认 PR #268 的 CodeRabbit / Greptile open thread 是否按预期收敛
