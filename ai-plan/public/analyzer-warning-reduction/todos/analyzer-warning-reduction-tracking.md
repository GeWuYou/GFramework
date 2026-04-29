# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-095`
- 当前阶段：`Phase 95`
- 当前焦点：
  - `2026-04-29` 继续处理 `PR #301` 的 latest-head review threads，只修复当前工作树上仍然成立的问题
  - 已修复 `MediatorArchitectureIntegrationTests` 中仍然成立的并发与阻塞问题：移除冗余分支、把 `Task.Delay().Wait()` 改为 `await`、把静态缓存换成 `ConcurrentDictionary`、并把共享计数更新改成原子操作
  - 已补 `GFramework.Game/Config` 运行时 schema 模型的构造期契约校验与 `<exception>` XML 文档，并新增 `YamlConfigModelContractTests` 锁定这些无效状态保护
  - 本轮明确暂不接受两个误报方向：`YamlConfigReferenceUsage.DisplayPath` 别名删除建议，以及两个本地枚举补 `[GenerateEnumExtensions]` 的泛化建议

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `0e32dab`（`2026-04-28T17:15:47+08:00`）。
- 当前直接验证结果：
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release -clp:Summary`
    - 最新结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigSchemaValidatorTests|FullyQualifiedName~YamlConfigModelContractTests"`
    - 最新结果：成功；`10` 通过、`0` 失败
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~MediatorArchitectureIntegrationTests|FullyQualifiedName~MediatorAdvancedFeaturesTests"`
    - 最新结果：成功；`25` 通过、`0` 失败
  - `git diff --check`
    - 最新结果：成功；无新增 whitespace / conflict-marker 问题
- 当前批次摘要：
  - 当前切片直接修改 `12` 个已有文件，并新增 `YamlConfigModelContractTests.cs` 作为模型契约回归覆盖
  - 本轮修复集中在 `GFramework.Cqrs.Tests` 与 `GFramework.Game` 两个最新 review thread 热区，没有再扩写回 warning-batch 的多文件并发清理范围
  - PR review triage 结论：
    - 接受：并发共享状态、阻塞等待、无效约束状态、缺失 `<exception>` 文档
    - 延后：`DisplayPath` 诊断别名删除建议
    - 驳回：两个枚举补 `[GenerateEnumExtensions]` 的泛化建议

## 当前风险

- 当前 GitHub PR 仍会保留尚未推送折叠的 open threads，以及被明确延后 / 驳回的机器人建议。
  - 缓解措施：提交并推送后重新执行 `$gframework-pr-review`，只保留仍有真实依据的剩余线程。
- 本轮未重跑仓库根 `dotnet clean` + `dotnet build`，因此 RP-094 的仓库级 warning 真值不能直接外推到这次 PR-review follow-up 之后。
  - 缓解措施：若下一轮重新回到 analyzer warning reduction 主线，先按仓库规则重新采样仓库根 clean build。

## 活跃文档

- 当前轮次归档：
  - [analyzer-warning-reduction-history-rp083-rp088.md](../archive/traces/analyzer-warning-reduction-history-rp083-rp088.md)
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
- `GFramework.Game` 当前 Release 构建已清零，并通过 config 定向测试。
- `GFramework.Cqrs.Tests` 当前 PR-review follow-up 定向测试通过，说明并发/缓存测试辅助实现的行为修正没有破坏现有集成断言。
- `git diff --check` 结果为空，说明本轮新增改动没有引入新的尾随空格或冲突标记。
- 本轮以受影响项目的 Release build / tests 为完成条件；若下轮恢复 warning reduction 仓库级真值，需要重新执行仓库根 `dotnet clean` + `dotnet build`。

## 下一步建议

1. 提交当前 PR-review follow-up 与本轮 `ai-plan` 同步。
2. 推送分支后重新执行 `$gframework-pr-review`，确认剩余 open threads 是否只剩延后 / 误报项。
3. 若下一轮恢复 warning reduction 主线，先重新执行仓库根 `dotnet clean` + `dotnet build` 建立新的权威基线。
