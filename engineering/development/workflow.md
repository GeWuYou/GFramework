# 开发与验证

本仓库独立维护源代码、测试、CI 和发布行为。个人 harness 与本地 adapter 只负责选择仓库和路由命令，不是构建前提。编码和提交要求见[贡献规则](../../AGENTS.md)。

## 开始与继续工作

先检查分支、HEAD 和工作树改动，再从[开发索引](../README.md)定位本任务需要的规则。多阶段工作只更新[路线图](../roadmap/README.md)里的单一任务；恢复时核对实际 diff 和证据适用性，不继承旧 PR 状态或历史测试结果。

使用隔离工作树，保留主检出和其他任务。个人使用时无需为不存在的第三方用户设计兼容层；涉及已有仓库使用方仍要同步迁移和验证。

## 仓库入口

从仓库根目录运行；先读取 .ai/environment/tools.ai.yaml，实际工具与库存不一致时报告差异。用户级 SDK 只通过当前进程环境选择，不把机器路径写入文档或项目。

| 目的 | 命令 |
| --- | --- |
| Core Release 构建 | `dotnet build GFramework.Core/GFramework.Core.csproj -c Release` |
| 全解决方案构建 | `dotnet build GFramework.sln -c Release` |
| 定向 Core 行为验证 | `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureLifecycleBehaviorTests"` |
| 全量测试 | `dotnet test GFramework.sln -c Release` |
| 命名检查 | `bash scripts/validate-csharp-naming.sh` |
| 许可证检查 | `python3 scripts/license-header.py --check` |
| 运行时/生成器边界 | `python3 scripts/validate-runtime-generator-boundaries.py` |
| 公开文档定向检查 | `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/index.md` |
| 文档站构建 | `cd docs && bun install --frozen-lockfile && bun run build` |

选择能证明本次改动的最小命令，不把全量门禁变成每次文案迭代的前提。提交前 review 完整 diff、受影响使用方及有意义的现有覆盖；改变运行时行为需补对应回归，纯文档不新增机械复述正文的测试。

## 证据与边界

临时探针、原始输出与交接笔记放入忽略的 `.local/`；它们不进入构建依赖、公开文档和 Git 提交。持久任务记录核对日期、源码版本、命令、实际结论和未验证内容即可。

编译成功不是行为验收，普通 dotnet test 不是 Godot/VS Code 原生宿主验收；历史 CI、合并、发布和部署分别需要当前证据。警告检查任务按 AGENTS 要求先 clean 再 build，不能用增量零警告证明警告基线。

本地构建/测试一次只运行一个，不启动后台 CI 监控。发布仅在任务授权范围内进行，文档改动使用 docs 类型，不触发功能版本发布。本轮核心评估的交付边界见[任务记录](../roadmap/core-governance.md)。
