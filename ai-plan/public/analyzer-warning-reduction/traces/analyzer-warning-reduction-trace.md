# Analyzer Warning Reduction 追踪

## 2026-04-24 — RP-047

### 阶段：solution warning 基线复核与 active plan 去噪

- 触发背景：
  - 用户要求继续按 `$gframework-batch-boot 75` 推进，并明确要求“通过 `dotnet build` 检查警告”
  - 用户追加要求清理当前计划中的噪音内容，因此本轮除了复核 warning 基线，还要同步压缩 active todo / trace
- 主线程实施：
  - 先读取 active topic 文档、基线信息与 branch diff 指标，确认 baseline 仍是 `origin/main`（`e692ed3`）
  - 复查当前工作树中的 warning-reduction 切片，确认主要未提交修改集中在 `GFramework.Game`、`GFramework.Godot`、`GFramework.SourceGenerators.Tests`
  - 执行 `dotnet build GFramework.Game/GFramework.Game.csproj -c Release` 与 `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`，二者均成功
  - 发现默认 terminal logger 输出不利于读取 warning 数，因此改用 `dotnet build GFramework.sln -c Release -tl:off -nologo`
  - solution Release build 在经典 logger 形态下成功完成，结果为 `0 Warning(s)` / `0 Error(s)` / `Time Elapsed 00:00:12.72`
  - 基于该真值，压缩 active todo / trace，移除已经过期的 `891 warnings` 旧基线和过多执行形态细节
- 当前结论：
  - 当前工作树的 solution warning 基线已经降到 `0 Warning(s)`；active plan 中旧的高噪音 warning 基线不再适合作为恢复入口
  - `-tl:off` 是当前最可靠的 warning 采样入口；默认 terminal logger 更适合看进度，不适合记录计数
  - 当前批次的主要剩余工作不再是继续找 warning，而是整理并提交现有切片，避免 reviewability 下降

## Archive Context

- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
