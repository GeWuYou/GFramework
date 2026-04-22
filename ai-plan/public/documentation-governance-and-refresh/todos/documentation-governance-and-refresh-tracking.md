# Documentation Governance And Refresh 跟踪

## 目标

继续以“文档必须可追溯到源码、测试与真实接入方式”为原则，收敛 `GFramework` 的仓库入口、模块入口与
`docs/zh-CN` 采用链路，避免未来再次出现 API、安装方式与目录结构失真。

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-GOVERNANCE-REFRESH-RP-010`
- 当前阶段：`Phase 3`
- 当前焦点：
  - 已建立统一公开 skill：`.agents/skills/gframework-doc-refresh/`
  - 文档重构入口已从“按 guide/tutorial/api 类型拆 skill”收口为“按源码模块驱动文档刷新”
  - PR #268 的当前未解决 review 线程已进入收口：Scene/UI 标题层级修正、共享脚本 review 修复、`gframework-pr-review` 多 AI reviewer 支持补齐
  - 下一轮需要用统一 skill 推进 Godot 相关生成器页面核对

## 当前状态摘要

- 文档治理规则已收口到仓库规范，README、站点入口与采用链路不再依赖旧文档自证
- 高优先级模块入口与 `core` 关键专题页已回到可作为默认导航入口的状态，本轮计划中的 `core` 剩余高风险页面已完成收口
- 当前主题仍是 active topic，因为 `source-generators` 栏目下的 Godot 相关页面仍可能包含与实现漂移的旧内容，且统一 skill 还需要在该场景上继续落地使用

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
    继续按源码、测试、`*.csproj` 与 `ai-libs/` 下已验证参考实现核对剩余 Godot 相关页面，不把旧文档当事实来源
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

## 下一步

1. 继续核对 Godot 相关生成器页面，优先处理 `godot-project-generator.md`、`get-node-generator.md` 与
   `bind-node-signal-generator.md`，优先用 `gframework-doc-refresh` 的模块扫描结果驱动判断
2. 下一次推送后先重新执行 `$gframework-pr-review`，确认 PR #268 的 CodeRabbit / Greptile open thread 是否按预期收敛
3. 再继续确认 `project.godot`、`AutoLoad` / `InputActions`、`GetNode` / `BindNodeSignal` 示例仍与当前包关系和生成器入口一致
