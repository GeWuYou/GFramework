---
title: 数据与存档系统
description: 以当前 GFramework.Game 源码与 PersistenceTests 为准，说明 DataRepository、UnifiedSettingsDataRepository 和 SaveRepository 的职责边界。
---

# 数据与存档系统

`GFramework.Game` 的数据持久化不是“只有一个万能仓库”。

当前更准确的理解是三层分工：

- `DataRepository`
  - 面向“一个 location 对应一份持久化对象”的通用数据仓库
- `UnifiedSettingsDataRepository`
  - 面向“多个设置 section 聚合到同一个文件”的设置仓库
- `SaveRepository<TSaveData>`
  - 面向“按槽位组织的版本化存档”

如果先把这三类入口分开理解，后续采用路径会清晰很多。

## 什么时候用哪个仓库

### `DataRepository`

适合：

- 单份玩家档案
- 单份运行时缓存
- 一条 location 对应一个文件的普通业务数据

默认语义是：

- `IDataLocation` 决定 key
- 一条 location 对应一份对象
- 覆盖保存时可按 `DataRepositoryOptions.AutoBackup` 创建 `<key>.backup`
- `SaveAllAsync(...)` 视为一次批量提交，只发送批量事件，不重复发送单项保存事件

### `UnifiedSettingsDataRepository`

适合：

- 音频、图形、语言等多个设置 section 统一落到一份文件
- 启动时一次性加载所有设置，再交给 `SettingsModel<TRepository>` 编排

默认语义是：

- 底层持久化文件只有一份，默认文件名是 `settings.json`
- 各个设置 section 仍然通过 `IDataLocation` 的 key 区分
- 保存、删除时会整文件回写，而不是只改单个 section 文件
- 开启 `AutoBackup` 时，备份粒度也是整个统一文件，不是单个 section

当 `DataRepositoryOptions.BasePath = "settings"`，并保持默认文件名时，最小目录结构通常是：

```text
settings/
  settings.json
```

如果同时开启 `AutoBackup = true`，则同一路径下还会额外出现：

```text
settings/
  settings.json
  settings.backup
```

### `SaveRepository<TSaveData>`

适合：

- 多槽位存档
- 需要版本迁移的 save data
- 需要列举现有槽位和删除槽位

默认语义是：

- 按 `SaveRoot` / `SaveSlotPrefix` / `SaveFileName` 组织目录
- 槽位不存在时，`LoadAsync(slot)` 返回新的 `TSaveData` 实例，而不是 `null`
- `ListSlotsAsync()` 只返回真实存在存档文件的槽位，并按升序排列
- 迁移成功后会把升级后的结果自动回写到槽位文件

## 当前公开入口

### `DataRepository`

`DataRepository` 是最通用的默认实现。当前仓库和测试确认的行为有几条需要特别记住：

- `LoadAsync<T>(location)` 在文件不存在时返回 `new T()`，不是抛异常
- `DeleteAsync(location)` 只有在目标数据真实存在并被删除时才发送删除事件
- `SaveAllAsync(...)` 会抑制逐项 `DataSavedEvent<T>`，只保留一次 `DataBatchSavedEvent`
- `AutoBackup = true` 时，覆盖旧值前会先把旧值写到 `<key>.backup`

最小接法通常是：项目先准备一个 `IDataLocation` 或 `IDataLocationProvider`，再把它交给 `DataRepository` 做
`location -> key` 的映射；repository 自己不负责推导业务对象应该落在哪个位置。

### `UnifiedSettingsDataRepository`

当前 `SettingsModel<TRepository>` 依赖的默认设置仓库就是它。

它和普通 `DataRepository` 的关键区别不是接口，而是落盘形态：

- `DataRepository`
  - 每个 location 对应一个独立文件
- `UnifiedSettingsDataRepository`
  - 所有 section 聚合到同一个统一文件

还有两个容易遗漏的点：

- `LoadAllAsync()` 依赖 `RegisterDataType(location, type)` 建立 section -> 运行时类型映射
- 仓库内部会先把统一文件加载进缓存，再在保存 / 删除时基于快照整文件提交

这就是为什么 `SettingsModel<TRepository>` 会在拿到 `GetData<T>()` 或 `RegisterApplicator(...)` 后主动把类型注册回 repository。

### `SaveRepository<TSaveData>`

`SaveRepository<TSaveData>` 用于槽位存档，不直接复用 `IDataLocation`。

最重要的公开配置是 `SaveConfiguration`：

```csharp
var config = new SaveConfiguration
{
    SaveRoot = "saves",
    SaveSlotPrefix = "slot_",
    SaveFileName = "save.json"
};
```

按这个配置，槽位 `1` 的默认文件结构就是：

```text
saves/
  slot_1/
    save.json
```

当前实现内部会先把根存储包装成 `ScopedStorage(storage, config.SaveRoot)`，再按槽位继续加前缀，因此项目层一般不需要手工再拼一次 `"saves/slot_1"`。

## 存档迁移的真实语义

`SaveRepository<TSaveData>` 只有在 `TSaveData` 实现了 `IVersionedData` 时，才支持 `RegisterMigration(...)`。

当前源码和 `PersistenceTests` 明确约束了下面这些行为：

- 非版本化 save type 注册迁移器会直接失败
- 同一个 `FromVersion` 不能重复注册迁移器
- 迁移链缺口会显式抛错，不会静默返回半升级结果
- 迁移器声明的 `ToVersion` 必须与实际返回对象的版本一致
- 如果读到比当前运行时代码更高版本的存档，也会明确失败
- 单次加载会先固定一份迁移表快照，避免并发注册让同一次加载看到变化中的链路

也就是说，`SaveRepository<TSaveData>` 的迁移语义更偏“严格升级管线”，而不是“尽量帮你读出来”。

## 最小接入路径

下面是当前 `Game` 层最常见的一套组合方式：

```csharp
using GFramework.Core.Abstractions.Serializer;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

var serializer = new JsonSerializer();
var storage = new FileStorage("GameData", serializer, ".json");

ISettingsDataRepository settingsRepository = new UnifiedSettingsDataRepository(
    storage,
    serializer,
    new DataRepositoryOptions
    {
        BasePath = "settings",
        AutoBackup = true
    });

var saveConfiguration = new SaveConfiguration
{
    SaveRoot = "saves",
    SaveSlotPrefix = "slot_",
    SaveFileName = "save.json"
};
```

分工应保持清晰：

- `storage` 只负责底层文件读写
- `settingsRepository` 负责统一设置文件
- `SaveRepository<TSaveData>` 负责槽位目录和存档迁移

## 当前边界

- `DataRepositoryOptions` 描述的是仓库公开行为契约，不是某一种固定落盘格式
- `UnifiedSettingsDataRepository` 不是通用万能仓库，它专门服务“多 section 聚合单文件”的场景
- `SaveRepository<TSaveData>` 不负责业务层的 autosave 策略、云同步或存档选择 UI
- `LoadAsync(...)` 返回新实例的行为适合默认启动路径；如果项目需要“缺档即报错”，应在业务层显式调用 `ExistsAsync(...)`

## 继续阅读

1. [设置系统](./setting.md)
2. [存储系统](./storage.md)
3. [序列化系统](./serialization.md)
4. [Game 入口](./index.md)
