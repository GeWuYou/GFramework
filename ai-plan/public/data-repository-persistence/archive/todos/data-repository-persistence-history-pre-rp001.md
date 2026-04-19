# Settings / Persistence / Serialization 历史任务单

> 说明：本归档由 `2026-04-19` 的 legacy `local-plan/settings-persistence-serialization-tracking.md` 迁移生成。
> 原文件同时承担 todo 与 trace 角色；本文件保留任务目标、已完成改动、验证与 backlog，供后续主题恢复引用。

## 目标

围绕 `GFramework` 当前的设置、持久化、序列化系统，先完成一轮高优先级收敛：

- 修正文档与当前实现不一致的问题
- 补足关键直接测试，避免只靠侧面覆盖
- 修复本轮实现中暴露出来的真实缺陷
- 为后续更大的存档迁移 / 持久化策略统一保留清晰恢复点

## 历史阶段完成情况

### 代码

- [x] `JsonSerializer` 支持注入 `JsonSerializerSettings`
- [x] `JsonSerializer` 暴露 `Converters` 入口，允许按实例追加 converter
- [x] `JsonSerializer` 反序列化失败会带上目标类型上下文
- [x] `JsonSerializer.Serialize(object, Type)` 保持 `obj == null` 时输出 `"null"` 的兼容行为
- [x] `JsonSerializer` 不再把参数校验抛出的 `ArgumentException` 包装成 `InvalidOperationException`
- [x] `DataRepository.SaveAllAsync(...)` 改为批量提交语义，只发送 `DataBatchSavedEvent`
- [x] `DataRepository.DeleteAsync(...)` 仅在目标真实存在时发送删除事件
- [x] `DataRepository` 在批量保存时保留运行时数据类型，避免自动备份读取到退化的 `IData` 类型
- [x] `SettingsModel.GetData<T>()` 在初始化后新增设置数据时会立即注册数据类型
- [x] `SettingsModel.RegisterApplicator<T>()` 会同步注册 applicator 绑定的数据类型
- [x] `SettingsModel.RegisterMigration(...)` 会失效对应类型的迁移缓存，避免 cache warm-up 后新增迁移不生效
- [x] 修复 `UnifiedSettingsDataRepository.SaveAsync(...)` 首次保存时可能发生的自锁死锁
- [x] `UnifiedSettingsDataRepository` 对齐 `DataRepositoryOptions` 契约，支持统一文件级自动备份
- [x] `UnifiedSettingsDataRepository.LoadAsync<T>()` 改为发送 `DataLoadedEvent<T>`，不再退化为
  `DataLoadedEvent<IData>`
- [x] `UnifiedSettingsDataRepository.SaveAllAsync(...)` 改为批量提交语义，不再隐式重复发送单项保存事件
- [x] `UnifiedSettingsDataRepository` 改为“快照 -> 持久化 -> 交换缓存”的提交模型，避免失败写入污染内存中的已提交状态
- [x] `ISaveRepository<T>` 增加正式的 `ISaveMigration<T>` 迁移接口
- [x] `SaveRepository<T>` 在 `LoadAsync(slot)` 时支持按版本链自动迁移并回写升级后的存档
- [x] `SaveRepository<T>` 的迁移注册表增加并发访问保护，禁止同一 `FromVersion` 重复注册被静默覆盖

### 测试

- [x] 新增 `JsonSerializer` 直接单测
- [x] 新增 `SettingsModel` 直接单测
- [x] 新增 `FileStorage` / `SaveRepository` / `UnifiedSettingsDataRepository` 持久化测试
- [x] 新增 `SaveRepository` 迁移链、重复注册和缺失迁移链的直接单测
- [x] 新增 `DataRepository` 批量事件与自动备份直接单测
- [x] 新增 `DataRepository.SaveAllAsync(...)` 运行时类型批量覆盖回归测试
- [x] 新增 `SettingsSystem` 直接测试，覆盖 `ApplyAll / Reset / SaveAll / ResetAll`
- [x] 新增 `UnifiedSettingsDataRepository` 事件开启场景下的上下文集成测试
- [x] 新增 `UnifiedSettingsDataRepository` 保存/删除失败时缓存一致性回归测试
- [x] 定向测试通过：`JsonSerializerTests | SettingsModelTests | PersistenceTests`
- [x] 定向测试通过：`SettingsModelTests | PersistenceTests | SettingsSystemTests`
- [x] 定向测试通过：`PersistenceTests | SettingsSystemTests`
- [x] 定向测试通过：`PersistenceTests | SettingsSystemTests | SettingsModelTests`
- [x] 全量 `GFramework.Game.Tests` 通过

### 文档

- [x] 重写 `docs/zh-CN/game/setting.md`，对齐当前 `ISettingsModel` / `SettingsSystem` / `ISettingsData`
- [x] 修正 `docs/zh-CN/game/data.md`，把存档迁移文档更新为 `ISaveRepository<T>.RegisterMigration(...)` 的内建能力说明
- [x] 修正 `docs/zh-CN/game/data.md`，补充 `IDataRepository` / `UnifiedSettingsDataRepository` 的统一事件与备份语义说明
- [x] 修正 `docs/zh-CN/game/data.md`，补充 `UnifiedSettingsDataRepository` 的最小接入示例、
  `RegisterDataType(...)` 说明与 `DataLoadedEvent<T>` 兼容性说明
