# Analyzer Warning Reduction 追踪

## 2026-04-28 — RP-092

### 阶段：复核 `PR #300` 的 open threads，并只修正当前分支仍然成立的 `ai-plan` 漂移

- 触发背景：
  - 用户要求恢复当前 `$gframework-pr-review` 任务，继续以 PR head 上的开放线程为准做 triage
- 主线程实施：
  - 重新读取 `fetch_current_pr_review.py --json-output /tmp/current-pr-review.json` 的 latest head open threads
  - 逐条对照本地文件后确认：`TestArchitectureContextBehaviorTests`、`TestArchitectureWithRegistry`、`TestResourceLoader`、`PartialGeneratedNotificationHandlerRegistry` 相关 CodeRabbit 线程在当前工作树上都已匹配修复，仅线程状态尚未随新 head 折叠
  - 继续核对 `RegistryInitializationHookBaseTests.OnPhase_Should_Not_Throw_When_Registry_Not_Found`，确认当前实现 `RegistryInitializationHookBase.OnPhase` 已在缺少注册表时保持 no-op，定向回归测试通过
  - 修正 `analyzer-warning-reduction-tracking.md` 中仍然成立的两处漂移：
    - 将文件计数更新为相对 `6cc87a9...HEAD` 的实际规模：`18` 个已修改文件、`38` 个新增文件、合计 `56` 个变更文件
    - 将验证口径统一为 trace 已记录的 `dotnet build`、定向 `dotnet test`、`git diff --check`
- 验证里程碑：
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~RegistryInitializationHookBaseTests.OnPhase_Should_Not_Throw_When_Registry_Not_Found|FullyQualifiedName~TestArchitectureContextBehaviorTests"`
    - 结果：成功；`10` 通过、`0` 失败
  - `git diff --check`
    - 结果：成功；无新增 whitespace / conflict-marker 问题

## 2026-04-28 — RP-091

### 阶段：收口 `PR #300` 的共享测试基础设施 nitpick，并升级 PR-review triage 规则

- 触发背景：
  - 用户追问 `TestArchitectureContext` / `TestArchitectureContextV3` 的共享基础设施 nitpick 是否已经处理完成
  - 同时要求把“本地验证后仍然成立的 nitpick 不能默认降级为可选项”写入 `AGENTS.md` 或 `$gframework-pr-review`
- 主线程实施：
  - 新增 `TestArchitectureContextBase`，把容器解析、共享 `EventBus` 行为，以及 legacy / CQRS 失败契约统一收敛到一处
  - 将 `TestArchitectureContext` 与 `TestArchitectureContextV3` 收窄为薄包装类型，只保留各自的命名入口与 `Id` 差异
  - 更新 `.agents/skills/gframework-pr-review/SKILL.md`，明确要求：latest-head `Nitpick comment` 一旦本地验证仍成立且指向真实漂移/回归风险，就必须作为 actionable review input 处理，而不是默认视作可选
- 验证里程碑：
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ArchitectureServicesTests|FullyQualifiedName~ContextAwareServiceExtensionsTests|FullyQualifiedName~TestArchitectureContextBehaviorTests|FullyQualifiedName~RegistryInitializationHookBaseTests|FullyQualifiedName~ArchitectureContextTests"`
    - 结果：成功；`67` 通过、`0` 失败
  - `git diff --check`
    - 结果：成功；无新增 whitespace / conflict-marker 问题

## 活跃风险

- GitHub PR 上的 open threads 在本地提交前仍可能显示为未关闭。
  - 缓解措施：以当前工作树和定向验证作为真值，推送后再让 PR 线程重新比对最新 head。
- `GFramework.Core.Tests` 项目当前存在独立于本轮改动的 `dotnet format` 基线。
  - 缓解措施：保持为后续单独格式治理切片，不在当前 PR review follow-up 中扩写。

## 下一步

1. 提交本轮 `ai-plan` 同步修复，使 PR head 能重新折叠文档相关线程。
2. 推送后重新执行 `$gframework-pr-review`，确认剩余 open threads 是否已经下降。

## 历史归档指针

- 最新 trace 归档：
  - [analyzer-warning-reduction-history-rp083-rp088.md](../archive/traces/analyzer-warning-reduction-history-rp083-rp088.md)
  - [analyzer-warning-reduction-history-rp073-rp078.md](../archive/traces/analyzer-warning-reduction-history-rp073-rp078.md)
  - [analyzer-warning-reduction-history-rp062-rp071.md](../archive/traces/analyzer-warning-reduction-history-rp062-rp071.md)
- 历史 todo 归档：
  - [analyzer-warning-reduction-history-rp074-rp078.md](../archive/todos/analyzer-warning-reduction-history-rp074-rp078.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/todos/analyzer-warning-reduction-history-rp042-rp048.md)
- 早期归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
