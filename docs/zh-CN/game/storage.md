---
title: 存储系统详解
description: 存储系统提供了灵活的文件存储和作用域隔离功能，支持跨平台数据持久化。
---

# 存储系统详解

## 概述

存储系统是 GFramework.Game 中用于管理文件存储的核心组件。它提供了统一的存储接口，支持键值对存储、作用域隔离、目录操作等功能，让你可以轻松实现游戏数据的持久化。

存储系统采用装饰器模式设计，通过 `IStorage` 接口定义统一的存储操作，`FileStorage` 提供基于文件系统的实现，`ScopedStorage`
提供作用域隔离功能。

**主要特性**：

- 统一的键值对存储接口
- 基于文件系统的持久化
- 作用域隔离和命名空间管理
- 线程安全的并发访问
- 支持同步和异步操作
- 目录和文件列举功能
- 路径安全防护
- 跨平台支持（包括 Godot）

## 核心概念

### 存储接口

`IStorage` 定义了统一的存储操作：

```csharp
public interface IStorage : IUtility
{
    // 检查键是否存在
    bool Exists(string key);
    Task<bool> ExistsAsync(string key);

    // 读取数据
    T Read&lt;T&gt;(string key);
    T Read&lt;T&gt;(string key, T defaultValue);
    Task&lt;T&gt; ReadAsync&lt;T&gt;(string key);

    // 写入数据
    void Write&lt;T&gt;(string key, T value);
    Task WriteAsync&lt;T&gt;(string key, T value);

    // 删除数据
    void Delete(string key);
    Task DeleteAsync(string key);

    // 目录操作
    Task<IReadOnlyList<string>> ListDirectoriesAsync(string path = "");
    Task<IReadOnlyList<string>> ListFilesAsync(string path = "");
    Task<bool> DirectoryExistsAsync(string path);
    Task CreateDirectoryAsync(string path);
}
```

### 文件存储

`FileStorage` 是基于文件系统的存储实现：

- 将数据序列化后保存为文件
- 支持自定义文件扩展名（默认 `.dat`）
- 使用细粒度锁保证线程安全
- 自动创建目录结构
- 防止路径遍历攻击

### 作用域存储

`ScopedStorage` 提供命名空间隔离：

- 为所有键添加前缀
- 支持嵌套作用域
- 透明包装底层存储
- 实现逻辑分组

### 存储类型

`StorageKinds` 枚举定义了不同的存储方式：

```csharp
[Flags]
public enum StorageKinds
{
    None = 0,
    Local = 1 << 0,      // 本地文件系统
    Memory = 1 << 1,     // 内存存储
    Remote = 1 << 2,     // 远程存储
    Database = 1 << 3    // 数据库存储
}
```

## 基本用法

### 创建文件存储

```csharp
using GFramework.Game.storage;
using GFramework.Game.serializer;

// 创建序列化器
var serializer = new JsonSerializer();

// 创Windows 示例）
var storage = new FileStorage(@"C:\MyGame\Data", serializer);

// 或使用自定义扩展名
var storage = new FileStorage(@"C:\MyGame\Data", serializer, ".json");
```

### 写入和读取数据

```csharp
// 写入简单类型
storage.Write("player_score", 1000);
storage.Write("player_name", "Alice");

// 写入复杂对象
var settings = new GameSettings
{
    Volume = 0.8f,
    Difficulty = "Hard",
    Language = "zh-CN"
};
storage.Write("settings", settings);

// 读取数据
int score = storage.Read<int>("player_score");
string name = storage.Read<string>("player_name");
var loadedSettings = storage.Read<GameSettings>("settings");

// 读取数据（带默认值）
int highScore = storage.Read("high_score", 0);
```

### 异步操作

```csharp
// 异步写入
await storage.WriteAsync("player_level", 10);

// 异步读取
int level = await storage.ReadAsync<int>("player_level");

// 异步检查存在
bool exists = await storage.ExistsAsync("player_level");

// 异步删除
await storage.DeleteAsync("player_level");
```

### 检查和删除

```csharp
// 检查键是否存在
if (storage.Exists("player_score"))
{
    Console.WriteLine("存档存在");
}

// 删除数据
storage.Delete("player_score");

// 异步检查
bool exists = await storage.ExistsAsync("player_score");
```

### 使用层级键

```csharp
// 使用 / 分隔符创建层级结构
storage.Write("player/profile/name", "Alice");
storage.Write("player/profile/level", 10);
storage.Write("player/inventory/gold", 1000);

// 文件结构：
// Data/
//   player/
//     profile/
//       name.dat
//       level.dat
//     inventory/
//       gold.dat

// 读取层级数据
string name = storage.Read<string>("player/profile/name");
int gold = storage.Read<int>("player/inventory/gold");
```

