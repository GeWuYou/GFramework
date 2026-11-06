# GFramework Skills

普通启动、续作和显式委派按仓库 [工作规则](../../AGENTS.md)执行，不需要加载流程包装 skill。
以下三个入口只保留项目专用取证与文档能力；审阅请求本身不授权修改、提交或发布。

| 场景 | 入口与结果 | 调用示例 |
| --- | --- | --- |
| 根据模块源码刷新文档 | [文档刷新](gframework-doc-refresh/SKILL.md)：解析模块与别名，核对接入路径并更新必要页面 | `$gframework-doc-refresh Core` |
| 梳理 GitHub Issue | [Issue 梳理](gframework-issue-review/SKILL.md)：读取问题、讨论与时间线，列出需本地核实的事实 | `$gframework-issue-review 梳理 issue <number>` |
| 核对 PR 审查与失败信号 | [PR 审查](gframework-pr-review/SKILL.md)：提取最新未解决审查线程、检查失败和测试诊断 | `$gframework-pr-review 检查当前分支 PR` |

保留现有 skill 调用名和脚本 CLI。普通启动、批处理与多 agent 编排的旧 skill 入口已移除；
继续任务时核对当前源码、diff 和必要的工程文档；不要求建立恢复文档。不得重建已停用的 `ai-plan/`，
也不以新目录保存同类执行留痕。需要临时笔记时使用忽略的 `.local/`，不要提交。

文档专用资源包括 `_shared/module-map.json`、[文档规范](_shared/DOCUMENTATION_STANDARDS.md)，
以及文档刷新入口下的 references、templates 和校验脚本。模块映射只维护 JSON 一份。
