# SaveRepository 使用指南

## 概述

`SaveRepository<TSaveData>` 是 GFramework 提供的通用存档仓库实现，基于 `IStorage` 抽象层，支持基于槽位的存档管理。

## 核心特性

- ✅ **完全解耦引擎依赖** - 框架层无 Godot 代码
- ✅ **配置对象带默认值** - 无需 ProjectSettings 也能工作
- ✅ **泛型设计** - 支持任意实现 `IData` 的存档类型
- ✅ **自动槽位管理** - 利用 `ScopedStorage` 实现路径隔离
- ✅ **异步操作** - 所有方法均为异步，避免阻塞主线程

## 快速开始

### 1. 定义存档数据类型

```csharp
using GFramework.Game.Abstractions.data;

public class GameSaveData : IData
{
    public string PlayerName { get; set; } = "";
    public int Level { get; set; } = 1;
    public float PlayTime { get; set; } = 0f;
    public DateTime SaveTime { get; set; } = DateTime.Now;
}
```

### 2. 在启动类中注册

```csharp
// 在 GameApp.cs 或类似的启动类中
protected override void OnRegisterUtility()
{
    // 1. 注册序列化器
    var serializer = new JsonSerializer();
    this.RegisterUtility<ISerializer>(serializer);

    // 2. 注册存储
    this.RegisterUtility<IStorage>(new GodotFileStorage(serializer));

    // 3. 创建存档配置
    var saveConfig = new SaveConfiguration
    {
        SaveRoot = "user://saves",      // 存档根目录
        SaveSlotPrefix = "slot_",       // 槽位前缀
        SaveFileName = "save.json"      // 存档文件名
    };

    // 4. 注册存档仓库
    this.RegisterUtility<ISaveRepository<GameSaveData>>(
        new SaveRepository<GameSaveData>(
            this.GetUtility<IStorage>()!,
            saveConfig
        )
    );
}
```

### 3. 使用存档仓库

```csharp
// 获取存档仓库
var saveRepo = this.GetUtility<ISaveRepository<GameSaveData>>();

// 保存存档到槽位 1
var saveData = new GameSaveData
{
    PlayerName = "玩家1",
    Level = 10,
    PlayTime = 3600f
};
await saveRepo.SaveAsync(slot: 1, saveData);

// 加载槽位 1 的存档
var loadedData = await saveRepo.LoadAsync(slot: 1);
GD.Print($"玩家: {loadedData.PlayerName}, 等级: {loadedData.Level}");

// 检查槽位是否存在
if (await saveRepo.ExistsAsync(slot: 1))
{
    GD.Print("槽位 1 存在存档");
}

// 列出所有有效槽位
var slots = await saveRepo.ListSlotsAsync();
GD.Print($"找到 {slots.Count} 个存档槽位");

// 删除槽位 1 的存档
await saveRepo.DeleteAsync(slot: 1);
```

## 高级用法

### 从 ProjectSettings 读取配置

```csharp
var saveConfig = new SaveConfiguration
{
    SaveRoot = ProjectSettings.HasSetting("application/config/save/save_path")
        ? ProjectSettings.GetSetting("application/config/save/save_path").AsString()
        : "user://saves",
    SaveSlotPrefix = ProjectSettings.HasSetting("application/config/save/save_slot_prefix")
        ? ProjectSettings.GetSetting("application/config/save/save_slot_prefix").AsString()
        : "slot_",
    SaveFileName = ProjectSettings.HasSetting("application/config/save/save_file_name")
        ? ProjectSettings.GetSetting("application/config/save/save_file_name").AsString()
        : "save.json"
};
```

### 支持版本迁移

```csharp
using GFramework.Game.Abstractions.data;

public class GameSaveData : IData, IVersionedData
{
    public int Version { get; set; } = 1;
    public string PlayerName { get; set; } = "";
    public int Level { get; set; } = 1;

    // 版本 2 新增字段
    public int Experience { get; set; } = 0;
}

// 实现迁移逻辑（未来可扩展）
public class GameSaveDataMigration_1_to_2 : IDataMigration<GameSaveData>
{
    public int FromVersion => 1;
    public int ToVersion => 2;

    public GameSaveData Migrate(GameSaveData oldData)
    {
        // 迁移逻辑：为旧存档添加默认经验值
        oldData.Experience = oldData.Level * 100;
        oldData.Version = 2;
        return oldData;
    }
}
```

### 多种存档类型

