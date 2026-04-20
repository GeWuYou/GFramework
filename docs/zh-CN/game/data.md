---
title: 数据与存档系统
description: 数据与存档系统提供统一的数据持久化基础能力，支持多槽位存档、版本化数据和仓库抽象。
---

# 数据与存档系统

## 概述

数据与存档系统是 GFramework.Game 中用于管理游戏数据持久化的核心组件。它提供了统一的数据加载和保存接口，支持多槽位存档管理、版本化数据模式，以及与存储系统、序列化系统的组合使用。

通过数据系统，你可以将游戏数据保存到本地存储，支持多个存档槽位，并在需要时于应用层实现版本迁移。

**主要特性**：

- 统一的数据持久化接口
- 多槽位存档管理
- 数据版本控制模式
- 异步加载和保存
- 批量数据操作
- 与存储系统集成

## 核心概念

### 数据接口

`IData` 标记数据类型：

```csharp
public interface IData
{
    // 标记接口，用于标识可持久化的数据
}
```

### 数据仓库

`IDataRepository` 提供通用的数据操作：

```csharp
public interface IDataRepository : IUtility
{
    Task<T> LoadAsync<T>(IDataLocation location) where T : class, IData, new();
    Task SaveAsync<T>(IDataLocation location, T data) where T : class, IData;
    Task<bool> ExistsAsync(IDataLocation location);
    Task DeleteAsync(IDataLocation location);
    Task SaveAllAsync(IEnumerable<(IDataLocation, IData)> dataList);
}
```

`IDataRepository` 描述的是“仓库语义”，不是固定的落盘格式。实现可以选择每个数据项独立成文件，也可以把多个 section 聚合到同一个文件里。

当前内建实现里：

- `DataRepository` 采用“每个 location 一份持久化对象”的模型
- `UnifiedSettingsDataRepository` 采用“所有设置聚合到一个统一文件”的模型

两者对外遵守同一套约定：

- `SaveAllAsync(...)` 视为一次批量提交，只发送 `DataBatchSavedEvent`，不会再为每个条目重复发送 `DataSavedEvent<T>`
- `DeleteAsync(...)` 只有在目标数据真实存在并被删除时才会发送删除事件
- 当 `DataRepositoryOptions.AutoBackup = true` 时，覆盖已有数据前会先保留上一份快照
- 对 `UnifiedSettingsDataRepository` 来说，备份粒度是整个统一文件，而不是单个设置 section

### 存档仓库

`ISaveRepository<T>` 专门用于管理游戏存档：

```csharp
public interface ISaveRepository<TSaveData> : IUtility
    where TSaveData : class, IData, new()
{
    ISaveRepository<TSaveData> RegisterMigration(ISaveMigration<TSaveData> migration);
    Task<bool> ExistsAsync(int slot);
    Task<TSaveData> LoadAsync(int slot);
    Task SaveAsync(int slot, TSaveData data);
    Task DeleteAsync(int slot);
    Task<IReadOnlyList<int>> ListSlotsAsync();
}
```

`ISaveMigration<TSaveData>` 定义单步迁移：

```csharp
public interface ISaveMigration<TSaveData>
    where TSaveData : class, IData
{
    int FromVersion { get; }
    int ToVersion { get; }
    TSaveData Migrate(TSaveData oldData);
}
```

### 版本化数据

`IVersionedData` 支持数据版本管理：

```csharp
public interface IVersionedData : IData
{
    int Version { get; }
    DateTime LastModified { get; }
}
```

## 基本用法

### 定义数据类型

```csharp
using GFramework.Game.Abstractions.Data;

// 简单数据
public class PlayerData : IData
{
    public string Name { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
}

// 版本化数据
public class SaveData : IVersionedData
{
    public int Version { get; set; } = 1;
    public PlayerData Player { get; set; }
    public DateTime SaveTime { get; set; }
}
```

### 使用存档仓库