- [x] 修正 `docs/zh-CN/game/index.md` 中 `JsonSerializer` 示例缺少 `Newtonsoft.Json` using 的问题
- [x] `setting.md` 中迁移接口示例统一改为 `ISettingsData`，清掉残留旧术语
- [x] 当时已更新 `AGENTS.md`，强调实现完成后必须同步维护恢复文档；当前仓库已由 `ai-plan` 治理规则接管该职责

## 历史改动文件

### 生产代码

- `GFramework.Game/Serializer/JsonSerializer.cs`
- `GFramework.Game/Setting/SettingsModel.cs`
- `GFramework.Game/Data/UnifiedSettingsDataRepository.cs`
- `GFramework.Game/Data/DataRepository.cs`
- `GFramework.Game.Abstractions/Data/ISaveMigration.cs`
- `GFramework.Game.Abstractions/Data/ISaveRepository.cs`
- `GFramework.Game/Data/SaveRepository.cs`
- `GFramework.Game.Abstractions/Data/DataRepositoryOptions.cs`

### 测试

- `GFramework.Game.Tests/Serializer/JsonSerializerTests.cs`
- `GFramework.Game.Tests/Setting/SettingsModelTests.cs`
- `GFramework.Game.Tests/Setting/SettingsSystemTests.cs`
- `GFramework.Game.Tests/Data/PersistenceTestUtilities.cs`
- `GFramework.Game.Tests/Data/PersistenceTests.cs`

### 文档

- `AGENTS.md`
- `docs/zh-CN/game/setting.md`
- `docs/zh-CN/game/data.md`
- `docs/zh-CN/game/index.md`

## 已验证结果

已执行：

```bash
dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj --filter "JsonSerializerTests"
dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj --filter "JsonSerializerTests|SettingsModelTests|PersistenceTests"
dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj
dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "PersistenceTests"
dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release
dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "PersistenceTests|SettingsSystemTests"
dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "PersistenceTests|SettingsSystemTests|SettingsModelTests"
```

结果：

- `JsonSerializerTests`：通过
- `JsonSerializerTests | SettingsModelTests | PersistenceTests`：12 个通过
- `PersistenceTests`（Release）：7 个通过
- `PersistenceTests | SettingsSystemTests`（Release）：16 个通过
- `PersistenceTests | SettingsSystemTests | SettingsModelTests`（Release）：18 个通过
- `PersistenceTests | SettingsSystemTests`（Release，补充回归后）：19 个通过
- `PersistenceTests | SettingsSystemTests | SettingsModelTests`（Release，补充回归后）：21 个通过
- `GFramework.Game.Tests` 全量（Release）：89 个通过

## 本轮发现的真实问题

### 已修复

- `UnifiedSettingsDataRepository.SaveAsync(...)` 在未加载状态下会先拿 `_lock`，再进入 `EnsureLoadedAsync()`，
  导致首次保存等待同一把 `SemaphoreSlim`，形成死锁
- `UnifiedSettingsDataRepository` 在删除或保存时先原地修改 `_file` 再写盘，如果持久化失败会让内存缓存领先于
  磁盘状态，后续无关保存可能把失败修改意外落盘；现已改为快照提交模型
- `SaveRepository<T>` 的迁移注册表在注册与加载并发时缺少同步保护，且重复注册相同 `FromVersion` 会被静默覆盖，
  已改为加锁访问并显式拒绝重复注册

### 已确认但暂未展开

- `SettingsSystem` / `SettingsModel` 仍依赖上下文事件发送，单元测试需要显式提供上下文

## 当前剩余待办

### P0

- [x] 统一 `DataRepository` 与 `UnifiedSettingsDataRepository` 的持久化策略约定

### P1

- [ ] 评估是否要给 `JsonSerializer` 增加更明确的只读配置说明，例如线程安全和实例级 converter 使用方式
- [x] 为 `SettingsSystem` 增加直接测试，覆盖 `ApplyAll / Reset / SaveAll` 的系统层语义
- [x] 为 `UnifiedSettingsDataRepository` 增加事件开启场景下的上下文集成测试

### P2

- [ ] 评估把设置、存档、通用仓库的版本迁移模型做统一抽象
- [ ] 评估压缩 / 加密 / 元数据策略是否应放进更明确的 codec / persistence pipeline

## 下次恢复建议

建议从这里继续：

1. 先评估 `JsonSerializer` 的只读配置、线程安全与实例级 converter 使用说明
2. 再考虑设置 / 存档 / 通用仓库的迁移模型是否进一步统一
3. 最后评估压缩 / 加密 / 元数据策略是否抽成更明确的 codec / persistence pipeline

恢复时优先查看：

- `GFramework.Game/Data/SaveRepository.cs`
- `GFramework.Game/Data/UnifiedSettingsDataRepository.cs`
- `GFramework.Game/Data/DataRepository.cs`
- `GFramework.Game/Setting/SettingsModel.cs`
- `GFramework.Game/Setting/SettingsSystem.cs`
- `GFramework.Game/Serializer/JsonSerializer.cs`
- `GFramework.Game.Tests/Data/PersistenceTests.cs`
- `GFramework.Game.Tests/Setting/SettingsSystemTests.cs`
- `docs/zh-CN/game/data.md`

## 备注

- 该历史阶段形成时，仓库存在大量与本任务无关的既有改动
- 后续继续实施时，应继续把改动严格收敛在设置 / 持久化 / 序列化相关文件，避免误碰其他重构主线