```csharp
// 游戏存档
this.RegisterUtility<ISaveRepository<GameSaveData>>(
    new SaveRepository<GameSaveData>(storage, new SaveConfiguration
    {
        SaveRoot = "user://saves/game",
        SaveSlotPrefix = "slot_",
        SaveFileName = "save.json"
    })
);

// 用户配置存档
this.RegisterUtility<ISaveRepository<UserProfileData>>(
    new SaveRepository<UserProfileData>(storage, new SaveConfiguration
    {
        SaveRoot = "user://saves/profile",
        SaveSlotPrefix = "profile_",
        SaveFileName = "profile.json"
    })
);

// 成就数据存档
this.RegisterUtility<ISaveRepository<AchievementData>>(
    new SaveRepository<AchievementData>(storage, new SaveConfiguration
    {
        SaveRoot = "user://saves/achievements",
        SaveSlotPrefix = "achievement_",
        SaveFileName = "data.json"
    })
);
```

## 文件结构

使用默认配置时，存档文件结构如下：

```
user://saves/
├── slot_1/
│   └── save.json
├── slot_2/
│   └── save.json
└── slot_3/
    └── save.json
```

## API 参考

### ISaveRepository<TSaveData>

| 方法 | 说明 | 返回值 |
|------|------|--------|
| `ExistsAsync(int slot)` | 检查指定槽位是否存在存档 | `Task<bool>` |
| `LoadAsync(int slot)` | 加载指定槽位的存档 | `Task<TSaveData>` |
| `SaveAsync(int slot, TSaveData data)` | 保存存档到指定槽位 | `Task` |
| `DeleteAsync(int slot)` | 删除指定槽位的存档 | `Task` |
| `ListSlotsAsync()` | 列出所有有效的存档槽位 | `Task<IReadOnlyList<int>>` |

### SaveConfiguration

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SaveRoot` | `string` | `"user://saves"` | 存档根目录 |
| `SaveSlotPrefix` | `string` | `"slot_"` | 槽位前缀 |
| `SaveFileName` | `string` | `"save.json"` | 存档文件名 |

## 从旧版 SaveStorageUtility 迁移

### 主要变化

1. **所有方法改为异步** - 使用 `async/await`
2. **接口名称变更** - `ISaveStorageUtility` → `ISaveRepository<TSaveData>`
3. **方法名称添加 Async 后缀**
4. **配置通过对象传递** - 支持默认值

### 迁移对照表

| 旧方法 | 新方法 |
|--------|--------|
| `Exists(int slot)` | `await ExistsAsync(int slot)` |
| `Load(int slot)` | `await LoadAsync(int slot)` |
| `Save(int slot, data)` | `await SaveAsync(int slot, data)` |
| `Delete(int slot)` | `await DeleteAsync(int slot)` |
| `ListSlots()` | `await ListSlotsAsync()` |

### 迁移示例

**旧代码:**
```csharp
var saveUtil = this.GetUtility<ISaveStorageUtility>();
saveUtil.Save(1, data);
var loadedData = saveUtil.Load(1);
```

**新代码:**
```csharp
var saveRepo = this.GetUtility<ISaveRepository<GameSaveData>>();
await saveRepo.SaveAsync(1, data);
var loadedData = await saveRepo.LoadAsync(1);
```

## 常见问题

### Q: 如何切换到云存档？

A: 只需替换 `IStorage` 实现即可：

```csharp
// 本地存档
this.RegisterUtility<IStorage>(new GodotFileStorage(serializer));

// 云存档（需要自行实现 CloudStorage）
this.RegisterUtility<IStorage>(new CloudStorage(apiKey, serializer));
```

### Q: 如何加密存档？

A: 使用装饰器模式包装 `IStorage`：

```csharp
var baseStorage = new GodotFileStorage(serializer);
var encryptedStorage = new EncryptedStorage(baseStorage, encryptionKey);
this.RegisterUtility<IStorage>(encryptedStorage);
```

### Q: 如何实现自动备份？

A: 使用 `DataRepository` 的 `AutoBackup` 选项（需要配合 `DataRepository` 使用）。

### Q: LoadAsync 返回的是新实例还是缓存实例？

A: 每次调用都会从存储读取并返回新实例，不会缓存。如果需要缓存，请在业务层实现。

## 最佳实践

1. **使用异步方法** - 避免阻塞主线程
2. **错误处理** - 使用 try-catch 捕获 IO 异常
3. **定期保存** - 不要等到游戏退出才保存
4. **槽位验证** - 加载前先检查槽位是否存在
5. **版本管理** - 为存档数据实现 `IVersionedData` 接口

## 相关文档

- [IStorage 接口文档](./IStorage.md)
- [ScopedStorage 使用指南](./ScopedStorage.md)
- [数据仓库模式](./DataRepository.md)
