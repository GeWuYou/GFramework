# Data Repository Persistence 历史追踪

> 说明：旧 `local-plan` 没有独立 trace，而是由
> `local-plan/settings-persistence-serialization-tracking.md` 同时承担 tracking / trace 职责。
> 本文件基于该混合材料整理，只保留可稳定确认的事实、决策、验证与 backlog，不补写无法从原文推导的逐时序细节。

## 历史阶段：设置 / 持久化 / 序列化第一轮收敛（pre-RP001）

### 关键目标

- 修正文档与当前实现不一致的问题
- 补足关键直接测试，避免只靠侧面覆盖
- 修复本轮实现中暴露出来的真实缺陷
- 为后续更大的存档迁移 / 持久化策略统一保留清晰恢复点

### 已确认落地的实现决策

- 为 `JsonSerializer` 补齐实例级配置入口，并保留 `Serialize(object, Type)` 在 `null` 场景下输出 `"null"` 的兼容语义
- 将 `DataRepository` 与 `UnifiedSettingsDataRepository` 的批量保存语义收口到批量提交事件，避免重复发送单项保存事件
- 让 `DataRepository` / `UnifiedSettingsDataRepository` 在批量保存和加载时保留运行时数据类型与泛型事件类型，避免回退到
  `IData`
- 为 `SettingsModel` 的动态注册、applicator 绑定与 migration cache 失效补齐直接语义
- 为 `UnifiedSettingsDataRepository` 引入“快照 -> 持久化 -> 交换缓存”的提交模型，避免失败写入污染已提交缓存
- 为 `ISaveRepository<T>` / `SaveRepository<T>` 建立正式的 `ISaveMigration<T>` 迁移链能力，并为注册表并发访问补同步保护

### 已确认落地的验证

- 已新增 `JsonSerializer`、`SettingsModel`、`SettingsSystem`、`Persistence` 等直接测试
- 已验证 `SaveRepository<T>` 迁移链、重复注册、缺失迁移链、批量事件、自动备份、运行时类型覆盖与缓存一致性回归
- 已同步更新 `docs/zh-CN/game/setting.md`、`docs/zh-CN/game/data.md` 与 `docs/zh-CN/game/index.md`
- 原始记录中的测试命令包括：
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj --filter "JsonSerializerTests"`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj --filter "JsonSerializerTests|SettingsModelTests|PersistenceTests"`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "PersistenceTests"`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "PersistenceTests|SettingsSystemTests"`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "PersistenceTests|SettingsSystemTests|SettingsModelTests"`
- 原始记录确认的结果包括：
  - `JsonSerializerTests`：通过
  - `JsonSerializerTests | SettingsModelTests | PersistenceTests`：12 个通过
  - `PersistenceTests`（Release）：7 个通过
  - `PersistenceTests | SettingsSystemTests`（Release）：16 个通过，补充回归后为 19 个通过
  - `PersistenceTests | SettingsSystemTests | SettingsModelTests`（Release）：18 个通过，补充回归后为 21 个通过
  - `GFramework.Game.Tests` 全量（Release）：89 个通过

### 从历史材料中确认的真实问题

- `UnifiedSettingsDataRepository.SaveAsync(...)` 在未加载状态下可能发生自锁死锁，已修复
- `UnifiedSettingsDataRepository` 持久化失败时会让缓存领先于磁盘状态，已改为快照提交模型
- `SaveRepository<T>` 迁移注册表缺少并发保护且会静默覆盖重复 `FromVersion` 注册，已改为加锁并显式拒绝重复注册

### 留存 backlog

- P1：评估 `JsonSerializer` 的只读配置、线程安全与实例级 converter 使用说明是否需要进一步补强
- P2：评估设置、存档、通用仓库的版本迁移模型是否应进一步统一抽象
- P2：评估压缩 / 加密 / 元数据策略是否应抽成更明确的 codec / persistence pipeline

### 恢复边界

- 这份历史追踪来自单文件混合材料，不包含独立的逐日执行日志
- 后续如需继续该主题，应以 active tracking / trace 为恢复入口，再回看本文件的稳定历史结论
