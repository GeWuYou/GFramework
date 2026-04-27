# Analyzer Warning Reduction 追踪

## 2026-04-27 — RP-080

### 阶段：补强 `$gframework-pr-review` 的测试报告提取并修复 `SettingsModelTests` 失败用例

- 触发背景：
  - 用户补充了 GitHub Test Reporter / CTRF 的完整 PR 评论，指出现有 `$gframework-pr-review` 输出虽然能显示 `failed=1`，但没有抓到失败测试名称与详细报错
  - PR 评论中的真实失败用例为 `RegisterMigration_During_Cache_Rebuild_Should_Not_Leave_Stale_Type_Cache`，报错集中在测试通过反射持锁后两个任务提前完成
- 主线程实施：
  - 扩展 `.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py` 的 `parse_test_report`，增加 CTRF `Summary` 统计、紧凑 failed-tests 列表、以及 `Name` / `Failure Message` HTML 表格的提取
  - 更新 `gframework-pr-review` skill 文档的输出预期，明确脚本现在应提取 GitHub Test Reporter / CTRF 的失败详情
  - 修正 `GFramework.Game.Tests/Setting/SettingsModelTests.cs`，让反射取得的 `_migrationMapLock` 在 `NET9_0_OR_GREATER` 下通过 `System.Threading.Lock.EnterScope()` 持锁，而不是退化成 `Monitor` 语义
- 验证里程碑：
  - `python3 -c "... parse_test_report(...)"`（基于 `/tmp/current-pr-review.json` 的现有原始评论）
    - 结果：成功；已能解析 `tests=2156`、`passed=2155`、`failed=1`、`duration=35.3s`，并抓到 `RegisterMigration_During_Cache_Rebuild_Should_Not_Leave_Stale_Type_Cache` 的 failure message
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --section tests --json-output /tmp/current-pr-review-v2.json`
    - 结果：成功；文本输出已直接显示失败测试名与报错摘要
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~RegisterMigration_During_Cache_Rebuild_Should_Not_Leave_Stale_Type_Cache"`
    - 结果：成功；`Failed: 0, Passed: 1, Skipped: 0, Total: 1`
- 当前结论：
  - `$gframework-pr-review` 现在已经能把 PR 测试评论里的关键失败用例信息直接提出来，不再只停留在 `failed=1`
  - `SettingsModelTests` 当前失败是测试锁语义与生产代码不一致导致的误判，已在本地复现并修正通过

## 活跃风险

- PR 上的 latest-head review thread 与测试报告仍需要等新提交进入远端后再复核。
  - 缓解措施：提交并推送后重新执行 `$gframework-pr-review`，只以新的 latest-head 和 test report 为准。
- `YamlConfigSchemaValidator*`、`YamlConfigLoader.cs` 与 `MA0048` 拆分仍是下一波次的高耦合候选。
  - 缓解措施：保持本轮边界只处理 PR review 工具链与失败测试，不顺手扩展 warning reduction 范围。

## 下一步

1. 完成本轮提交。
2. 推送后重新执行 `$gframework-pr-review`，确认 PR `#295` 的 failed test detail 与 unresolved thread 是否已刷新。

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