```csharp
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class SaveController : IController
{
    public async Task SaveGame(int slot)
    {
        var saveRepo = this.GetUtility<ISaveRepository<SaveData>>();

        // 创建存档数据
        var saveData = new SaveData
        {
            Player = new PlayerData
            {
                Name = "Player1",
                Level = 10,
                Experience = 1000
            },
            SaveTime = DateTime.Now
        };

        // 保存到指定槽位
        await saveRepo.SaveAsync(slot, saveData);
        Console.WriteLine($"游戏已保存到槽位 {slot}");
    }

    public async Task LoadGame(int slot)
    {
        var saveRepo = this.GetUtility<ISaveRepository<SaveData>>();

        // 检查存档是否存在
        if (!await saveRepo.ExistsAsync(slot))
        {
            Console.WriteLine($"槽位 {slot} 不存在存档");
            return;
        }

        // 加载存档
        var saveData = await saveRepo.LoadAsync(slot);
        Console.WriteLine($"加载存档: {saveData.Player.Name}, 等级 {saveData.Player.Level}");
    }

    public async Task DeleteSave(int slot)
    {
        var saveRepo = this.GetUtility<ISaveRepository<SaveData>>();

        // 删除存档
        await saveRepo.DeleteAsync(slot);
        Console.WriteLine($"已删除槽位 {slot} 的存档");
    }
}
```

### 注册存档仓库

```csharp
using GFramework.Game.Data;

public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 获取存储系统
        var storage = this.GetUtility<IStorage>();

        // 创建存档配置
        var saveConfig = new SaveConfiguration
        {
            SaveRoot = "saves",
            SaveSlotPrefix = "slot_",
            SaveFileName = "save.json"
        };

        // 注册存档仓库
        var saveRepo = new SaveRepository<SaveData>(storage, saveConfig);
        RegisterUtility<ISaveRepository<SaveData>>(saveRepo);
    }
}
```

## 高级用法

### 列出所有存档

```csharp
public async Task ShowSaveList()
{
    var saveRepo = this.GetUtility<ISaveRepository<SaveData>>();

    // 获取所有存档槽位
    var slots = await saveRepo.ListSlotsAsync();

    Console.WriteLine($"找到 {slots.Count} 个存档:");
    foreach (var slot in slots)
    {
        var saveData = await saveRepo.LoadAsync(slot);
        Console.WriteLine($"槽位 {slot}: {saveData.Player.Name}, " +
                         $"等级 {saveData.Player.Level}, " +
                         $"保存时间 {saveData.SaveTime}");
    }
}
```

### 自动保存

```csharp
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class AutoSaveController : IController
{
    private CancellationTokenSource? _autoSaveCts;

    public void StartAutoSave(int slot, TimeSpan interval)
    {
        _autoSaveCts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!_autoSaveCts.Token.IsCancellationRequested)
            {
                await Task.Delay(interval, _autoSaveCts.Token);

                try
                {
                    await SaveGame(slot);
                    Console.WriteLine("自动保存完成");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"自动保存失败: {ex.Message}");
                }
            }
        }, _autoSaveCts.Token);
    }

    public void StopAutoSave()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
        _autoSaveCts = null;
    }

    private async Task SaveGame(int slot)
    {
        var saveRepo = this.GetUtility<ISaveRepository<SaveData>>();
        var saveData = CreateSaveData();
        await saveRepo.SaveAsync(slot, saveData);
    }

    private SaveData CreateSaveData()
    {
        // 从游戏状态创建存档数据
        return new SaveData();
    }
}
```

### 数据版本迁移

`SaveRepository<TSaveData>` 现在支持注册正式的迁移器，并在 `LoadAsync(slot)` 时自动升级旧版本存档。

迁移规则如下：

- `TSaveData` 需要实现 `IVersionedData`
- 仓库以 `new TSaveData().Version` 作为当前运行时目标版本
- 每个迁移器负责一个 `FromVersion -> ToVersion` 跳转
- 同一个 `FromVersion` 只能注册一个迁移器，且 `ToVersion` 必须严格大于 `FromVersion`
- 加载时仓库会按链路连续执行迁移，并在成功后自动回写升级后的存档
- 迁移器返回数据上的 `Version` 必须与声明的 `ToVersion` 一致
- 如果缺少中间迁移器，或者读到了比当前运行时更高的版本，`LoadAsync` 会抛出异常，避免静默加载错误数据

