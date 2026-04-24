# Analyzer Warning Reduction 跟踪

## 目标

继续以“优先低风险、保持行为兼容”为原则收敛当前仓库的 Meziantou analyzer warnings，并确保 active recovery 入口保持精简、可恢复。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-042`
- 当前阶段：`Phase 42`
- 当前焦点：
  - 已于 `2026-04-24` 使用 `gframework-pr-review` 复核当前分支 PR #280，latest-head review 仍有 `3` 条 open threads
  - 本地确认这 `3` 条 open threads 均指向 `ai-plan` 文档：错误归档链接、`rp002-rp041` trace 混入 `RP-001` 段落，以及 active trace 的恢复信息失真
  - 本轮按最小写集直接修正文档恢复入口，不再扩大 `GFramework.SourceGenerators.Tests` 的代码写集
  - `RP-041` 验证完成时，分支相对 `origin/main` 的唯一变更文件数为 `4`；这说明继续只处理同一热点文件时，该指标增长会很慢
  - `GFramework.SourceGenerators.Tests` 在 `RP-042` 的 `net10.0` Release build 中仍为 `10` 条 `MA0051` warning、`0` error；剩余热点继续集中在 `CqrsHandlerRegistryGeneratorTests.cs`

## 当前状态摘要

- 已修正 `archive/todos/analyzer-warning-reduction-history-rp002-rp041.md` 中指向 `RP-001` 归档的相对链接，恢复历史入口可点击性
- 已从 `archive/traces/analyzer-warning-reduction-history-rp002-rp041.md` 中移除误混入的 `RP-001` 段落，确保文件名与内容范围一致
- 已刷新 active tracking / trace 的恢复点描述，使其反映当前仍待远端收敛的是文档类 review threads，而不是已处理过的代码项
- PR #280 的 MegaLinter 仍显示 `dotnet-format` warning，但测试报告为 `2156 passed / 0 failed`；该 warning 目前更像 CI 环境 restore / SDK 噪音，而不是本地代码行为回归

## 当前活跃事实

- 当前主题仍保持 active，因为 `GFramework.SourceGenerators.Tests` 尚有剩余 `MA0051` warning 需要决定是否继续推进
- 继续按“单文件单方法”节奏处理 `CqrsHandlerRegistryGeneratorTests.cs` 可以稳定消除 warning，但不利于快速提高唯一变更文件数
- 当前 PR review 已没有新的 failed-test 信号；当前优先级是提交这轮 `ai-plan` 修正并等待远端 PR threads 收敛

## 当前风险

- warning 治理策略风险：如果用户仍以“唯一变更文件数接近 `75`”作为目标，继续深挖同一测试文件会让目标推进缓慢
  - 缓解措施：下一轮先确认是继续压低 `MA0051` 基线，还是切换到新的文件写集
- WSL 构建环境风险：当前 worktree 的 .NET 定向验证仍需显式附带 `-p:RestoreFallbackFolders=`，并在沙箱外运行以规避命名管道 / socket 限制
  - 缓解措施：后续所有 affected-project Release build 继续复用该参数组合
- source generator test warning 范围风险：一旦继续触达 `GFramework.SourceGenerators.Tests`，剩余 warning 会继续成为本轮完成条件的一部分
  - 缓解措施：继续用最小写集和 warnings-only build 锁定范围

## 活跃文档

- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)

## 验证说明

- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders=""`
  - 结果：通过；重写了受 Windows fallback package folder 影响的测试项目资产文件
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
  - 结果：`10 Warning(s)`，`0 Error(s)`；warning 仍全部来自 `CqrsHandlerRegistryGeneratorTests.cs` 的既有 `MA0051` 基线

## 下一步建议

1. 提交 `RP-042` 后重新抓取 PR #280 review，确认这 `3` 条 latest-head open threads 是否随新提交收敛
2. 若 PR threads 收敛，再决定下一轮是继续清理 `CqrsHandlerRegistryGeneratorTests.cs` 的剩余 `MA0051`，还是切换到新的文件写集
3. 如果仍要继续沿用“唯一变更文件数接近 `75`”的目标，应优先切到新的 warning 写集，而不是继续深挖同一测试文件
