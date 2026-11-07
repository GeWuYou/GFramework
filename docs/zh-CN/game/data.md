---
title: 数据与存档系统
description: 使用构造注入的数据仓库、统一设置文件和槽位存档，管理取消、迁移与通知。
---

# 数据与存档系统

Game 提供三类普通服务，可直接构造或登记到标准 DI：

| 服务 | 数据组织 | 适用场景 |
| --- | --- | --- |
| `DataRepository` | 一个 `IDataLocation` 对应一份对象 | 玩家档案、缓存与独立业务数据 |
| `UnifiedSettingsDataRepository` | 多个 section 聚合到一个文件 | 音频、图形、语言和输入设置 |
| `SaveRepository<TSaveData>` | 按槽位组织数据 | 游戏进度及版本迁移 |

仓库通过构造函数接收存储和序列化依赖，操作不要求 Architecture、Utility 角色或初始化调用。静态内容配置使用独立的 [配置系统](./config-system.md)。

## 槽位存档

```csharp
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

using var storage = new FileStorage("GameData", new JsonSerializer());
var saves = new SaveRepository<PlayerSave>(storage, new SaveConfiguration
{
    SaveRoot = "saves"
});
await saves.SaveAsync(1, new PlayerSave { Score = 12 });
var restored = await saves.LoadAsync(1);

public sealed class PlayerSave : IData
{
    public int Score { get; set; }
}
```

`ExistsAsync` 检查槽位；`LoadAsync` 在槽位不存在时创建默认对象；`ListSlotsAsync` 按编号返回包含存档文件的槽位。缺失文件、损坏文件和不支持的版本由应用按业务需要处理，保存应由明确的业务动作触发。

版本化 DTO 实现 `IVersionedData`，通过 `RegisterMigration` 登记递增版本的迁移。重复起始版本被拒绝；缺少迁移链、非法输出版本和高于当前运行时的文件版本会失败。迁移使用注册表快照，全部成功后才回写升级内容。

## 数据与统一设置仓库

`DataRepository` 接收 `IStorage`；`UnifiedSettingsDataRepository` 同时接收 `IRuntimeTypeSerializer`。使用统一文件时，在加载前调用 `RegisterDataType(location, typeof(T))` 明确登记允许恢复的类型。

`DataRepositoryOptions.AutoBackup` 在覆盖前保存旧快照。独立数据按 key 备份，统一设置按完整文件备份。统一仓库先生成下一份缓存快照，在写入成功后交换缓存，失败不会把未提交数据留在内存中。

`SaveAllAsync` 发出一次批量通知。独立数据仓库逐项写入；统一设置仓库将本批 section 合并到一次文件提交。

## 通知

仓库构造函数最后的 `Action<object>? publishEvent` 是可选通知出口，`EnableEvents` 控制是否发布。宿主可在组合根把它接到自己的事件总线或显式回调。没有回调时，仓库只执行数据操作。

通知使用 `DataLoadedEvent<T>`、`DataSavedEvent<T>`、`DataDeletedEvent` 和 `DataBatchSavedEvent`。通知在对应操作成功后发生；内部加载、备份和迁移回写不额外生成公开操作通知。

## 取消与关闭

仓库异步方法的最后一个参数是 `CancellationToken`。令牌撤销尚未接纳的工作、设置提交锁等待和后续批次项；已经开始的底层写入会被完整等待。调用方应等待所有保存任务，再释放底层存储。

`IStorage` 提供同步与异步读写；文件存储使用同目录临时文件替换目标。路径、扩展名和存储所有权见 [存储系统](./storage.md)。

## 迁移

删除仓库的 `SetContext`、Utility 注册和空初始化代码，改为显式传入依赖。需要事件时传入通知回调；文件布局、槽位命名和已有序列化格式保持原样。

## 相关主题

- [设置系统](./setting.md)
- [序列化系统](./serialization.md)
- [存储系统](./storage.md)