```csharp
public sealed class SaveData : IVersionedData
{
    // 当前运行时代码支持的最新版本
    public int Version { get; set; } = 2;
    public string PlayerName { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public DateTime LastModified { get; set; }
}

public sealed class SaveDataMigrationV1ToV2 : ISaveMigration<SaveData>
{
    public int FromVersion => 1;

    public int ToVersion => 2;

    public SaveData Migrate(SaveData oldData)
    {
        return new SaveData
        {
            Version = 2,
            PlayerName = oldData.PlayerName,
            Level = oldData.Level,
            Experience = oldData.Level * 100,
            LastModified = DateTime.UtcNow
        };
    }
}

public sealed class SaveModule : AbstractModule
{
    public override void Install(IArchitecture architecture)
    {
        var storage = architecture.GetUtility<IStorage>();
        var saveConfig = new SaveConfiguration
        {
            SaveRoot = "saves",
            SaveSlotPrefix = "slot_",
            SaveFileName = "save"
        };

        var saveRepo = new SaveRepository<SaveData>(storage, saveConfig)
            .RegisterMigration(new SaveDataMigrationV1ToV2());

        architecture.RegisterUtility<ISaveRepository<SaveData>>(saveRepo);
    }
}

public async Task<SaveData> LoadGame(int slot)
{
    var saveRepo = this.GetUtility<ISaveRepository<SaveData>>();

    // 如果槽位里是 v1，仓库会自动迁移到 v2，并把新版本重新写回存储。
    return await saveRepo.LoadAsync(slot);
}
```

`ISaveMigration<TSaveData>` 接收和返回的是同一个存档类型。也就是说，框架提供的是“当前类型内的版本升级管线”，
而不是跨 CLR 类型的双模型反序列化系统。如果旧版本缺失了新字段，反序列化会先使用当前类型的默认值，再由迁移器补齐。

### 使用数据仓库

```csharp
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class SettingsController : IController
{
    public async Task SaveSettings()
    {
        var dataRepo = this.GetUtility<IDataRepository>();

        var settings = new GameSettings
        {
            MasterVolume = 0.8f,
            MusicVolume = 0.6f,
            SfxVolume = 0.7f
        };

        // 定义数据位置
        var location = new DataLocation("settings", "game_settings.json");

        // 保存设置
        await dataRepo.SaveAsync(location, settings);
    }

    public async Task<GameSettings> LoadSettings()
    {
        var dataRepo = this.GetUtility<IDataRepository>();
        var location = new DataLocation("settings", "game_settings.json");

        // 检查是否存在
        if (!await dataRepo.ExistsAsync(location))
        {
            return new GameSettings(); // 返回默认设置
        }

        // 加载设置
        return await dataRepo.LoadAsync<GameSettings>(location);
    }
}
```

### 批量保存数据

```csharp
public async Task SaveAllGameData()
{
    var dataRepo = this.GetUtility<IDataRepository>();

    var dataList = new List<(IDataLocation, IData)>
    {
        (new DataLocation("player", "profile.json"), playerData),
        (new DataLocation("inventory", "items.json"), inventoryData),
        (new DataLocation("quests", "progress.json"), questData)
    };

    // 批量保存
    await dataRepo.SaveAllAsync(dataList);
    Console.WriteLine("所有数据已保存");
}
```

`SaveAllAsync(...)` 的事件语义和逐项调用 `SaveAsync(...)` 不同。它代表一次显式的批量提交，因此适合让监听器在收到 `DataBatchSavedEvent` 时统一刷新 UI、缓存或元数据，而不是对每个条目单独响应。

### 聚合设置仓库

如果你希望把设置统一保存到单个文件中，可以使用 `UnifiedSettingsDataRepository`：

