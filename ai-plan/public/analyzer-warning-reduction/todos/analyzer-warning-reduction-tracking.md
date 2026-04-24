# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-054`
- 当前阶段：`Phase 54`
- 当前焦点：
  - `2026-04-24` 本轮继续按 `$gframework-batch-boot 75` 推进，切入 `GFramework.Game.Tests` 的低风险测试 warning，而不进入 `YamlConfigLoaderTests.cs` 等高上下文热点
  - 已完成 `PersistenceTestUtilities` 的单类型拆分，并在多组 YAML / persistence 测试中补齐 `.ConfigureAwait(false)` 与字段态显式状态检查
  - `GFramework.Game.Tests` 当前 `Release` build 已从本轮入口观测值 `116 warning(s)` 收敛到 `71 warning(s)`，且本轮 touched files 已不再出现在 warning 输出里
  - 当前工作树相对 `origin/main` 的累计 diff 已达到 `76` 个文件、`986` 行变更，超过 `$gframework-batch-boot 75` 的主停止阈值；本轮必须在提交后停止继续扩批

## 当前活跃事实

- 之前记录的 plain `dotnet build` `0 Warning(s)` 属于增量构建假阴性，不能再作为 warning 检查真值
- 本轮直接执行仓库根目录 `dotnet clean` 仍在 `ValidateSolutionConfiguration` 阶段失败，输出未提供具体 error 文本
- 本轮直接执行仓库根目录 `dotnet build GFramework.sln -c Release` 成功，并给出 `116 warning(s)` 的当前整仓入口观测值；其中低风险热点主要落在 `GFramework.Game.Tests`
- `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release` 在本轮收尾验证中为 `71 Warning(s)`、`0 Error(s)`；剩余 warning 已集中在未触碰的 `YamlConfigLoaderTests.cs`、`GeneratedConfigConsumerIntegrationTests.cs`、`GameConfigBootstrapTests.cs`、`ArchitectureConfigIntegrationTests.cs`、`JsonSerializerTests.cs`
- 本轮已验证 `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderIfThenElseTests|FullyQualifiedName~YamlConfigLoaderDependentSchemasTests|FullyQualifiedName~YamlConfigLoaderDependentRequiredTests|FullyQualifiedName~YamlConfigLoaderNegationTests|FullyQualifiedName~YamlConfigLoaderAllOfTests|FullyQualifiedName~YamlConfigLoaderEnumTests|FullyQualifiedName~YamlConfigTextValidatorTests|FullyQualifiedName~YamlConfigSchemaValidatorTests|FullyQualifiedName~PersistenceTests"`，结果为 `Passed: 63`
- `PersistenceTestUtilities.cs` 已拆分为 `TestDataLocation.cs`、`TestSaveData.cs`、`TestVersionedSaveData.cs`、`TestSimpleData.cs`、`TestNamedData.cs`，与仓库“一文件一主类型”风格对齐

## 当前风险

- 如果后续继续依赖增量 `dotnet build`，容易再次把 warning 数量误判为 0
  - 缓解措施：每轮 warning 检查前先执行 `dotnet clean`，再执行目标 `dotnet build`
- 仓库根目录 `dotnet clean` 目前仍然无法给出新的 clean 基线
  - 缓解措施：若下一轮继续做整仓 warning reduction，先定位 `dotnet clean` 的 solution-level / project-level 失败原因，或明确继续沿用用户确认的 `1193 warning(s)` clean 基线与本轮 direct build 观测值
- 当前 worktree 仍存在未跟踪的 `.codex` 目录
  - 缓解措施：提交当前批次时只暂存 analyzer-warning-reduction 相关源码与 `ai-plan` 文件，避免把工作目录辅助文件混入提交
- 当前批次已触发 `$gframework-batch-boot 75` 的主停止条件
  - 缓解措施：本轮提交后停止继续扩批；下一次继续前先评估是否需要基于更新后的 `origin/main` 重新选择基线，或切到新分支 / 新轮次处理剩余 `GFramework.Game.Tests` 热点
- `GFramework.Game.Tests` 的剩余 warning 主要集中在大文件与集成测试文件
  - 缓解措施：后续若继续，优先把 `YamlConfigLoaderTests.cs` 单独作为一个高上下文切片处理，不要和其它 warning family 混批

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

- `dotnet clean GFramework.sln -c Release`
  - 结果：失败；停在 solution `ValidateSolutionConfiguration`，`0 Warning(s)`、`0 Error(s)`，未输出更具体的 error 文本
- `dotnet build GFramework.sln -c Release`
  - 结果：成功；`116 Warning(s)`、`0 Error(s)`
- `dotnet clean GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
  - 结果：失败；clean 阶段在 MSBuild 清理路径结束前返回 `0 Warning(s)`、`0 Error(s)`，未输出额外错误文本
- `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
  - 结果：成功；`71 Warning(s)`、`0 Error(s)`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderIfThenElseTests|FullyQualifiedName~YamlConfigLoaderDependentSchemasTests|FullyQualifiedName~YamlConfigLoaderDependentRequiredTests|FullyQualifiedName~YamlConfigLoaderNegationTests|FullyQualifiedName~YamlConfigLoaderAllOfTests|FullyQualifiedName~YamlConfigLoaderEnumTests|FullyQualifiedName~YamlConfigTextValidatorTests|FullyQualifiedName~YamlConfigSchemaValidatorTests|FullyQualifiedName~PersistenceTests"`
  - 结果：成功；`Passed: 63`、`Failed: 0`

## 下一步建议

1. 提交当前 `GFramework.Game.Tests` warning 清理批次与 `RP-054` tracking 更新，然后停止当前 batch loop，因为 branch diff 已达 `76/75`
2. 下一轮若继续 warning reduction，应先决定是重新整理 `origin/main` 基线，还是单独开一个高上下文批次处理 `YamlConfigLoaderTests.cs`
