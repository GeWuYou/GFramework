---
title: 序列化系统
description: 序列化系统提供了统一的对象序列化和反序列化接口，支持 JSON 格式和运行时类型处理。
---

# 序列化系统

## 概述

序列化系统是 GFramework.Game 中用于对象序列化和反序列化的核心组件。它提供了统一的序列化接口，支持将对象转换为字符串格式（如
JSON）进行存储或传输，并能够将字符串数据还原为对象。

序列化系统与数据存储、配置管理、存档系统等模块深度集成，为游戏数据的持久化提供了基础支持。

**主要特性**：

- 统一的序列化接口
- JSON 格式支持
- 运行时类型序列化
- 泛型和非泛型 API
- 与存储系统无缝集成
- 类型安全的反序列化

## 核心概念

### 序列化器接口

`ISerializer` 定义了基本的序列化操作：

```csharp
public interface ISerializer : IUtility
{
    // 将对象序列化为字符串
    string Serialize&lt;T&gt;(T value);

    // 将字符串反序列化为对象
    T Deserialize&lt;T&gt;(string data);
}
```

### 运行时类型序列化器

`IRuntimeTypeSerializer` 扩展了基本接口，支持运行时类型处理：

```csharp
public interface IRuntimeTypeSerializer : ISerializer
{
    // 使用运行时类型序列化对象
    string Serialize(object obj, Type type);

    // 使用运行时类型反序列化对象
    object Deserialize(string data, Type type);
}
```

### JSON 序列化器

`JsonSerializer` 是基于 Newtonsoft.Json 的实现：

```csharp
public sealed class JsonSerializer : IRuntimeTypeSerializer
{
    string Serialize&lt;T&gt;(T value);
    T Deserialize&lt;T&gt;(string data);
    string Serialize(object obj, Type type);
    object Deserialize(string data, Type type);
}
```

## 基本用法

### 注册序列化器

在架构中注册序列化器：

```csharp
using GFramework.Core.Abstractions.serializer;
using GFramework.Game.serializer;

public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册 JSON 序列化器
        var jsonSerializer = new JsonSerializer();
        RegisterUtility<ISerializer>(jsonSerializer);
        RegisterUtility<IRuntimeTypeSerializer>(jsonSerializer);
    }
}
```

### 序列化对象

使用泛型 API 序列化对象：

```csharp
using GFramework.SourceGenerators.Abstractions.rule;

public class PlayerData
{
    public string Name { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
}

[ContextAware]
public partial class SaveController : IController
{
    public void SavePlayer()
    {
        var serializer = this.GetUtility<ISerializer>();

        var player = new PlayerData
        {
            Name = "Player1",
            Level = 10,
            Experience = 1000
        };

        // 序列化为 JSON 字符串
        string json = serializer.Serialize(player);
        Console.WriteLine(json);
        // 输出: {"Name":"Player1","Level":10,"Experience":1000}
    }
}
```

### 反序列化对象

从字符串还原对象：

```csharp
public void LoadPlayer()
{
    var serializer = this.GetUtility<ISerializer>();

    string json = "{\"Name\":\"Player1\",\"Level\":10,\"Experience\":1000}";

    // 反序列化为对象
    var player = serializer.Deserialize<PlayerData>(json);

    Console.WriteLine($"玩家: {player.Name}, 等级: {player.Level}");
}
```

### 运行时类型序列化

处理不确定类型的对象：

```csharp
public void SerializeRuntimeType()
{
    var serializer = this.GetUtility<IRuntimeTypeSerializer>();

    object data = new PlayerData { Name = "Player1", Level = 10 };
    Type dataType = data.GetType();

    // 使用运行时类型序列化
    string json = serializer.Serialize(data, dataType);

    // 使用运行时类型反序列化
    object restored = serializer.Deserialize(json, dataType);

    var player = restored as PlayerData;
    Console.WriteLine($"玩家: {player?.Name}");
}
```

## 高级用法

### 与存储系统集成

序列化器与存储系统配合使用：

