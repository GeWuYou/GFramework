# 开发与验证

本仓库独立维护源代码、测试、CI 和发布行为。个人 harness 与本地 adapter 只负责选择仓库和路由命令，不是构建前提。编码和提交要求见[贡献规则](../../AGENTS.md)。

## 开始与继续工作

先检查分支、HEAD 和工作树改动，再从[开发索引](../README.md)定位本任务需要的规则。继续工作时核对实际 diff 和证据适用性，不继承旧 PR 状态或历史测试结果。多步骤任务无需先写计划或恢复文档；只有目标、依赖、验收条件或状态发生实质变化时，才更新[路线图](../roadmap/README.md)的已有条目。

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
| 查看累计提交的 CI 影响范围 | `python3 scripts/ci_impact.py --base origin/main --head HEAD --json` |
| 验证 CI 路由与执行边界 | `python3 -m unittest discover -s scripts -p 'test_ci_*.py'` |

选择能证明本次改动的最小命令，不把全量门禁变成每次文案迭代的前提。提交前 review 完整 diff、受影响使用方及有意义的现有覆盖；改变运行时行为需补对应回归，纯文档不新增机械复述正文的测试。

## 按需 CI

PR 先分析从共同祖先到源分支 head 的累计差异，再验证 GitHub 提供的合并修订。分析入口只读取 Git 提交，不包含未提交改动；手动运行 CI 或使用 `--full` 始终执行完整验证。选择结果记录比较修订、实际验证修订、项目清单和原因，执行入口拒绝用于其他修订的结果。

| 输入 | PR 验证范围 |
| --- | --- |
| 工程文档、贡献说明、个人技能 | 轻量仓库约束、脚本回归与差异秘密扫描 |
| 文档站内容、配置和依赖 | 文档构建；支持的源码或配置还执行 MegaLinter |
| 配置工具 | 独立工具测试及相关质量检查 |
| .NET 源码、测试、生成器与模块构建配置 | 受影响项目及实际使用方的 Release 构建、测试和包校验 |
| 根级构建配置、解决方案、共享 CI 路由 | 完整验证 |

模块关系来自项目引用，包括 private/analyzer 引用、共享源码和导入的 targets；比较前、源分支和实际合并修订的关系共同决定范围，删除或移除引用不会隐藏原使用方。包内 README 属于打包输入，AnalyzerReleases 属于编译输入；普通模块说明不触发 .NET。未知输入、不能可靠解析的引用或缺失基线回退完整验证。

.NET 使用一个构建任务和临时 solution filter 复用 MSBuild 依赖图；只执行选中的测试项目并保留各自退出码。没有测试的选择不生成测试报告。包清单校验的 `--expected-packages` 参数仅用于显式子集，正式发布继续使用默认完整清单。

CodeQL 仅在 C# 或相关构建配置变化时响应 PR，触发后仍分析全部 C#，保留跨模块安全数据流。每日及手动扫描继续全量运行。秘密扫描覆盖文档；MegaLinter 的扫描、格式与报告上传错误均阻断合并。`CI Result` 要求分析、轻量治理和全部选中任务成功，失败、取消、缺失或意外跳过均不能通过汇总。

MegaLinter 的共享工作流由 PR 按需调用，并在 `main` 更新时独立全量扫描，不重复启动 .NET 构建、测试或发布。MegaLinter v10 使用 Betterleaks 代替已移除的 Gitleaks；聚合 SARIF 报告上传到代码扫描，缺失报告或上传失败会明确报错。手动运行 MegaLinter 也执行全量扫描。

框架包与合规工作流仅接受合法的 `v1.2.3`、`V1.2.3`、`1.2.3` 及 SemVer 预发布/构建后缀。配置工具继续使用 `gframework-config-tool-v*`；文档仅随正式小写 `v` 标签或手动操作发布。触发过滤后还会校验版本，格式错误的标签不会启动发布任务。版本审批、Benchmark 和许可证修复仍使用各自手动入口。

GitHub 自动管理的 NuGet 依赖提交独立于主 CI。默认分支规则集要求来自 GitHub Actions 的 `CI Result` 与 `submit-nuget` 成功，并要求分支基于最新主分支验证；管理员及自动合并均没有绕过名单。按影响范围明确不适用的任务允许跳过，已选中的任务必须成功。新增独立 PR 检查时，应将其接入汇总或显式加入必需状态检查，避免出现失败但不阻断合并的检查。标签发布、每日扫描等合并后任务保留各自触发和审批边界。

文档与配置工具任务固定使用官方 npm 发布的 Bun 1.4.4，避开重复失败的 GitHub Release 下载链路。下载服务的 HTTP 5xx 或工具下载损坏应保留失败证据，核对源码和工具版本后只重试失败任务；重试不免除必需检查。

## 证据与边界

必要的临时探针、原始输出与本地交接可放入忽略的 `.local/`，不要求每个任务生成。它们不进入构建依赖或 Git 提交，也不改名搬入工程文档。实际验证结果与未验证项在 PR 或对话中报告；长期文档只保留仍有用的设计结论和限制。

编译成功不是行为验收，普通 dotnet test 不是 Godot/VS Code 原生宿主验收；历史 CI、合并、发布和部署分别需要当前证据。警告检查任务按 AGENTS 要求先 clean 再 build，不能用增量零警告证明警告基线。

本地构建/测试一次只运行一个，不启动后台 CI 监控。发布仅在任务授权范围内进行，文档改动使用 docs 类型，不触发功能版本发布。核心治理的阶段边界见[实施路线](../roadmap/core-governance.md)。
