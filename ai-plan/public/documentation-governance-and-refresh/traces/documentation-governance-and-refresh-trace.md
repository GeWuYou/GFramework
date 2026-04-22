# Documentation Governance And Refresh Trace

## 2026-04-22

### 当前恢复点：RP-012

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
- 本轮复核 `docs/zh-CN/tutorials/godot-integration.md`，确认其中仍残留 `GetNodeX`、`AbstractGodotModule`、`InstallGodotModule`、
  `GFramework.Godot.Pool` 等旧 API / 旧模块叙述，tutorial 已成为下一轮高优先级清理对象

### 当前决策

- active trace 只保留当前恢复点、关键事实、验证和下一步；完成阶段继续进入 `archive/traces/`
- `scene.md` 与 `ui.md` 的集成说明除目录布局外，也要保证标题层级能真实反映采用路径语义
- `gframework-pr-review` 继续以 latest-head unresolved thread 为主信号，同时显式声明支持的 AI reviewer 名单，避免 skill
  声明与实际抓取能力再次漂移
- `Godot.SourceGenerators` 专题页继续采用“源码 / 测试 / README 优先，`ai-libs/` 只补消费者 wiring”的证据顺序
- `BindNodeSignal` 页面明确记录“当前不自动生成 `_Ready()` / `_ExitTree()`”，避免继续把它写成自动生命周期织入器
- `auto-register-exported-collections` 页面明确区分“运行时 null 时跳过注册”和“配置错误时编译期报错”，避免旧文档把两类边界混为一谈
- `godot-integration.md` 不再适合作为当前 Godot 采用路径的事实来源；后续只能把它当待修输出，而不是事实输入

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
- `rg -n "GetNodeX|AbstractGodotModule|InstallGodotModule|GodotGameArchitecture|GFramework\\.Godot\\.Pool" docs/zh-CN/tutorials/godot-integration.md`

### 下一步

1. 下一次推送后重新执行 `$gframework-pr-review`，确认 PR #268 的 CodeRabbit / Greptile open thread 是否关闭或减少
2. 继续使用 `gframework-doc-refresh` 对 `Godot.SourceGenerators` 做真实模块扫描
3. 优先重写或拆分 `tutorials/godot-integration.md`，先清掉已确认失真的 Godot 旧 API 与旧模块示例