```csharp
using GFramework.Core.Abstractions.storage;
using GFramework.Game.storage;
using GFramework.SourceGenerators.Abstractions.rule;

[ContextAware]
public partial class DataManager : IController
{
    public async Task SaveData()
    {
        var serializer = this.GetUtility<ISerializer>();
        var storage = this.GetUtility<IStorage>();

        var gameData = new GameData
        {
            Score = 1000,
            Coins = 500
        };

        // 序列化数据
        string json = serializer.Serialize(gameData);

        // 写入存储
        await storage.WriteAsync("game_data", json);
    }

    public async Task<GameData> LoadData()
    {
        var serializer = this.GetUtility<ISerializer>();
        var storage = this.GetUtility<IStorage>();

        // 从存储读取
        string json = await storage.ReadAsync<string>("game_data");

        // 反序列化数据
        return serializer.Deserialize<GameData>(json);
    }
}
```

### 序列化复杂对象

处理嵌套和集合类型：

```csharp
public class InventoryData
{
    public List<ItemData> Items { get; set; }
    public Dictionary<string, int> Resources { get; set; }
}

public class ItemData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
}

public void SerializeComplexData()
{
    var serializer = this.GetUtility<ISerializer>();

    var inventory = new InventoryData
    {
        Items = new List<ItemData>
        {
            new ItemData { Id = "sword_01", Name = "铁剑", Quantity = 1 },
            new ItemData { Id = "potion_hp", Name = "生命药水", Quantity = 5 }
        },
        Resources = new Dictionary<string, int>
        {
            { "gold", 1000 },
            { "wood", 500 }
        }
    };

    // 序列化复杂对象
    string json = serializer.Serialize(inventory);

    // 反序列化
    var restored = serializer.Deserialize<InventoryData>(json);

    Console.WriteLine($"物品数量: {restored.Items.Count}");
    Console.WriteLine($"金币: {restored.Resources["gold"]}");
}
```

### 处理多态类型

序列化继承层次结构：

```csharp
public abstract class EntityData
{
    public string Id { get; set; }
    public string Type { get; set; }
}

public class PlayerEntityData : EntityData
{
    public int Level { get; set; }
    public int Experience { get; set; }
}

public class EnemyEntityData : EntityData
{
    public int Health { get; set; }
    public int Damage { get; set; }
}

public void SerializePolymorphic()
{
    var serializer = this.GetUtility<IRuntimeTypeSerializer>();

    // 创建不同类型的实体
    EntityData player = new PlayerEntityData
    {
        Id = "player_1",
        Type = "Player",
        Level = 10,
        Experience = 1000
    };

    EntityData enemy = new EnemyEntityData
    {
        Id = "enemy_1",
        Type = "Enemy",
        Health = 100,
        Damage = 20
    };

    // 使用运行时类型序列化
    string playerJson = serializer.Serialize(player, player.GetType());
    string enemyJson = serializer.Serialize(enemy, enemy.GetType());

    // 根据类型反序列化
    var restoredPlayer = serializer.Deserialize(playerJson, typeof(PlayerEntityData));
    var restoredEnemy = serializer.Deserialize(enemyJson, typeof(EnemyEntityData));
}
```

### 自定义序列化逻辑

虽然 GFramework 使用 Newtonsoft.Json，但你可以通过特性控制序列化行为：

```csharp
using Newtonsoft.Json;

public class CustomData
{
    // 忽略此属性
    [JsonIgnore]
    public string InternalId { get; set; }

    // 使用不同的属性名
    [JsonProperty("player_name")]
    public string Name { get; set; }

    // 仅在值不为 null 时序列化
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? OptionalField { get; set; }

    // 格式化日期
    [JsonProperty("created_at")]
    [JsonConverter(typeof(IsoDateTimeConverter))]
    public DateTime CreatedAt { get; set; }
}
```

### 批量序列化

处理多个对象的序列化：

```csharp
public async Task SaveMultipleData()
{
    var serializer = this.GetUtility<ISerializer>();
    var storage = this.GetUtility<IStorage>();

    var dataList = new Dictionary<string, object>
    {
        { "player", new PlayerData { Name = "Player1", Level = 10 } },
        { "inventory", new InventoryData { Items = new List<ItemData>() } },
        { "settings", new SettingsData { Volume = 0.8f } }
    };

    // 批量序列化和保存
    foreach (var (key, data) in dataList)
    {
        string json = serializer.Serialize(data);
        await storage.WriteAsync(key, json);
    }

    Console.WriteLine($"已保存 {dataList.Count} 个数据文件");
}
```

### 错误处理

处理序列化和反序列化错误：

