# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-080`
- 当前阶段：`Phase 80`
- 当前焦点：
  - `2026-04-27` 已补齐 `$gframework-pr-review` 对 GitHub Test Reporter / CTRF 测试摘要与失败用例详情的提取，当前文本输出可以直接显示失败测试名和 failure message 摘要
  - `SettingsModelTests.RegisterMigration_During_Cache_Rebuild_Should_Not_Leave_Stale_Type_Cache` 已按 `System.Threading.Lock` 的真实语义修正测试实现，并完成针对性 Release 测试验证
  - 当前剩余 warning 热点仍集中在 `YamlConfigSchemaValidator*`、`YamlConfigLoader.cs` 与大批量 `MA0048` 文件名拆分；这些 slice 仍高于本轮 PR review / test follow-up 的低风险边界

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `617e0bf`（`2026-04-26T12:17:15+08:00`）。
- 当前 PR review 真值：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output <current-pr-review-json>`
    - 最新结果：成功；当前分支对应 PR 为 `#295`
    - 当前测试报告输出已能显示 `Summary` 统计、失败测试名称，以及 `Name / Failure Message` 表格中的关键信息
- 当前直接验证结果：
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~RegisterMigration_During_Cache_Rebuild_Should_Not_Leave_Stale_Type_Cache"`
    - 最新结果：成功；`Failed: 0, Passed: 1, Skipped: 0, Total: 1`
- 当前分支 stop-condition 指标：
  - `git diff --name-only refs/remotes/origin/main...HEAD | wc -l`
    - 最新结果：`30`
  - `git diff --numstat refs/remotes/origin/main...HEAD`
    - 最新结果：`642` changed lines
- 当前批次摘要：
  - 三轮低风险 warning 清理已在此前验证中将仓库根 warning 从 `639` 降到 `397`
  - 当前批次的已完成 slice 明细已迁移到归档，active todo 仅保留恢复真值
  - 本轮新增内容为 PR review 工具链补强与单个失败测试修正，不扩展 warning reduction 的热点清理边界
- 当前建议保留到下一波次的候选：
  - `GFramework.Game/Config/YamlConfigLoader.cs` 的 `MA0158`（单点可修，但文件本身同时承载其他高耦合 warning）
  - 测试项目中的 `MA0048` 文件名拆分波次（会显著增加 changed-file 数）

## 当前风险

- `GFramework.Game/Config/YamlConfigSchemaValidator*.cs` 仍然聚集多类高耦合 warning。
  - 缓解措施：本轮先避开该热点，只清理低风险且 ownership 清晰的文件集合。
- `MA0158` 迁移涉及 `net8.0` / `net9.0` / `net10.0` 多目标兼容。
  - 缓解措施：复用 `StoreSelection.cs` 已存在的 `#if NET9_0_OR_GREATER` 专用锁模式，不在 `net8.0` 引入不兼容 API。
- 当前 PR open thread 与 CI 失败信号仍依赖新提交进入远端 PR head 才能复核。
  - 缓解措施：本轮提交后重新执行 `$gframework-pr-review`，同时确认 review thread 与 failed test signal 是否一起收口。

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
- `GFramework.Core.Tests` 当前仍有既有 analyzer / nullable warning 基线，因此本轮验证只证明 PR review 修复未引入构建错误，未将该项目 warning 清零。
- 后续若刷新构建或 PR review 真值，只更新上述权威区块，不在本节重复抄录。

## 下一步建议

1. 提交本轮 `$gframework-pr-review` 解析增强、`SettingsModelTests` 修复与 `ai-plan` 同步。
2. 推送后重新执行 `$gframework-pr-review`，确认 PR `#295` 的 failed test detail 与 open thread 是否已更新为新 head 真值。
3. 若后续继续推进 warning reduction，建议另开下一波次处理 `YamlConfigLoader.cs` 热点或测试项目 `MA0048` 拆分波次。
