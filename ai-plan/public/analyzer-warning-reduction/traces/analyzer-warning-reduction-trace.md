# Analyzer Warning Reduction 追踪

## 2026-04-24 — RP-042

### 阶段：PR #280 review follow-up 与 ai-plan 恢复入口修正

- 启动复核：
  - 使用 `gframework-pr-review` 抓取当前分支 PR #280 的 latest-head review threads、MegaLinter 摘要与测试报告
  - 本地核对后确认 `3` 条 open threads 均仍成立，但全部集中在 `ai-plan` 文档恢复入口，而不是新的代码行为问题
- 决策：
  - 不再继续扩大 `GFramework.SourceGenerators.Tests` 的写集，先把远端 latest-head review 中仍成立的文档问题全部收口
  - 保持 `RP-042` 作为 active recovery point，仅刷新其事实描述、归档链接和 trace 范围边界
- 实施调整：
  - 修正 `archive/todos/analyzer-warning-reduction-history-rp002-rp041.md` 中两条指向 `RP-001` 归档的相对链接
  - 从 `archive/traces/analyzer-warning-reduction-history-rp002-rp041.md` 中移除误混入的 `RP-001` 段落，使文件只保留 `RP-002` 到 `RP-041`
  - 刷新 active tracking / trace 的恢复点描述，明确当前 open threads 已收敛为文档问题，并记录本轮 follow-up 的事实与下一步
- 验证结果：
  - `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`10 Warning(s)`，`0 Error(s)`；warning 仍全部来自 `CqrsHandlerRegistryGeneratorTests.cs` 的既有 `MA0051` 基线
- 当前结论：
  - PR #280 当前没有 failed-test 回归信号；latest-head review 剩余项可以全部在 `ai-plan` 范围内处理
  - active 恢复入口与历史归档范围已重新对齐，后续 `boot` 不会再从 `rp002-rp041` 误读 `RP-001`
- 下一步建议：
  - 提交后重新抓取 PR #280 review，确认 open threads 是否收敛
  - 若 threads 收敛，则回到 `CqrsHandlerRegistryGeneratorTests.cs` 剩余 `MA0051`，或根据目标改切新的 warning 写集

## Archive Context

- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
