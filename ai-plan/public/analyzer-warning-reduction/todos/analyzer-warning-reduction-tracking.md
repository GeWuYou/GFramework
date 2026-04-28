# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-088`
- 当前阶段：`Phase 88`
- 当前焦点：
  - `2026-04-28` 已执行 `$gframework-pr-review`，确认 `PR #300` 最新 head 上仍有 `8` 条 CodeRabbit open threads、`1` 个 failed test，以及 `dotnet-format restore failed` 的 CI 噪音
  - 本轮已核对并收敛仍然成立的 review comments：`TestArchitectureContext*` 旧入口显式失败、共享事件总线、`RegisterLifecycleHook` 语义统一、`TestResourceLoader` 契约、`TestLogger` / `CapturingLoggerFactoryProvider` 快照访问、`DeterministicNotificationHandlerState` 并发说明与 `PartialGeneratedNotificationHandlerRegistry` XML 异常文档
  - 已新增 `TestArchitectureContextBehaviorTests.cs`，直接覆盖共享事件总线、旧入口失败契约与接口视角生命周期钩子行为
  - `RegistryInitializationHookBase` 现已在注册表缺失时保持 no-op，修复了 PR 上报的失败测试 `OnPhase_Should_Not_Throw_When_Registry_Not_Found`

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `6cc87a9`（`2026-04-27T20:28:50+08:00`）。
- 当前直接验证结果：
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 最新结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
    - 最新结果：成功；`125 Warning(s)`、`0 Error(s)`；warning 仍集中在既有 `Mediator/*` 文件，不在本轮 PR review 修复写集内
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
    - 最新结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~RegistryInitializationHookBaseTests|FullyQualifiedName~WaitForMultipleEventsTests|FullyQualifiedName~ResourceManagerTests|FullyQualifiedName~LoggerTests|FullyQualifiedName~TestArchitectureContextBehaviorTests"`
    - 最新结果：成功；`97` 通过、`0` 失败
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~CqrsHandlerRegistrarTests"`
    - 最新结果：成功；`11` 通过、`0` 失败
- 当前批次摘要：
  - 当前工作树包含 `11` 个已修改文件和 `1` 个新增测试文件，全部来自 `Core` / `Core.Tests` / `Cqrs.Tests` 的 PR review follow-up
  - 本轮没有触碰 `Mediator/*` 或 `YamlConfigSchemaValidator*` 的高耦合 warning 波次

## 当前风险

- `GFramework.Cqrs.Tests` 当前项目级 Release 构建仍有 `125` 条既有 warning，主要集中在 `MediatorArchitectureIntegrationTests.cs`、`MediatorAdvancedFeaturesTests.cs` 与 `MediatorComprehensiveTests.cs`。
  - 缓解措施：本轮仅记录为现存 blocker，不在 PR #300 的 review follow-up 里扩展到 `Mediator/*` warning reduction 波次。
- `GFramework.Game/Config/YamlConfigSchemaValidator*` 仍然是仓库根 warning 热点，但与本轮 review 修复无交集。
  - 缓解措施：继续保持为独立高耦合波次。

## 活跃文档

- 当前轮次归档：
  - [analyzer-warning-reduction-history-rp074-rp078.md](../archive/todos/analyzer-warning-reduction-history-rp074-rp078.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/todos/analyzer-warning-reduction-history-rp042-rp048.md)
- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp073-rp078.md](../archive/traces/analyzer-warning-reduction-history-rp073-rp078.md)
  - [analyzer-warning-reduction-history-rp062-rp071.md](../archive/traces/analyzer-warning-reduction-history-rp062-rp071.md)
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)

## 验证说明

- 权威验证结果统一维护在“当前活跃事实”。
- `GFramework.Core` 与 `GFramework.Core.Tests` 的当前受影响项目 Release 构建都已清零，并通过对应定向测试回归。
- `GFramework.Cqrs.Tests` 的本轮 helper 改动已由 `CqrsHandlerRegistrarTests` 回归覆盖，但项目级 Release 构建仍暴露 `Mediator/*` 的既有 warning。
- warning reduction 的仓库级真值只以同轮 `dotnet clean` 后的 `dotnet build` 为准。

## 下一步建议

1. 提交本轮 `PR #300` review follow-up 与 `ai-plan` 同步。
2. 若继续处理 `GFramework.Cqrs.Tests` warning，下一轮单独切到 `Mediator/*` 波次，并先接受当前 `125` 条 warning 作为显式基线。
3. `YamlConfigSchemaValidator*` 继续保持为独立高耦合波次，不与 `Mediator/*` 混提。
