# GFramework 存储模块使用指南

本模块提供了基于文件系统的存储功能，包括两个主要类：FileStorage
和 ScopedStorage。

## FileStorage

FileStorage 是一个基于文件系统的存储实现，它将数据以序列化形式保存到磁盘上的指定目录。

### 特性

- 将数据保存为文件（默认扩展名为 `.dat`）
- 支持同步和异步操作
- 自动创建存储目录
- 防止路径遍历攻击（过滤非法文件名字符）
- 通过细粒度锁机制保证线程安全（每个键都有独立的锁）

### 构造函数

```csharp
FileStorage(string rootPath, ISerializer serializer, string extension = ".dat")
```

### 使用示例

```csharp
// 创建 JSON 序列化器
var serializer = new JsonSerializer();

// 创建文件存储实例
var storage = new FileStorage(@"C:\MyGame\Data", serializer);

// 写入数据
storage.Write("player_score", 1000);
storage.Write("player_settings", new { Volume = 0.8f, Difficulty = "Hard" });

// 读取数据
int score = storage.Read<int>("player_score");
var settings = storage.Read("player_settings", new { Volume = 0.5f, Difficulty = "Normal" });

// 异步读取数据
Task<int> scoreTask = storage.ReadAsync<int>("player_score");

// 检查数据是否存在
if (storage.Exists("player_score"))
{
    // 数据存在
}

// 异步检查数据是否存在
Task<bool> existsTask = storage.ExistsAsync("player_score");

// 删除数据
storage.Delete("player_score");

// 异步写入数据
storage.WriteAsync("player_score", 1200);
```

## ScopedStorage

ScopedStorage 是一个装饰器模式的实现，它为所有存储键添加前缀，从而实现逻辑分组和命名空间隔离。

### 特性

- 为所有键添加指定前缀
- 透明地包装底层存储实现
- 支持嵌套作用域
- 与底层存储共享物理存储
- 支持同步和异步操作

### 构造函数

```csharp
ScopedStorage(IStorage inner, string prefix)
```

### 使用示例

```csharp
// 基于文件存储创建带作用域的存储
var scopedStorage = new ScopedStorage(fileStorage, "game_settings");

// 所有操作都会添加前缀 "game_settings/"
scopedStorage.Write("volume", 0.8f);      // 实际存储为 "game_settings/volume.dat"
scopedStorage.Write("theme", "dark");     // 实际存储为 "game_settings/theme.dat"

// 读取操作同样适用前缀
float volume = scopedStorage.Read<float>("volume");  // 从 "game_settings/volume.dat" 读取

// 创建嵌套作用域
var nestedStorage = scopedStorage.Scope("graphics"); // 前缀变为 "game_settings/graphics/"
nestedStorage.Write("resolution", "1920x1080");      // 实际存储为 "game_settings/graphics/resolution.dat"

// 异步操作
await scopedStorage.WriteAsync("audio", new { MasterVolume = 0.9f });
var audioSettings = await scopedStorage.ReadAsync<object>("audio");
```

## 组合使用示例

```csharp
// 创建基础序列化器和文件存储
var serializer = new JsonSerializer();
var baseStorage = new FileStorage(@"C:\MyGame\Data", serializer);

// 创建不同作用域的存储
var playerStorage = new ScopedStorage(baseStorage, "player");
var gameStorage = new ScopedStorage(baseStorage, "game");
var settingsStorage = new ScopedStorage(baseStorage, "settings");

// 在不同的作用域中使用相同键而不会冲突
playerStorage.Write("level", 5);
gameStorage.Write("level", "forest_area_1");
settingsStorage.Write("level", "high");

// 结果是三个不同的文件：
// - player/level.dat
// - game/level.dat
// - settings/level.dat
```

## 注意事项

1. **序列化器选择**：确保使用的 ISerializer
   实现能够正确处理你要存储的数据类型。
2. **错误处理**：FileStorage 的 `Read<T>(string key)` 方法会在键不存在时抛出异常，可以使用
   `Read<T>(string key, T defaultValue)` 来避免异常。
3. **线程安全**：FileStorage 通过细粒度锁机制保证线程安全，每个键都有独立的锁，因此不同键的操作可以并发执行。
4. **文件权限**：确保应用程序对指定的存储目录有读写权限。
5. **路径安全**：FileStorage 会自动防止路径遍历攻击，因此键不能包含 `..`，并且特殊字符会被替换为下划线。
6. **存储键格式**：键可以包含 `/` 作为分隔符，这将被转换为相应的目录层级，例如 `"player/profile"` 会存储在
   `player/profile.dat` 文件中。
7. **异步操作**：尽管异步读写操作使用了异步IO，但仍会使用锁来保证对同一键的并发访问安全。