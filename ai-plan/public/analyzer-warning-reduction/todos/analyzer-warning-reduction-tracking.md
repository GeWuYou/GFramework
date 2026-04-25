# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-072`
- 当前阶段：`Phase 72`
- 当前焦点：
  - `2026-04-25` 主线程再次按 `$gframework-pr-review` 复核当前分支 PR `#291`，确认 latest-head 仍剩 `1` 条 open review thread 与 `1` 条 nitpick，均指向 active `ai-plan` 文档
  - 当前批次只同步 active todo/trace 到最新 review 真值：压缩 active trace，并把重复维护的验证结果改回单一权威区块
  - `dotnet clean` + `dotnet build` 的直接仓库根基线仍为 `639 Warning(s)`、`0 Error(s)`，因此本轮属于文档真值收口，而不是新的 warning 清理批次

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `4ad880c`（`2026-04-25T14:35:38+08:00`）。
- 提权后的直接仓库根验证当前确认为：
  - `dotnet clean`
    - 结果：成功；此前沙箱内 “Build FAILED but 0 errors” 的 clean 结果不是仓库真值
  - `dotnet build`
    - 最新结果：成功；`639 Warning(s)`、`0 Error(s)`
- 当前分支低风险批次文件：
  - `AGENTS.md`
  - `.gitignore`
  - `GFramework.Core.Tests/Logging/LogContextTests.cs`
  - `GFramework.Core.Tests/Logging/LoggerTests.cs`
  - `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs`
  - `GFramework.Cqrs.Tests/Logging/TestLogger.cs`
  - `GFramework.Cqrs.Tests/Mediator/MediatorAdvancedFeaturesTests.cs`
  - `GFramework.Ecs.Arch.Tests/Ecs/EcsAdvancedTests.cs`
  - `GFramework.Game.Tests/Config/GameConfigBootstrapTests.cs`
  - `GFramework.Game.Tests/Config/GeneratedConfigConsumerIntegrationTests.cs`
  - `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs`
  - `GFramework.Game.Tests/Config/YamlConfigTextValidatorTests.cs`
  - `GFramework.Game/Internal/VersionedMigrationRunner.cs`
  - `ai-plan/public/analyzer-warning-reduction/todos/analyzer-warning-reduction-tracking.md`
  - `ai-plan/public/analyzer-warning-reduction/traces/analyzer-warning-reduction-trace.md`
  - `ai-plan/public/analyzer-warning-reduction/archive/traces/analyzer-warning-reduction-history-rp062-rp071.md`
- 当前批次验证结果：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json`
    - 最新主线程结果：成功；确认 PR `#291` latest-head open review thread 为 `1`、CodeRabbit nitpick 为 `1`，两者都指向 active `ai-plan` 文档入口
  - `dotnet build`
    - 最新主线程结果：成功；`0 Warning(s)`、`0 Error(s)`；该次为增量 Debug 构建，只作为本轮文档同步完成校验，warning 权威基线仍以 `dotnet clean` 后的 `639 Warning(s)` 为准
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 最新主线程结果：成功；`326 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
    - 最新主线程结果：成功；`149 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 上一轮主线程结果：成功；`0 Warning(s)`、`0 Error(s)`

## 当前风险

- `GFramework.Core`、`GFramework.Game`、`GFramework.Core.Tests`、`GFramework.Cqrs.Tests` 仍有较大 warning 基线。
  - 缓解措施：后续批次继续优先挑低风险、少文件、可独立验证的测试与局部逻辑切片。
- 当前 review 相关真值要等新 head 推送后才能在 GitHub UI 中自动收口。
  - 缓解措施：本轮提交后立即重新执行 `$gframework-pr-review`，确认 PR `#291` 的 latest-head thread 与 nitpick 是否消失。

## 活跃文档

- 当前轮次归档：
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/todos/analyzer-warning-reduction-history-rp042-rp048.md)
- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp062-rp071.md](../archive/traces/analyzer-warning-reduction-history-rp062-rp071.md)
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)

## 验证说明

- 权威验证结果统一维护在“当前活跃事实”和“当前批次验证结果”。
- 后续若刷新构建或 PR review 真值，只更新上述权威区块，不在本节重复抄录。

## 下一步建议

1. 推送包含本轮 active todo/trace 同步的提交后，重新执行 `$gframework-pr-review`，确认 PR `#291` 的 latest-head open thread 与 nitpick 是否已自动收口。
2. 若 PR `#291` 已清零，继续以当前 `639 Warning(s)` 根基线为恢复点，按 `$gframework-batch-boot 50` 规则挑选下一个 1-3 文件的低风险热点。
3. 若 GitHub 仍保留 review 信号，先确认它们是否指向新 head，再决定是否需要继续细化 active `ai-plan` 入口。
