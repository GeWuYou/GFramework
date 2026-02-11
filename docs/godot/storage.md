# 存储模块 (Storage Module)

## 概述

存储模块是 GFramework.Godot 的核心存储实现，专门为 Godot 引擎设计的文件存储系统。该模块支持 Godot 的虚拟路径系统（如
`res://` 和 `user://`），并提供了按键级别的细粒度锁机制来保证线程安全。

## 核心类

### GodotFileStorage

Godot 特化的文件存储实现，实现了 `IStorage` 接口。

**主要特性：**

- ✅ Godot 虚拟路径支持（`res://`, `user://`）
- ✅ 线程安全（按键级别的细粒度锁）
- ✅ 同步/异步读写操作
- ✅ 自动创建目录结构
- ❌ 删除操作（Delete 方法未实现）

## 功能特性

### 路径处理

该存储系统支持三种路径类型：

#### 1. Godot 资源路径 (`res://`)

- **用途**：存储游戏资源文件
- **特点**：只读，包含在游戏构建中
- **示例**：`res://config/game_settings.json`

#### 2. Godot 用户数据路径 (`user://`)

- **用途**：存储用户数据、存档、配置等
- **特点**：可读写，游戏可访问的用户目录
- **示例**：`user://saves/save_001.dat`

#### 3. 普通文件系统路径

- **用途**：存储临时文件或调试数据
- **特点**：完整的文件系统访问
- **示例**：`C:/Games/MyGame/logs/debug.log`

### 路径验证与清理

```mermaid
graph TD
    A[输入路径] --> B{包含 ".." ?}
    B -->|是| C[抛出异常]
    B -->|否| D{是 Godot 路径?}
    D -->|是| E[直接使用]
    D -->|否| F[清理路径段]
    F --> G[替换无效字符]
    G --> H[创建目录结构]
    H --> I[返回绝对路径]
    C --> J[结束]
    E --> J
    I --> J
```

### 线程安全机制

每个文件路径都有独立的锁对象，确保：

1. **细粒度锁** - 不同文件可以并发访问
2. **避免死锁** - 锁的获取顺序一致
3. **高性能** - 减少锁竞争

## API 接口

### IStorage 接口

```csharp
public interface IStorage
{
    // 读取操作
    T Read<T>(string key);
    T Read<T>(string key, T defaultValue);
    Task<T> ReadAsync<T>(string key);
    
    // 写入操作
    void Write<T>(string key, T value);
    Task WriteAsync<T>(string key, T value);
    
    // 检查存在性
    bool Exists(string key);
    Task<bool> ExistsAsync(string key);
    
    // 删除操作（未实现）
    void Delete(string key);
}
```

## 使用示例

### 基本使用

```csharp
// 创建存储实例（需要序列化器）
var serializer = new JsonSerializer(); // 或其他序列化器
var storage = new GodotFileStorage(serializer);

// 写入用户数据
var userData = new UserData
{
    PlayerName = "Alice",
    Level = 5,
    Score = 1000
};
storage.Write("user://player.dat", userData);

// 读取用户数据
var loadedData = storage.Read<UserData>("user://player.dat");
Console.WriteLine($"Player: {loadedData.PlayerName}, Level: {loadedData.Level}");
```

### 异步操作

```csharp
// 异步写入游戏配置
var config = new GameConfig
{
    Resolution = "1920x1080",
    Fullscreen = true,
    Volume = 0.8f
};
await storage.WriteAsync("user://config.json", config);

// 异步读取配置
var loadedConfig = await storage.ReadAsync<GameConfig>("user://config.json");
```

### 不同路径类型使用

```csharp
// 读取游戏资源（只读）
var levelData = storage.Read<LevelData>("res://levels/level_001.json");

// 存储用户存档
var saveData = new SaveData { /* ... */ };
storage.Write("user://saves/slot_001.dat", saveData);

// 存储调试信息（普通路径）
var debugLog = new DebugLog { /* ... */ };
storage.Write("logs/debug_" + DateTime.Now.Ticks + ".json", debugLog);
```

### 存在性检查

```csharp
// 检查文件是否存在
if (storage.Exists("user://settings.json"))
{
    var settings = storage.Read<AppSettings>("user://settings.json");
    // 使用设置...
}
else
{
    // 使用默认设置
    var defaultSettings = new AppSettings();
    storage.Write("user://settings.json", defaultSettings);
}
```

### 带默认值的读取

```csharp
// 尝试读取，如果文件不存在则返回默认值
var settings = storage.Read("user://user_prefs.json", new UserPrefs
{
    Language = "en",
    Volume = 1.0f,
    Difficulty = 1
});
```

## 路径扩展

该模块使用了路径扩展方法：

```csharp
public static class GodotPathExtensions
{
    public static bool IsUserPath(this string path);
    public static bool IsResPath(this string path);
    public static bool IsGodotPath(this string path);
}
```

**使用示例：**

```csharp
string path1 = "user://save.dat";
string path2 = "res://config.json";
string path3 = "C:/temp/file.txt";

Console.WriteLine(path1.IsGodotPath()); // true
Console.WriteLine(path1.IsUserPath());  // true
Console.WriteLine(path2.IsResPath());    // true
Console.WriteLine(path3.IsGodotPath()); // false
```

## 性能考虑

### 1. 锁机制

- 每个文件路径独立锁，减少锁竞争
- 读写操作串行化，避免数据损坏

### 2. 文件访问

- Godot 虚拟路径使用 `FileAccess` API
- 普通路径使用标准 .NET 文件 I/O
- 自动创建目录结构

### 3. 内存使用

- 锁对象使用 `ConcurrentDictionary` 管理
- 锁对象按需创建，避免内存泄漏

## 错误处理

### 常见异常

1. **ArgumentException** - 路径参数无效
    - 空路径
    - 包含 ".." 的路径
    - 无效的存储键

2. **FileNotFoundException** - 文件不存在
    - 读取不存在的文件时抛出

3. **IOException** - 文件操作失败
    - 写入权限不足
    - 磁盘空间不足

### 错误处理示例

```csharp
try
{
    var data = storage.Read<UserData>("user://save.dat");
}
catch (FileNotFoundException)
{
    Console.WriteLine("存档文件不存在，创建新的存档");
    var newSave = new UserData();
    storage.Write("user://save.dat", newSave);
}
catch (Exception ex)
{
    Console.WriteLine($"读取存档失败: {ex.Message}");
}
```

## 最佳实践

1. **路径选择**
    - 游戏资源使用 `res://`
    - 用户数据使用 `user://`
    - 调试/临时文件使用普通路径

2. **异常处理**
    - 总是处理 `FileNotFoundException`
    - 使用带默认值的 `Read` 重载方法

3. **性能优化**
    - 批量读写时使用异步方法
    - 避免频繁的小文件操作

4. **序列化器选择**
    - JSON：人类可读，调试友好
    - 二进制：性能更好，文件更小

## 相关链接

- [路径扩展](../extensions/README.md#godotpathextensions)
- [设置系统](../setting/README.md)
- [抽象接口定义](../../../GFramework.Core/Abstractions/storage/)