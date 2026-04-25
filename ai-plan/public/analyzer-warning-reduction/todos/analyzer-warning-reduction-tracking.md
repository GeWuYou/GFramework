# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-067`
- 当前阶段：`Phase 67`
- 当前焦点：
  - `2026-04-25` 已连续提交 `6a704f3` `fix(analyzer): 固化沙箱外验证并清理测试噪音` 与 `be26640` `test(game-tests): 收敛热重载长方法 warning`
  - 当前单文件批次聚焦 `GFramework.Game/Internal/VersionedMigrationRunner.cs`，通过拆分迁移主循环中的职责收敛根构建直接确认的 `MA0051`
  - 提权后的直接仓库根基线已从 `652 Warning(s)` 降至 `649 Warning(s)`，说明本轮 runtime 单文件批次继续有效
  - `GFramework.Game` 的直接受影响 `Release` build 当前为 `0 Warning(s)`、`0 Error(s)`
  - 本轮只新增 1 个源码唯一文件，branch diff 仍显著低于 `$gframework-batch-boot 50` 阈值

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `4ad880c`（`2026-04-25T14:35:38+08:00`）。
- 提权后的直接仓库根验证当前确认为：
  - `dotnet clean`
    - 结果：成功；此前沙箱内 “Build FAILED but 0 errors” 的 clean 结果不是仓库真值
  - `dotnet build`
    - 最新结果：成功；`649 Warning(s)`、`0 Error(s)`
- 已提交的低风险批次文件：
  - `AGENTS.md`
  - `GFramework.Ecs.Arch.Tests/Ecs/EcsAdvancedTests.cs`
  - `GFramework.Game.Tests/Config/GameConfigBootstrapTests.cs`
  - `GFramework.Game.Tests/Config/GeneratedConfigConsumerIntegrationTests.cs`
  - `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs`
  - `GFramework.Game.Tests/Config/YamlConfigTextValidatorTests.cs`
  - `GFramework.Game/Internal/VersionedMigrationRunner.cs`
  - `ai-plan/public/analyzer-warning-reduction/todos/analyzer-warning-reduction-tracking.md`
  - `ai-plan/public/analyzer-warning-reduction/traces/analyzer-warning-reduction-trace.md`
- 当前批次验证结果：
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 最新主线程结果：成功；`0 Warning(s)`、`0 Error(s)`

## 当前风险

- active `ai-plan` 之外的历史归档仍保留一部分沙箱内 workaround / 假阻塞记录。
  - 缓解措施：active todo/trace 已刷新为新真值；历史归档保留为时间线，不再作为默认恢复入口。
- 当前 worktree 仍存在未跟踪的 `.codex` 目录。
  - 缓解措施：提交时只暂存 analyzer-warning-reduction 相关源码与 `ai-plan` / `AGENTS.md` 变更。
- `GFramework.Core`、`GFramework.Game`、`GFramework.Core.Tests`、`GFramework.Cqrs.Tests` 仍有较大 warning 基线。
  - 缓解措施：后续批次继续优先挑低风险、少文件、可独立验证的测试与局部逻辑切片。

## 活跃文档

- 当前轮次归档：
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/todos/analyzer-warning-reduction-history-rp042-rp048.md)
- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)

## 验证说明

- `dotnet clean`
  - 当前结果：成功；在提权后的直接 shell 中可正常完成仓库根 clean
- `dotnet build`
  - 当前结果：成功；`649 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
  - 当前结果：成功；`0 Warning(s)`、`0 Error(s)`

## 下一步建议

1. 以当前 `649 Warning(s)` 根基线为新恢复点，继续按 `$gframework-batch-boot 50` 规则重算 branch diff，并挑选下一个 1-3 文件的低风险热点。
2. 下一轮优先从 `GFramework.Cqrs.Tests` 或 `GFramework.Game` 中继续选择单文件 `MA0051`、`MA0016` 或测试噪音切片，避免过早推高 review 范围。
3. 后续凡是沙箱内 `.NET` 验证再次出现无诊断失败、pipe/socket 权限问题或与普通 shell 不一致的结果，直接申请沙箱外重跑同一命令，不再扩散 workaround 型命令噪音。
