# Analyzer Warning Reduction 追踪

## 2026-04-29 — RP-094

### 阶段：收尾 `YamlConfigSchemaValidator` 剩余 `MA0051` 并将仓库根 clean build 归零

- 触发背景：
  - 用户要求先拿构建 warning，再在 warning 很多时分批指派 subagent；本轮按 `$gframework-batch-boot 50` 继续执行
- 基线与停机判断：
  - 当前 `origin/main` 仍为 `0e32dab`（`2026-04-28T17:15:47+08:00`）
  - 本轮标准仓库根 `dotnet clean` + `dotnet build` 直接成功；warning 总数为 `15`，但全部集中在 `GFramework.Game/Config/YamlConfigSchemaValidator.cs`
  - 由于 `15` 条 warning 实际只对应同一文件内 `5` 个独立 `MA0051` 方法，不满足“warning 非常多且可安全分派多个独立写边界”的条件，因此不再新增 worker
- 主线程实施：
  - 将 `ParseNode` 拆成 `ResolveNodeTypeName`、`ValidateObjectOnlyKeywords`、`CreateParsedNodeForType`
  - 将 `ValidateObjectNode` 拆成对象类型确认、属性遍历与 required 校验 helper
  - 将 `ValidateObjectConstraints` 拆成 property count、`dependentRequired`、`dependentSchemas`、`allOf`、条件分支五个 helper
  - 将 `ValidateScalarNode` 与 `ValidateNumericScalarConstraints` 分别拆成标量类型确认、引用回写、数值上下界和 `multipleOf` helper
  - 追加 `ValidateConditionalSchemaBranch` 收口 if/then/else 分支；随后修正该 helper 引入的 `MA0006`
- 验证里程碑：
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release -clp:Summary`
    - 第一次结果：成功；`3` warnings、`0` errors（均为新 helper 中 `branchName == "then"` 引入的 `MA0006`）
    - 第二次结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests|FullyQualifiedName~YamlConfigSchemaValidatorTests"`
    - 结果：成功；`80` 通过、`0` 失败
  - `dotnet clean`
    - 结果：成功
  - `dotnet build`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `git diff --check`
    - 结果：成功；无新增 whitespace / conflict-marker 问题
- 当前指标：
  - 仓库根 clean build warning：`15` -> `0`
  - 当前分支相对 `origin/main...HEAD` 仍为 `22` 个变更文件，低于 `$gframework-batch-boot 50` 的文件阈值
  - 当前停止原因：warning hotspot 已耗尽，不再有可重复切片
- 下一步：
  - 提交 `YamlConfigSchemaValidator` 收尾重构与本轮 `ai-plan` 真值更新

## 2026-04-29 — RP-093

### 阶段：按 `$gframework-batch-boot 50` 从 clean build warning 基线分批清理

- 触发背景：
  - 用户要求先拿构建 warning，再分批指派 subagent 加快处理；停止条件解析为分支相对 `origin/main` 接近 `50` 个变更文件
- 基线与环境：
  - 当前 `origin/main` 为 `0e32dab`（`2026-04-28T17:15:47+08:00`）
  - 标准 `dotnet clean` 在当前 WSL 环境仍被 Windows NuGet fallback package folder 阻塞；按既有环境口径先执行 `dotnet restore GFramework.sln -p:RestoreFallbackFolders= --disable-parallel` 后，使用 `-p:RestoreFallbackFolders=` 完成 clean / build
  - clean 后 warning 基线：`236` warnings、`0` errors
- 已接受的 worker 范围：
  - `ed269d4`：`GFramework.Cqrs.Tests/Mediator/MediatorArchitectureIntegrationTests.cs`，清理 `MA0048` / `MA0004` / `MA0016`
  - `121df44`：`GFramework.Cqrs.Tests/Mediator/MediatorAdvancedFeaturesTests.cs`，清理 `MA0048` / `MA0004` / `MA0015`
  - `9109eec`：`GFramework.Cqrs.Tests/Mediator/MediatorComprehensiveTests.cs`，清理 `MA0048` / `MA0004` / `MA0016` / `MA0002` / `MA0015`
- 主线程实施：
  - 在 `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 为固定格式正则与 schema `pattern` 正则补充 timeout，避免运行时正则输入继续触发 `MA0009`
  - 将三处字符串等值比较改为 ordinal `string.Equals`，清理 `MA0006`
  - 接受 `1395b84` 的 `YamlConfigSchemaValidator.ObjectKeywords.cs` 方法拆分，清理该文件 `MA0051`
  - 收口被中止 worker 留下的 schema model 拆文件变更，将 `YamlConfigSchemaValidator.cs` 末尾类型移动到同名文件，清理 `MA0048`
- 验证里程碑：
  - `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release -p:RestoreFallbackFolders= -m:1 -nodeReuse:false -clp:Summary`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-build -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~Mediator"`
    - 结果：成功；`45` 通过、`0` 失败
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release -p:RestoreFallbackFolders= -m:1 -nodeReuse:false -clp:Summary`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~YamlConfigLoaderTests|FullyQualifiedName~YamlConfigSchemaValidatorTests"`
    - 结果：成功；`80` 通过、`0` 失败
  - `dotnet clean -p:RestoreFallbackFolders= -v:quiet`
    - 结果：成功
  - `dotnet build -p:RestoreFallbackFolders= -clp:WarningsOnly -v:minimal -m:1 -nodeReuse:false`
    - 中间结果：成功；`75` warnings、`0` errors
  - `dotnet clean -p:RestoreFallbackFolders= -v:quiet`
    - 结果：成功
  - `dotnet build -p:RestoreFallbackFolders= -clp:Summary -v:minimal -m:1 -nodeReuse:false`
    - 结果：成功；`15 Warning(s)`、`0 Error(s)`
  - `git diff --check`
    - 结果：成功；无新增 whitespace / conflict-marker 问题
- 当前指标：
  - warning 总数：`236` -> `15`
  - 剩余 warning 分布：`GFramework.Game/Config/YamlConfigSchemaValidator.cs` 的 `MA0051` `15` 条（5 个方法跨 3 个 TFM）
  - 本轮提交后预计分支 diff：`22` 个文件，低于 `50` 个文件阈值
- 下一步：
  - 按用户要求本轮到此结束；下一轮只处理 `YamlConfigSchemaValidator.cs` 剩余 `MA0051` 方法拆分

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