```csharp
using GFramework.Game.Data;
using GFramework.Game.Serializer;

public sealed class GameArchitecture : Architecture
{
    protected override void Init()
    {
        var storage = this.GetUtility<IStorage>();
        var serializer = new JsonSerializer();

        var settingsRepo = new UnifiedSettingsDataRepository(
            storage,
            serializer,
            new DataRepositoryOptions
            {
                AutoBackup = true,
                EnableEvents = true
            },
            "settings.json");

        settingsRepo.RegisterDataType(new DataLocation("settings", "graphics"), typeof(GraphicsSettings));
        settingsRepo.RegisterDataType(new DataLocation("settings", "audio"), typeof(AudioSettings));

        RegisterUtility<ISettingsDataRepository>(settingsRepo);
    }
}
```

这个实现依然满足 `IDataRepository` 的通用契约，但有两个实现层面的差异需要明确：

- 它把所有设置 section 缓存在内存中，并在保存或删除时整文件回写
- 开启自动备份时，备份的是整个 `settings.json` 文件，因此适合做“上一次完整设置快照”的恢复，而不是 section 级别回滚

如果你需要 `LoadAllAsync()`，或者希望在同一个统一文件里混合反序列化多个 section，必须先为每个 section 注册类型：

```csharp
public async Task PrintSettingsSnapshot()
{
    var repo = this.GetUtility<ISettingsDataRepository>();

    var all = await repo.LoadAllAsync();

    var graphics = (GraphicsSettings)all["graphics"];
    var audio = (AudioSettings)all["audio"];

    Console.WriteLine($"Resolution: {graphics.ResolutionWidth}x{graphics.ResolutionHeight}");
    Console.WriteLine($"MasterVolume: {audio.MasterVolume}");
}
```

最小采用要求：

- 项目需要可用的 `IStorage`
- 项目需要一个可用的序列化器实例，例如 `GFramework.Game.Serializer.JsonSerializer`
- 在注册仓库时，把所有需要参与 `LoadAllAsync()` 或混合 section 反序列化的 location/type 对显式调用一次 `RegisterDataType(...)`

兼容性说明：

- 现在 `UnifiedSettingsDataRepository.LoadAsync<T>()` 发送的是 `DataLoadedEvent<T>`，而不是 `DataLoadedEvent<IData>`
- 如果你之前监听的是 `DataLoadedEvent<IData>`，需要改成订阅具体类型，例如 `DataLoadedEvent<GraphicsSettings>` 或 `DataLoadedEvent<AudioSettings>`

### 存档备份

```csharp
public async Task BackupSave(int slot)
{
    var saveRepo = this.GetUtility<ISaveRepository<SaveData>>();

    if (!await saveRepo.ExistsAsync(slot))
    {
        Console.WriteLine("存档不存在");
        return;
    }

    // 加载原存档
    var saveData = await saveRepo.LoadAsync(slot);

    // 保存到备份槽位
    int backupSlot = slot + 100;
    await saveRepo.SaveAsync(backupSlot, saveData);

    Console.WriteLine($"存档已备份到槽位 {backupSlot}");
}

public async Task RestoreBackup(int slot)
{
    int backupSlot = slot + 100;
    var saveRepo = this.GetUtility<ISaveRepository<SaveData>>();

    if (!await saveRepo.ExistsAsync(backupSlot))
    {
        Console.WriteLine("备份不存在");
        return;
    }

    // 加载备份
    var backupData = await saveRepo.LoadAsync(backupSlot);

    // 恢复到原槽位
    await saveRepo.SaveAsync(slot, backupData);

    Console.WriteLine($"已从备份恢复到槽位 {slot}");
}
```

## 最佳实践

1. **使用版本化数据**：为存档数据实现 `IVersionedData`
   ```csharp
   ✓ public class SaveData : IVersionedData { public int Version { get; set; } = 1; }
   ✗ public class SaveData : IData { } // 无法进行版本管理
   ```

2. **定期自动保存**：避免玩家数据丢失
   ```csharp
   // 每 5 分钟自动保存
   StartAutoSave(currentSlot, TimeSpan.FromMinutes(5));
   ```