```csharp
public void SafeDeserialize()
{
    var serializer = this.GetUtility<ISerializer>();

    string json = "{\"Name\":\"Player1\",\"Level\":\"invalid\"}"; // 错误的数据

    try
    {
        var player = serializer.Deserialize<PlayerData>(json);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"反序列化失败: {ex.Message}");
        // 返回默认值或重新尝试
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"JSON 格式错误: {ex.Message}");
    }
}

public PlayerData DeserializeWithFallback(string json)
{
    var serializer = this.GetUtility<ISerializer>();

    try
    {
        return serializer.Deserialize<PlayerData>(json);
    }
    catch
    {
        // 返回默认数据
        return new PlayerData
        {
            Name = "DefaultPlayer",
            Level = 1,
            Experience = 0
        };
    }
}
```

### 版本兼容性

处理数据结构变化：

```csharp
// 旧版本数据
public class PlayerDataV1
{
    public string Name { get; set; }
    public int Level { get; set; }
}

// 新版本数据（添加了新字段）
public class PlayerDataV2
{
    public string Name { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; } = 0;  // 新增字段，提供默认值
    public DateTime LastLogin { get; set; } = DateTime.Now;  // 新增字段
}

public PlayerDataV2 LoadWithMigration(string json)
{
    var serializer = this.GetUtility<ISerializer>();

    try
    {
        // 尝试加载新版本
        return serializer.Deserialize<PlayerDataV2>(json);
    }
    catch
    {
        // 如果失败，尝试加载旧版本并迁移
        var oldData = serializer.Deserialize<PlayerDataV1>(json);
        return new PlayerDataV2
        {
            Name = oldData.Name,
            Level = oldData.Level,
            Experience = oldData.Level * 100,  // 根据等级计算经验
            LastLogin = DateTime.Now
        };
    }
}
```

## 最佳实践

1. **使用接口而非具体类型**：依赖 `ISerializer` 接口
   ```csharp
   ✓ var serializer = this.GetUtility<ISerializer>();
   ✗ var serializer = new JsonSerializer(); // 避免直接实例化
   ```

2. **为数据类提供默认值**：确保反序列化的健壮性
   ```csharp
   public class GameData
   {
       public string Name { get; set; } = "Default";
       public int Score { get; set; } = 0;
       public List<string> Items { get; set; } = new();
   }
   ```

3. **处理反序列化异常**：避免程序崩溃
   ```csharp
   try
   {
       var data = serializer.Deserialize<GameData>(json);
   }
   catch (Exception ex)
   {
       Logger.Error($"反序列化失败: {ex.Message}");
       return GetDefaultData();
   }
   ```

4. **避免序列化敏感数据**：使用 `[JsonIgnore]` 标记
   ```csharp
   public class UserData
   {
       public string Username { get; set; }

       [JsonIgnore]
       public string Password { get; set; }  // 不序列化密码
   }
   ```

5. **使用运行时类型处理多态**：保持类型信息
   ```csharp
   var serializer = this.GetUtility<IRuntimeTypeSerializer>();
   string json = serializer.Serialize(obj, obj.GetType());
   ```

6. **验证反序列化的数据**：确保数据完整性
   ```csharp
   var data = serializer.Deserialize<GameData>(json);
   if (string.IsNullOrEmpty(data.Name) || data.Score < 0)
   {
       throw new InvalidDataException("数据验证失败");
   }
   ```

## 性能优化

### 减少序列化开销

```csharp
// 避免频繁序列化大对象
public class CachedSerializer
{
    private string? _cachedJson;
    private GameData? _cachedData;

    public string GetJson(GameData data)
    {
        if (_cachedData == data && _cachedJson != null)
        {
            return _cachedJson;
        }

        var serializer = GetSerializer();
        _cachedJson = serializer.Serialize(data);
        _cachedData = data;
        return _cachedJson;
    }
}
```

### 异步序列化

```csharp
public async Task SaveDataAsync()
{
    var serializer = this.GetUtility<ISerializer>();
    var storage = this.GetUtility<IStorage>();

    var data = GetLargeData();

    // 在后台线程序列化
    string json = await Task.Run(() => serializer.Serialize(data));

    // 异步写入存储
    await storage.WriteAsync("large_data", json);
}
```

### 分块序列化

