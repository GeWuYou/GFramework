# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-079`
- 当前阶段：`Phase 79`
- 当前焦点：
  - `2026-04-27` 已按 PR `#295` 的 latest-head review 收口本地仍成立的问题：`AsyncExtensionsTests` 的 `ArgumentException.ParamName` 契约与 active `ai-plan` 文档过长问题
  - 当前轮次已重新确认 `origin/main` 基线与 `HEAD` 同为 `617e0bf`；当前已提交 `HEAD` 的 stop metric 仍为 `30 / 50` files、`642` changed lines，本轮 PR review 同步尚未提交入该指标
  - 当前剩余 warning 主要集中在 `YamlConfigSchemaValidator*`、`YamlConfigLoader.cs` 与大批量 `MA0048` 文件名拆分；这些 slice 仍高于本轮 PR review 收口的低风险边界

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `617e0bf`（`2026-04-26T12:17:15+08:00`）。
- 当前 PR review 真值：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output <current-pr-review-json>`
    - 最新结果：成功；当前分支对应 PR 为 `#295`
    - latest-head review 在本轮修复前共有 `2` 条 open thread，分别指向 `GFramework.Core.Tests/Extensions/AsyncExtensionsTests.cs` 与 active `trace`
  - 本轮已在本地收口上述两类问题，待提交后可重新执行 `$gframework-pr-review` 确认线程自动收口
- 提权后的直接验证当前确认为：
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 最新结果：成功；`126 Warning(s)`、`0 Error(s)`
    - 当前仍可见的既有 warning ID：`CS8766`、`CS8625`、`CS8602`、`CS8618`、`MA0048`、`MA0002`、`MA0008`
- 当前分支 stop-condition 指标：
  - `git diff --name-only refs/remotes/origin/main...HEAD | wc -l`
    - 最新结果：`30`
  - `git diff --numstat refs/remotes/origin/main...HEAD`
    - 最新结果：`642` changed lines
- 当前批次摘要：
  - 三轮低风险 warning 清理已在此前验证中将仓库根 warning 从 `639` 降到 `397`
  - 当前批次的已完成 slice 明细已迁移到归档，active todo 仅保留恢复真值
- 当前建议保留到下一波次的候选：
  - `GFramework.Game/Config/YamlConfigLoader.cs` 的 `MA0158`（单点可修，但文件本身同时承载其他高耦合 warning）
  - 测试项目中的 `MA0048` 文件名拆分波次（会显著增加 changed-file 数）

## 当前风险

- `GFramework.Game/Config/YamlConfigSchemaValidator*.cs` 仍然聚集多类高耦合 warning。
  - 缓解措施：本轮先避开该热点，只清理低风险且 ownership 清晰的文件集合。
- `MA0158` 迁移涉及 `net8.0` / `net9.0` / `net10.0` 多目标兼容。
  - 缓解措施：复用 `StoreSelection.cs` 已存在的 `#if NET9_0_OR_GREATER` 专用锁模式，不在 `net8.0` 引入不兼容 API。
- 当前 PR open thread 的自动收口仍依赖新提交进入远端 PR head。
  - 缓解措施：本轮提交后重新执行 `$gframework-pr-review`，仅以最新 head review 为准。

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

1. 提交本轮 PR review 修复与 `ai-plan` 精简同步，再重跑 `$gframework-pr-review` 验证 PR `#295` 的 open thread 是否随新 head 收口。
2. 若后续继续推进 warning reduction，建议另开下一波次，优先明确是否接受 `YamlConfigLoader.cs` 热点触碰，或是否要专门做测试项目 `MA0048` 拆分波次。
3. 默认在当前恢复点停下，因为继续推进已不再符合本轮“PR review 收口 + 少文件同步”的边界。
