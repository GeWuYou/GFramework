# Analyzer Warning Reduction 跟踪归档（RP074-RP078）

## 范围

- 归档 `RP074` 到 `RP078` 期间从 active todo 中迁出的批次明细。
- 保留当前波次的已完成 slice 摘要、验证收口与延后候选，供后续恢复时回溯。

## 已完成批次摘要

- 第一轮并行 warning 清理：
  - `GFramework.Core` 事件 / 状态 / 属性 / 协程统计中的 `MA0158` 专用锁迁移
  - `GFramework.Game/Data` 中 `DataRepository`、`UnifiedSettingsDataRepository`、`SaveRepository` 的 `ConfigureAwait` / 比较器 / 专用锁修正
  - `GFramework.Game/Scene/SceneRouterBase.cs` 与 `GFramework.Game/UI/UiRouterBase.cs` 中的显式上下文 / 参数名 / 比较器修正
  - 收口提交：`fb0a55f` `fix(analyzer): 收口首轮并行警告清理`
- 第三轮 `Core.Tests` 低风险 slice：
  - `GFramework.Core.Tests/Concurrency/AsyncKeyLockManagerTests.cs` 的 `MA0004`
  - `GFramework.Core.Tests/Pause/PauseStackManagerTests.cs` 的 `MA0158`
  - `GFramework.Core.Tests/Extensions/AsyncExtensionsTests.cs` 的 `MA0015`
  - `GFramework.Core.Tests/Architectures/ArchitectureModulesBehaviorTests.cs` 的 `MA0004`

## 批次验证快照

- `dotnet clean`
  - 结果：提权直接执行成功，确认为当前权威 clean 基线
- `dotnet build`
  - 结果：提权直接构建成功；warning 从 `639` 降到 `397`
- `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：提权直接构建成功；`0 Warning(s)`、`0 Error(s)`

## 延后候选

- `GFramework.Game/Config/YamlConfigLoader.cs` 的 `MA0158`
  - 原因：单点可修，但文件同时承载其他高耦合 warning，不适合在当前低风险批次顺手推进
- 测试项目中的 `MA0048` 文件名拆分波次
  - 原因：会显著增加 changed-file 数，更适合另开后续波次

## 关联资料

- 详细执行过程见 [analyzer-warning-reduction-history-rp073-rp078.md](../traces/analyzer-warning-reduction-history-rp073-rp078.md)。
