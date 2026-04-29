# Analyzer Warning Reduction 跟踪

## 目标

继续以“直接看构建输出、直接修构建 warning”为原则推进当前分支，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-094`
- 当前阶段：`Phase 94`
- 当前焦点：
  - `2026-04-29` 继续按 `$gframework-batch-boot 50` 从仓库根 `dotnet clean` + `dotnet build` 的权威 warning 基线收尾 `YamlConfigSchemaValidator`
  - 本轮 clean build 只剩 `15` 条 warning，但实际只对应 `YamlConfigSchemaValidator.cs` 同一文件中的 `5` 个独立 `MA0051` 热点，因此不再并发派发 worker，避免同文件冲突
  - 已将 `ParseNode`、`ValidateObjectNode`、`ValidateObjectConstraints`、`ValidateScalarNode`、`ValidateNumericScalarConstraints` 按语义拆成 helper，并补齐对象条件分支 helper
  - 当前仓库根 clean build 已收敛到 `0` warnings、`0` errors；本轮停止原因从“接近文件阈值”切换为“当前 warning hotspot 已耗尽”

## 当前活跃事实

- 当前 `origin/main` 基线提交为 `0e32dab`（`2026-04-28T17:15:47+08:00`）。
- 当前直接验证结果：
  - `dotnet clean`
    - 最新结果：成功；标准仓库根 clean 本轮可直接运行，未再命中需要额外绕开的环境噪音
  - `dotnet build`
    - 最新结果：成功；`0 Warning(s)`、`0 Error(s)`；本轮开始时同一口径 clean build 的 `15` 条 warning 已全部清零
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release -clp:Summary`
    - 最新结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests|FullyQualifiedName~YamlConfigSchemaValidatorTests"`
    - 最新结果：成功；`80` 通过、`0` 失败
  - `git diff --check`
    - 最新结果：成功；无新增 whitespace / conflict-marker 问题
- 当前批次摘要：
  - 当前分支提交后预计相对 `origin/main...HEAD` 包含 `22` 个变更文件，低于 `50` 个文件阈值
  - 已完成 worker 切片：
    - `ed269d4`：`MediatorArchitectureIntegrationTests.cs`，清理 `MA0048` / `MA0004` / `MA0016`
    - `121df44`：`MediatorAdvancedFeaturesTests.cs`，清理 `MA0048` / `MA0004` / `MA0015`
    - `9109eec`：`MediatorComprehensiveTests.cs`，清理 `MA0048` / `MA0004` / `MA0016` / `MA0002` / `MA0015`
  - 主线程切片：`YamlConfigSchemaValidator.cs` 方法拆分，清理剩余 `MA0051`，并修正新增 helper 里的 `MA0006`
  - Game 追加切片：
    - `1395b84`：`YamlConfigSchemaValidator.ObjectKeywords.cs`，清理该文件 `MA0051`
    - 已完成：将 `YamlConfigSchemaValidator.cs` 末尾 schema model 类型拆到独立同名文件，清理 `MA0048`

## 当前风险

- 当前仓库根 clean build warning 已清零，本主题暂时没有剩余源码 warning 风险。
  - 缓解措施：若后续继续 batch warning 清理，先重新执行同轮 `dotnet clean` + `dotnet build` 采样，再决定是否需要分派 subagent。

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
- `GFramework.Game` 当前 Release 构建已清零，并通过 config 定向测试；本轮标准仓库根 Debug clean build 也已清零。
- 本轮标准仓库根 `dotnet clean` + `dotnet build` 已直接回到 `0 Warning(s)`、`0 Error(s)`，因此 warning reduction 真值已从模块级验证收口到仓库级 clean build。
- `git diff --check` 结果为空，说明本轮新增改动没有引入新的尾随空格或冲突标记。
- warning reduction 的仓库级真值以同轮 `dotnet build`、定向 `dotnet test` 与 `git diff --check` 为准，并与 trace 中的验证里程碑保持一致。

## 下一步建议

1. 提交 `YamlConfigSchemaValidator` 收尾重构与本轮 `ai-plan` 同步。
2. 如需继续 warning reduction，先从新的仓库根 clean build 重新采样是否还有新增 warning hotspot。
3. 若未来 warning 再次分散到多个文件，再按 `$gframework-batch-boot 50` 规则切换回多 worker 并行模式。