```csharp
public async Task SaveLargeDataset()
{
    var serializer = this.GetUtility<ISerializer>();
    var storage = this.GetUtility<IStorage>();

    var largeDataset = GetLargeDataset();

    // 分块保存
    const int chunkSize = 100;
    for (int i = 0; i < largeDataset.Count; i += chunkSize)
    {
        var chunk = largeDataset.Skip(i).Take(chunkSize).ToList();
        string json = serializer.Serialize(chunk);
        await storage.WriteAsync($"data_chunk_{i / chunkSize}", json);
    }
}
```

## 常见问题

### 问题：如何序列化循环引用的对象？

**解答**：
Newtonsoft.Json 默认不支持循环引用，需要配置：

```csharp
// 注意：GFramework 的 JsonSerializer 使用默认设置
// 如需处理循环引用，避免创建循环引用的数据结构
// 或使用 [JsonIgnore] 打破循环

public class Node
{
    public string Name { get; set; }
    public List<Node> Children { get; set; }

    [JsonIgnore]  // 忽略父节点引用，避免循环
    public Node? Parent { get; set; }
}
```

### 问题：序列化后的 JSON 太大怎么办？

**解答**：
使用压缩或分块存储：

```csharp
public async Task SaveCompressed()
{
    var serializer = this.GetUtility<ISerializer>();
    var storage = this.GetUtility<IStorage>();

    var data = GetLargeData();
    string json = serializer.Serialize(data);

    // 压缩 JSON
    byte[] compressed = Compress(json);

    // 保存压缩数据
    await storage.WriteAsync("data_compressed", compressed);
}

private byte[] Compress(string text)
{
    using var output = new MemoryStream();
    using (var gzip = new GZipStream(output, CompressionMode.Compress))
    using (var writer = new StreamWriter(gzip))
    {
        writer.Write(text);
    }
    return output.ToArray();
}
```

### 问题：如何处理不同平台的序列化差异？

**解答**：
使用平台无关的数据类型：

```csharp
public class CrossPlatformData
{
    // 使用 string 而非 DateTime（避免时区问题）
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    // 使用 double 而非 float（精度一致）
    public double Score { get; set; }

    // 明确指定编码
    public string Text { get; set; }
}
```

### 问题：反序列化失败时如何恢复？

**解答**：
实现备份和恢复机制：

```csharp
public async Task<GameData> LoadWithBackup(string key)
{
    var serializer = this.GetUtility<ISerializer>();
    var storage = this.GetUtility<IStorage>();

    try
    {
        // 尝试加载主数据
        string json = await storage.ReadAsync<string>(key);
        return serializer.Deserialize<GameData>(json);
    }
    catch
    {
        // 尝试加载备份
        try
        {
            string backupJson = await storage.ReadAsync<string>($"{key}_backup");
            return serializer.Deserialize<GameData>(backupJson);
        }
        catch
        {
            // 返回默认数据
            return new GameData();
        }
    }
}
```

### 问题：如何加密序列化的数据？

**解答**：
在序列化后加密：

```csharp
public async Task SaveEncrypted(string key, GameData data)
{
    var serializer = this.GetUtility<ISerializer>();
    var storage = this.GetUtility<IStorage>();

    // 序列化
    string json = serializer.Serialize(data);

    // 加密
    byte[] encrypted = EncryptString(json);

    // 保存
    await storage.WriteAsync(key, encrypted);
}

public async Task<GameData> LoadEncrypted(string key)
{
    var serializer = this.GetUtility<ISerializer>();
    var storage = this.GetUtility<IStorage>();

    // 读取
    byte[] encrypted = await storage.ReadAsync<byte[]>(key);

    // 解密
    string json = DecryptToString(encrypted);

    // 反序列化
    return serializer.Deserialize<GameData>(json);
}
```

### 问题：序列化器是线程安全的吗？

**解答**：
`JsonSerializer` 本身是线程安全的，但建议通过架构的 Utility 系统访问：

```csharp
// 线程安全的访问方式
public async Task ParallelSave()
{
    var tasks = Enumerable.Range(0, 10).Select(async i =>
    {
        var serializer = this.GetUtility<ISerializer>();
        var data = new GameData { Score = i };
        string json = serializer.Serialize(data);
        await SaveToStorage($"data_{i}", json);
    });

    await Task.WhenAll(tasks);
}
```

## 相关文档

- [数据与存档系统](/zh-CN/game/data) - 数据持久化
- [存储系统](/zh-CN/game/storage) - 文件存储
- [设置系统](/zh-CN/game/setting) - 设置数据序列化
- [Utility 系统](/zh-CN/core/utility) - 工具类注册