## 作用域存储

### 创建作用域存储

```csharp
using GFramework.Game.storage;

// 基于文件存储创建作用域存储
var baseStorage = new FileStorage(@"C:\MyGame\Data", serializer);
var playerStorage = new ScopedStorage(baseStorage, "player");

// 所有操作都会添加 "player/" 前缀
playerStorage.Write("name", "Alice");      // 实际存储为 "player/name.dat"
playerStorage.Write("level", 10);          // 实际存储为 "player/level.dat"

// 读取时也使用相同的前缀
string name = playerStorage.Read<string>("name");  // 从 "player/name.dat" 读取
```

### 嵌套作用域

```csharp
// 创建嵌套作用域
var settingsStorage = new ScopedStorage(baseStorage, "settings");
var graphicsStorage = new ScopedStorage(settingsStorage, "graphics");

// 前缀变为 "settings/graphics/"
graphicsStorage.Write("resolution", "1920x1080");
// 实际存储为 "settings/graphics/resolution.dat"

// 或使用 Scope 方法
var audioStorage = settingsStorage.Scope("audio");
audioStorage.Write("volume", 0.8f);
// 实际存储为 "settings/audio/volume.dat"
```

### 多作用域隔离

```csharp
// 创建不同作用域的存储
var playerStorage = new ScopedStorage(baseStorage, "player");
var gameStorage = new ScopedStorage(baseStorage, "game");
var settingsStorage = new ScopedStorage(baseStorage, "settings");

// 在不同作用域中使用相同的键不会冲突
playerStorage.Write("level", 5);              // player/level.dat
gameStorage.Write("level", "forest_area_1");  // game/level.dat
settingsStorage.Write("level", "high");       // settings/level.dat

// 读取时各自独立
int playerLevel = playerStorage.Read<int>("level");           // 5
string gameLevel = gameStorage.Read<string>("level");         // "forest_area_1"
string settingsLevel = settingsStorage.Read<string>("level"); // "high"
```

## 高级用法

### 目录操作

```csharp
// 列举子目录
var directories = await storage.ListDirectoriesAsync("player");
foreach (var dir in directories)
{
    Console.WriteLine($"目录: {dir}");
}

// 列举文件
var files = await storage.ListFilesAsync("player/inventory");
foreach (var file in files)
{
    Console.WriteLine($"文件: {file}");
}

// 检查目录是否存在
bool exists = await storage.DirectoryExistsAsync("player/quests");

// 创建目录
await storage.CreateDirectoryAsync("player/achievements");
```

### 批量操作

```csharp
public async Task SaveAllPlayerData(PlayerData player)
{
    var playerStorage = new ScopedStorage(baseStorage, $"player_{player.Id}");

    // 批量写入
    var tasks = new List<Task>
    {
        playerStorage.WriteAsync("profile", player.Profile),
        playerStorage.WriteAsync("inventory", player.Inventory),
        playerStorage.WriteAsync("quests", player.Quests),
        playerStorage.WriteAsync("achievements", player.Achievements)
    };

    await Task.WhenAll(tasks);
    Console.WriteLine("所有玩家数据已保存");
}

public async Task<PlayerData> LoadAllPlayerData(int playerId)
{
    var playerStorage = new ScopedStorage(baseStorage, $"player_{playerId}");

    // 批量读取
    var tasks = new[]
    {
        playerStorage.ReadAsync<Profile>("profile"),
        playerStorage.ReadAsync<Inventory>("inventory"),
        playerStorage.ReadAsync<QuestData>("quests"),
        playerStorage.ReadAsync<Achievements>("achievements")
    };

    await Task.WhenAll(tasks);

    return new PlayerData
    {
        Id = playerId,
        Profile = tasks[0].Result,
        Inventory = tasks[1].Result,
        Quests = tasks[2].Result,
        Achievements = tasks[3].Result
    };
}
```

### 存储迁移

```csharp
public async Task MigrateStorage(IStorage oldStorage, IStorage newStorage, string path = "")
{
    // 列举所有文件
    var files = await oldStorage.ListFilesAsync(path);

    foreach (var file in files)
    {
        var key = string.IsNullOrEmpty(path) ? file : $"{path}/{file}";

        // 读取旧数据
        var data = await oldStorage.ReadAsync<object>(key);

        // 写入新存储
        await newStorage.WriteAsync(key, data);

        Console.WriteLine($"已迁移: {key}");
    }

    // 递归处理子目录
    var directories = await oldStorage.ListDirectoriesAsync(path);
    foreach (var dir in directories)
    {
        var subPath = string.IsNullOrEmpty(path) ? dir : $"{path}/{dir}";
        await MigrateStorage(oldStorage, newStorage, subPath);
    }
}
```