3. **保存前验证数据**：确保数据完整性
   ```csharp
   public async Task SaveGame(int slot)
   {
       var saveData = CreateSaveData();

       if (!ValidateSaveData(saveData))
       {
           throw new InvalidOperationException("存档数据无效");
       }

       await saveRepo.SaveAsync(slot, saveData);
   }
   ```

4. **处理保存失败**：使用 try-catch 捕获异常
   ```csharp
   try
   {
       await saveRepo.SaveAsync(slot, saveData);
   }
   catch (Exception ex)
   {
       Logger.Error($"保存失败: {ex.Message}");
       ShowErrorMessage("保存失败，请重试");
   }
   ```

5. **提供多个存档槽位**：让玩家可以管理多个存档
   ```csharp
   // 支持 10 个存档槽位
   for (int i = 1; i <= 10; i++)
   {
       if (await saveRepo.ExistsAsync(i))
       {
           ShowSaveSlot(i);
       }
   }
   ```

6. **在关键时刻保存**：场景切换、关卡完成等
   ```csharp
   public async Task OnLevelComplete()
   {
       // 关卡完成时自动保存
       await SaveGame(currentSlot);
   }
   ```

## 常见问题

### 问题：如何实现多个存档槽位？

**解答**：
使用 `ISaveRepository<T>` 的槽位参数：

```csharp
// 保存到不同槽位
await saveRepo.SaveAsync(1, saveData);  // 槽位 1
await saveRepo.SaveAsync(2, saveData);  // 槽位 2
await saveRepo.SaveAsync(3, saveData);  // 槽位 3
```

### 问题：如何处理数据版本升级？

**解答**：
实现 `IVersionedData`，并在仓库初始化阶段注册 `ISaveMigration<TSaveData>`。之后 `LoadAsync(slot)` 会自动执行迁移并回写：

```csharp
var saveRepo = new SaveRepository<SaveData>(storage, saveConfig)
    .RegisterMigration(new SaveDataMigrationV1ToV2())
    .RegisterMigration(new SaveDataMigrationV2ToV3());

var data = await saveRepo.LoadAsync(slot);
```

### 问题：存档数据保存在哪里？

**解答**：
由存储系统决定，通常在：

- Windows: `%AppData%/GameName/saves/`
- Linux: `~/.local/share/GameName/saves/`
- macOS: `~/Library/Application Support/GameName/saves/`

### 问题：如何实现云存档？

**解答**：
实现自定义的 `IStorage`，将数据保存到云端：

```csharp
public class CloudStorage : IStorage
{
    public async Task WriteAsync(string path, byte[] data)
    {
        await UploadToCloud(path, data);
    }

    public async Task<byte[]> ReadAsync(string path)
    {
        return await DownloadFromCloud(path);
    }
}
```

### 问题：如何加密存档数据？

**解答**：
在保存和加载时进行加密/解密：

```csharp
public async Task SaveEncrypted(int slot, SaveData data)
{
    var json = JsonSerializer.Serialize(data);
    var encrypted = Encrypt(json);
    await storage.WriteAsync(path, encrypted);
}

public async Task<SaveData> LoadEncrypted(int slot)
{
    var encrypted = await storage.ReadAsync(path);
    var json = Decrypt(encrypted);
    return JsonSerializer.Deserialize<SaveData>(json);
}
```

### 问题：存档损坏怎么办？

**解答**：
实现备份和恢复机制：

```csharp
public async Task SaveWithBackup(int slot, SaveData data)
{
    // 先备份旧存档
    if (await saveRepo.ExistsAsync(slot))
    {
        var oldData = await saveRepo.LoadAsync(slot);
        await saveRepo.SaveAsync(slot + 100, oldData);
    }

    // 保存新存档
    await saveRepo.SaveAsync(slot, data);
}
```

## 相关文档

- [设置系统](/zh-CN/game/setting) - 游戏设置管理
- [场景系统](/zh-CN/game/scene) - 场景切换时保存
- [存档系统实现教程](/zh-CN/tutorials/save-system) - 完整示例
- [Godot 集成](/zh-CN/godot/index) - Godot 中的数据管理
