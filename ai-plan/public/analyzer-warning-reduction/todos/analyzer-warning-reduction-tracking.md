# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-065`
- 当前阶段：`Phase 65`
- 当前焦点：
  - `2026-04-25` 已确认此前一批 `dotnet clean` / `dotnet build` / `dotnet test` 异常主要来自 agent 沙箱环境，而不是仓库或 WSL 默认 shell 本身
  - 已用提权后的直接命令重新建立仓库根基线：`dotnet clean` 成功，`dotnet build` 结果为 `656 Warning(s)`、`0 Error(s)`
  - 当前 `HEAD` 与 `origin/main` 对齐；基于 `$gframework-batch-boot 50` 的 committed branch diff 现为 `0 files / 0 lines`
- 当前活跃写集是 4 个测试噪音清理文件，属于新的低风险 warning-reduction 批次；提交前需由主线程在沙箱外重新验证
  - 主线程已完成该批次的沙箱外复核，当前可安全并入本轮提交
  - 已决定把“沙箱内 .NET 验证失败时必须申请沙箱外重跑并以该结果为准”写入 `AGENTS.md`，避免后续继续扩散伪环境阻塞

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `4ad880c`（`2026-04-25T14:35:38+08:00`）。
- `fix/analyzer-warning-reduction-batch` 当前 `HEAD` 等于 `origin/main`；因此 shorthand 阈值 `$gframework-batch-boot 50` 的当前 committed 规模为：
  - `git diff --name-only origin/main...HEAD | wc -l`：`0`
  - `git diff --numstat origin/main...HEAD`：`0 added / 0 deleted`
- 提权后的直接仓库根验证已经确认：
  - `dotnet clean`
    - 结果：成功；此前沙箱内 “Build FAILED but 0 errors” 的 clean 结果不是仓库真值
  - `dotnet build`
    - 结果：成功；`656 Warning(s)`、`0 Error(s)`，当前 warning reduction 应以此为总基线
- 当前待集成的低风险批次文件：
  - `GFramework.Game.Tests/Config/GameConfigBootstrapTests.cs`
  - `GFramework.Game.Tests/Config/GeneratedConfigConsumerIntegrationTests.cs`
  - `GFramework.Game.Tests/Config/YamlConfigTextValidatorTests.cs`
  - `GFramework.Ecs.Arch.Tests/Ecs/EcsAdvancedTests.cs`
- 上述批次的 worker 侧验证结果：
  - `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
    - 最新主线程结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Ecs.Arch.Tests/GFramework.Ecs.Arch.Tests.csproj -c Release`
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
  - 当前结果：成功；`656 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
  - 当前结果：成功；`0 Warning(s)`、`0 Error(s)`
- `dotnet build GFramework.Ecs.Arch.Tests/GFramework.Ecs.Arch.Tests.csproj -c Release`
  - 当前结果：成功；`0 Warning(s)`、`0 Error(s)`
- `git diff --name-only origin/main...HEAD | wc -l`
  - 当前结果：`0`
- `git diff --numstat origin/main...HEAD`
  - 当前结果：`0 added / 0 deleted`

## 下一步建议

1. 集成并复核当前 4 文件测试噪音批次后，用沙箱外 `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release` 与 `dotnet build GFramework.Ecs.Arch.Tests/GFramework.Ecs.Arch.Tests.csproj -c Release` 重新确认结果。
2. 下一轮 warning reduction 继续优先处理 `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs` 的长方法切片；它仍是已确认的单文件低风险热点，适合继续用 worker 独立推进。
3. 后续凡是沙箱内 `.NET` 验证再次出现无诊断失败、pipe/socket 权限问题或与普通 shell 不一致的结果，直接申请沙箱外重跑同一命令，不再扩散 workaround 型命令噪音。