### 存储备份

```csharp
public class StorageBackupSystem
{
    private readonly IStorage _storage;
    private readonly string _backupPrefix = "backup";

    public async Task CreateBackup(string sourcePath)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = $"{_backupPrefix}/{timestamp}";

        await CopyDirectory(sourcePath, backupPath);
        Console.WriteLine($"备份已创建: {backupPath}");
    }

    public async Task RestoreBackup(string backupName, string targetPath)
    {
        var backupPath = $"{_backupPrefix}/{backupName}";

        if (!await _storage.DirectoryExistsAsync(backupPath))
        {
            throw new DirectoryNotFoundException($"备份不存在: {backupName}");
        }

        await CopyDirectory(backupPath, targetPath);
        Console.WriteLine($"已从备份恢复: {backupName}");
    }

    private async Task CopyDirectory(string source, string target)
    {
        var files = await _storage.ListFilesAsync(source);
        foreach (var file in files)
        {
            var sourceKey = $"{source}/{file}";
            var targetKey = $"{target}/{file}";
            var data = await _storage.ReadAsync<object>(sourceKey);
            await _storage.WriteAsync(targetKey, data);
        }

        var directories = await _storage.ListDirectoriesAsync(source);
        foreach (var dir in directories)
        {
            await CopyDirectory($"{source}/{dir}", $"{target}/{dir}");
        }
    }
}
```

### 缓存层

```csharp
public class CachedStorage : IStorage
{
    private readonly IStorage _innerStorage;
    private readonly ConcurrentDictionary<string, object> _cache = new();

    public CachedStorage(IStorage innerStorage)
    {
        _innerStorage = innerStorage;
    }

    public T Read&lt;T&gt;(string key)
    {
        // 先从缓存读取
        if (_cache.TryGetValue(key, out var cached))
        {
            return (T)cached;
        }

        // 从存储读取并缓存
        var value = _innerStorage.Read&lt;T&gt;(key);
        _cache[key] = value;
        return value;
    }

    public void Write&lt;T&gt;(string key, T value)
    {
        // 写入存储
        _innerStorage.Write(key, value);

        // 更新缓存
        _cache[key] = value;
    }

    public void Delete(string key)
    {
        _innerStorage.Delete(key);
        _cache.TryRemove(key, out _);
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
```

## Godot 集成

### 使用 Godot 文件存储

```csharp
using GFramework.Godot.storage;

// 创建 Godot 文件存储
var storage = new GodotFileStorage(serializer);

// 使用 user:// 路径（用户数据目录）
storage.Write("user://saves/slot1.dat", saveData);
var data = storage.Read<SaveData>("user://saves/slot1.dat");

// 使用 res:// 路径（资源目录，只读）
var config = storage.Read<Config>("res://config/default.json");

// 普通文件路径也支持
storage.Write("/tmp/temp_data.dat", tempData);
```

### Godot 路径说明

```csharp
// user:// - 用户数据目录
// Windows: %APPDATA%/Godot/app_userdata/[project_name]
// Linux: ~/.local/share/godot/app_userdata/[project_name]
// macOS: ~/Library/Application Support/Godot/app_userdata/[project_name]
storage.Write("user://save.dat", data);

// res:// - 项目资源目录（只读）
var config = storage.Read<Config>("res://data/config.json");

// 绝对路径
storage.Write("/home/user/game/data.dat", data);
```

## 最佳实践

1. **使用作用域隔离不同类型的数据**
   ```csharp
   ✓ var playerStorage = new ScopedStorage(baseStorage, "player");
   ✓ var settingsStorage = new ScopedStorage(baseStorage, "settings");
   ✗ storage.Write("player_name", name);  // 不使用作用域
   ```

2. **使用异步操作避免阻塞**
   ```csharp
   ✓ await storage.WriteAsync("data", value);
   ✗ storage.Write("data", value);  // 在 UI 线程中同步操作
   ```

3. **读取时提供默认值**
   ```csharp
   ✓ int score = storage.Read("score", 0);
   ✗ int score = storage.Read<int>("score");  // 键不存在时抛异常
   ```

4. **使用层级键组织数据**
   ```csharp
   ✓ storage.Write("player/inventory/gold", 1000);
   ✗ storage.Write("player_inventory_gold", 1000);
   ```

5. **处理存储异常**
   ```csharp
   try
   {
       await storage.WriteAsync("data", value);
   }
   catch (IOException ex)
   {
       Logger.Error($"存储失败: {ex.Message}");
       ShowErrorMessage("保存失败，请检查磁盘空间");
   }
   ```

