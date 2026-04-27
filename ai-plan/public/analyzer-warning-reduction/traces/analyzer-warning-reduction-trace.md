# Analyzer Warning Reduction 追踪

## 2026-04-27 — RP-079

### 阶段：按 PR `#295` latest-head review 收口当前仍成立的 open thread

- 触发背景：
  - 重新执行 `$gframework-pr-review` 后，确认当前分支对应的最新公开 PR 为 `#295`，而不是旧 trace 中记录的 `#291`
  - latest-head review 仍有 `2` 条 open thread，分别指向 `GFramework.Core.Tests/Extensions/AsyncExtensionsTests.cs` 的参数名契约问题，以及 active trace 过长问题
- 主线程实施：
  - 修正 `ThrowShouldNotRetry` 中 `ArgumentException` 的 `ParamName` 传递逻辑，并补充断言锁定 `nameof(taskFactory)` 契约
  - 将 active trace 精简为单一恢复入口，并把 `RP073` 到 `RP078` 的详细过程迁入归档
  - 同步压缩 active todo，只保留当前恢复点真值与归档指针
- 验证里程碑：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output <current-pr-review-json>`
    - 结果：成功；确认 PR `#295` latest-head review 在本轮修复前共有 `2` 条 open thread
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`126 Warning(s)`、`0 Error(s)`
    - 当前可见的既有 warning ID：`CS8766`、`CS8625`、`CS8602`、`CS8618`、`MA0048`、`MA0002`、`MA0008`
- 当前结论：
  - 当前仍成立的 latest-head review 问题已经在本地收口，下一步应以新提交刷新 PR head 再复查
  - active `ai-plan` 入口现已回到“当前真值 + 归档指针”的体量，符合仓库对恢复文档的要求

## 活跃风险

- PR review thread 是否自动关闭仍取决于新提交是否进入远端 PR head。
  - 缓解措施：提交后重新执行 `$gframework-pr-review`，只以新的 latest-head 结果为准。
- `YamlConfigSchemaValidator*`、`YamlConfigLoader.cs` 与 `MA0048` 拆分仍是下一波次的高耦合候选。
  - 缓解措施：保持本轮边界只处理 PR review 收口，不顺手扩展 warning reduction 范围。

## 下一步

1. 完成本轮验证并提交。
2. 重新执行 `$gframework-pr-review`，确认 PR `#295` latest-head 是否还有 unresolved thread。

## 历史归档指针

- 最新 trace 归档：
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