6. **定期清理过期数据**
   ```csharp
   public async Task CleanupOldData(TimeSpan maxAge)
   {
       var files = await storage.ListFilesAsync("temp");
       foreach (var file in files)
       {
           var data = await storage.ReadAsync<TimestampedData>($"temp/{file}");
           if (DateTime.Now - data.Timestamp > maxAge)
           {
               await storage.DeleteAsync($"temp/{file}");
           }
       }
   }
   ```

7. **使用合适的序列化器**
   ```csharp
   // JSON - 可读性好，适合配置文件
   var jsonStorage = new FileStorage(path, new JsonSerializer(), ".json");

   // 二进制 - 性能好，适合大量数据
   var binaryStorage = new FileStorage(path, new BinarySerializer(), ".dat");
   ```

## 常见问题

### 问题：如何实现跨平台存储路径？

**解答**：
使用 `Environment.GetFolderPath` 获取平台特定路径：

```csharp
public static string GetStoragePath()
{
    var appData = Environment.GetFolderPath(
        Environment.SpecialFolder.ApplicationData);
    return Path.Combine(appData, "MyGame", "Data");
}

var storage = new FileStorage(GetStoragePath(), serializer);
```

### 问题：存储系统是否线程安全？

**解答**：
是的，`FileStorage` 使用细粒度锁机制保证线程安全：

```csharp
// 不同键的操作可以并发执行
Task.Run(() => storage.Write("key1", value1));
Task.Run(() => storage.Write("key2", value2));

// 相同键的操作会串行化
Task.Run(() => storage.Write("key", value1));
Task.Run(() => storage.Write("key", value2));  // 等待第一个完成
```

### 问题：如何实现存储加密？

**解答**：
创建加密存储包装器：

```csharp
public class EncryptedStorage : IStorage
{
    private readonly IStorage _innerStorage;
    private readonly IEncryption _encryption;

    public void Write&lt;T&gt;(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        var encrypted = _encryption.Encrypt(json);
        _innerStorage.Write(key, encrypted);
    }

    public T Read&lt;T&gt;(string key)
    {
        var encrypted = _innerStorage.Read<byte[]>(key);
        var json = _encryption.Decrypt(encrypted);
        return JsonSerializer.Deserialize&lt;T&gt;(json);
    }
}
```

### 问题：如何限制存储大小？

**解答**：
实现配额管理：

```csharp
public class QuotaStorage : IStorage
{
    private readonly IStorage _innerStorage;
    private readonly long _maxSize;
    private long _currentSize;

    public void Write&lt;T&gt;(string key, T value)
    {
        var data = Serialize(value);
        var size = data.Length;

        if (_currentSize + size > _maxSize)
        {
            throw new InvalidOperationException("存储配额已满");
        }

        _innerStorage.Write(key, value);
        _currentSize += size;
    }
}
```

### 问题：如何实现存储压缩？

**解答**：
使用压缩序列化器：

```csharp
public class CompressedSerializer : ISerializer
{
    private readonly ISerializer _innerSerializer;

    public string Serialize&lt;T&gt;(T value)
    {
        var json = _innerSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json);
        var compressed = Compress(bytes);
        return Convert.ToBase64String(compressed);
    }

    public T Deserialize&lt;T&gt;(string data)
    {
        var compressed = Convert.FromBase64String(data);
        var bytes = Decompress(compressed);
        var json = Encoding.UTF8.GetString(bytes);
        return _innerSerializer.Deserialize&lt;T&gt;(json);
    }

    private byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}
```

### 问题：如何监控存储操作？

**解答**：
实现日志存储包装器：

```csharp
public class LoggingStorage : IStorage
{
    private readonly IStorage _innerStorage;
    private readonly ILogger _logger;

    public void Write&lt;T&gt;(string key, T value)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _innerStorage.Write(key, value);
            _logger.Info($"写入成功: {key}, 耗时: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.Error($"写入失败: {key}, 错误: {ex.Message}");
            throw;
        }
    }

    public T Read&lt;T&gt;(string key)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var value = _innerStorage.Read&lt;T&gt;(key);
            _logger.Info($"读取成功: {key}, 耗时: {stopwatch.ElapsedMilliseconds}ms");
            return value;
        }
        catch (Exception ex)
        {
            _logger.Error($"读取失败: {key}, 错误: {ex.Message}");
            throw;
        }
    }
}
```

## 相关文档

- [数据与存档系统](/zh-CN/game/data) - 数据持久化
- [序列化系统](/zh-CN/game/serialization) - 数据序列化
- [Godot 集成](/zh-CN/godot/index) - Godot 中的存储
- [存档系统教程](/zh-CN/tutorials/save-system) - 完整示例
