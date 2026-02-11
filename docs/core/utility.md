# Utility 包使用说明

## 概述

Utility 包定义了工具类层。Utility 提供无状态的辅助功能，如数学计算、文件操作、序列化等通用工具方法。与 System 不同，Utility
不依赖架构状态，是纯粹的工具函数集合。

## 核心接口

### IUtility

Utility 标记接口，所有工具类都应实现此接口。

**接口定义：**

```csharp
public interface IUtility
{
    // 标记接口，无方法定义
}
```

### IContextUtility

上下文工具接口，扩展了IUtility接口，为需要感知架构上下文的工具类提供基础能力。

**接口定义：**

```csharp
public interface IContextUtility : IUtility
{
    void Init();  // 初始化上下文工具
}
```

## 核心类

### [`AbstractContextUtility`](AbstractContextUtility.cs)

抽象上下文工具类，提供上下文相关的通用功能实现。继承自 ContextAwareBase 并实现 IContextUtility 接口。

**使用方式：**

```csharp
public abstract class AbstractContextUtility : ContextAwareBase, IContextUtility
{
    protected ILogger Logger = null!;
    
    void IContextUtility.Init() 
    {
        var name = GetType().Name;
        Logger = LoggerFactoryResolver.Provider.CreateLogger(name);
        Logger.Debug($"Initializing Context Utility: {name}");
        
        OnInit();  // 子类实现初始化逻辑
        
        Logger.Info($"Context Utility initialized: {name}");
    }
    
    protected abstract void OnInit();  // 子类实现具体的初始化逻辑
}
```

## 基本使用

### 1. 定义 Utility

```csharp
// 存储工具类，继承自AbstractContextUtility
public class StorageUtility : AbstractContextUtility
{
    private const string SavePath = "user://save_data.json";
    
    protected override void OnInit()
    {
        Logger.Info("StorageUtility initialized");
    }
    
    public void Save<T>(T data)
    {
        string json = JsonSerializer.Serialize(data);
        // 实际保存逻辑
        File.WriteAllText(SavePath, json);
    }
    
    public T Load<T>()
    {
        if (!File.Exists(SavePath))
            return default(T);
            
        string json = File.ReadAllText(SavePath);
        return JsonSerializer.Deserialize<T>(json);
    }
    
    public void Delete()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
    }
}

// 数学工具类，作为普通Utility
public class MathUtility : IUtility
{
    public float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0f, 1f);
    }
    
    public bool IsInRange(float value, float min, float max)
    {
        return value >= min && value <= max;
    }
}

// 时间工具类
public class TimeUtility : IUtility
{
    public string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        return $"{minutes:D2}:{secs:D2}";
    }
    
    public long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
    
    public bool IsExpired(long timestamp, int durationSeconds)
    {
        return GetCurrentTimestamp() > timestamp + durationSeconds;
    }
}
```

### 2. 注册 Utility

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册 Utility（不需要初始化）
        this.RegisterUtility<StorageUtility>(new StorageUtility());
        this.RegisterUtility<MathUtility>(new MathUtility());
        this.RegisterUtility<TimeUtility>(new TimeUtility());
    }
}
```

### 3. 使用 Utility

```csharp
// 在 System 中使用
public class SaveSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<SaveGameEvent>(OnSaveGame);
        this.RegisterEvent<LoadGameEvent>(OnLoadGame);
    }
    
    private void OnSaveGame(SaveGameEvent e)
    {
        var storage = this.GetUtility<StorageUtility>();
        var playerModel = this.GetModel<PlayerModel>();
        
        var saveData = new SaveData
        {
            PlayerName = playerModel.Name.Value,
            Level = playerModel.Level.Value,
            Gold = playerModel.Gold.Value,
            Timestamp = this.GetUtility<TimeUtility>().GetCurrentTimestamp()
        };
        
        storage.Save(saveData);
        this.SendEvent(new GameSavedEvent());
    }
    
    private void OnLoadGame(LoadGameEvent e)
    {
        var storage = this.GetUtility<StorageUtility>();
        var saveData = storage.Load<SaveData>();
        
        if (saveData != null)
        {
            var playerModel = this.GetModel<PlayerModel>();
            playerModel.Name.Value = saveData.PlayerName;
            playerModel.Level.Value = saveData.Level;
            playerModel.Gold.Value = saveData.Gold;
            
            this.SendEvent(new GameLoadedEvent());
        }
    }
}

// 在 Command 中使用
public class MovePlayerCommand : AbstractCommand
{
    public Vector3 TargetPosition { get; set; }
    public float Speed { get; set; }
    
    protected override void OnExecute()
    {
        var playerModel = this.GetModel<PlayerModel>();
        var mathUtil = this.GetUtility<MathUtility>();
        
        // 使用工具类计算
        Vector3 currentPos = playerModel.Position.Value;
        Vector3 direction = (TargetPosition - currentPos).Normalized();
        Vector3 newPos = currentPos + direction * Speed;
        
        playerModel.Position.Value = newPos;
    }
}
```

## 常见 Utility 类型

### 1. 序列化/反序列化工具

```csharp
public class JsonUtility : IUtility
{
    public string Serialize<T>(T obj)
    {
        return Json.Stringify(obj);
    }
    
    public T Deserialize<T>(string json) where T : new()
    {
        return Json.Parse<T>(json);
    }
    
    public bool TryDeserialize<T>(string json, out T result) where T : new()
    {
        try
        {
            result = Json.Parse<T>(json);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}
```

### 2. 随机数工具

```csharp
public class RandomUtility : IUtility
{
    private Random _random = new Random();
    
    public int Range(int min, int max)
    {
        return _random.Next(min, max + 1);
    }
    
    public float Range(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }
    
    public T Choose<T>(params T[] items)
    {
        return items[Range(0, items.Length - 1)];
    }
    
    public List<T> Shuffle<T>(List<T> list)
    {
        var shuffled = new List<T>(list);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Range(0, i);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        return shuffled;
    }
    
    public bool Probability(float chance)
    {
        return _random.NextDouble() < chance;
    }
}
```

### 3. 字符串工具

```csharp
public class StringUtility : IUtility
{
    public string Truncate(string text, int maxLength, string suffix = "...")
    {
        if (text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength - suffix.Length) + suffix;
    }
    
    public string FormatNumber(int number)
    {
        if (number >= 1000000)
            return $"{number / 1000000.0:F1}M";
        if (number >= 1000)
            return $"{number / 1000.0:F1}K";
        return number.ToString();
    }
    
    public string ToTitleCase(string text)
    {
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
    }
    
    public bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
```

### 4. 加密工具

```csharp
public class EncryptionUtility : IUtility
{
    private const string EncryptionKey = "YourSecretKey123";
    
    public string Encrypt(string plainText)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(plainText);
        byte[] encrypted = EncryptBytes(data);
        return Convert.ToBase64String(encrypted);
    }
    
    public string Decrypt(string encryptedText)
    {
        byte[] data = Convert.FromBase64String(encryptedText);
        byte[] decrypted = DecryptBytes(data);
        return System.Text.Encoding.UTF8.GetString(decrypted);
    }
    
    private byte[] EncryptBytes(byte[] data)
    {
        // 简单的 XOR 加密示例（实际项目应使用更安全的算法）
        byte[] key = System.Text.Encoding.UTF8.GetBytes(EncryptionKey);
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        }
        return result;
    }
    
    private byte[] DecryptBytes(byte[] data)
    {
        return EncryptBytes(data); // XOR 解密与加密相同
    }
}
```

### 5. 对象池工具

```csharp
public class ObjectPoolUtility : IUtility
{
    private Dictionary<Type, Queue<object>> _pools = new();
    
    public T Get<T>() where T : new()
    {
        Type type = typeof(T);
        if (_pools.ContainsKey(type) && _pools[type].Count > 0)
        {
            return (T)_pools[type].Dequeue();
        }
        return new T();
    }
    
    public void Return<T>(T obj)
    {
        Type type = typeof(T);
        if (!_pools.ContainsKey(type))
        {
            _pools[type] = new Queue<object>();
        }
        _pools[type].Enqueue(obj);
    }
    
    public void Clear<T>()
    {
        Type type = typeof(T);
        if (_pools.ContainsKey(type))
        {
            _pools[type].Clear();
        }
    }
    
    public void ClearAll()
    {
        _pools.Clear();
    }
}
```

### 6. 日志工具

```csharp
public class LogUtility : IUtility
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
    
    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        string prefix = level switch
        {
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Info => "[INFO]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Error => "[ERROR]",
            _ => ""
        };
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        GD.Print($"{timestamp} {prefix} {message}");
    }
    
    public void Debug(string message) => Log(message, LogLevel.Debug);
    public void Info(string message) => Log(message, LogLevel.Info);
    public void Warning(string message) => Log(message, LogLevel.Warning);
    public void Error(string message) => Log(message, LogLevel.Error);
}
```

## Utility vs System

### Utility（工具层）

- **无状态** - 不存储业务数据
- **纯函数** - 相同输入产生相同输出
- **独立性** - 不依赖架构状态
- **可复用** - 可在多个项目中使用

### System（逻辑层）

- **有状态** - 可能存储临时状态
- **业务逻辑** - 处理特定业务流程
- **架构依赖** - 需要访问 Model
- **项目特定** - 针对特定项目设计

```csharp
// ✅ Utility: 无状态的工具方法
public class MathUtility : IUtility
{
    public float CalculateDamage(float attackPower, float defense)
    {
        return Math.Max(1, attackPower - defense * 0.5f);
    }
}

// ✅ System: 有状态的业务逻辑
public class CombatSystem : AbstractSystem
{
    private List<CombatInstance> _activeCombats = new();
    
    protected override void OnInit()
    {
        this.RegisterEvent<AttackEvent>(OnAttack);
    }
    
    private void OnAttack(AttackEvent e)
    {
        var mathUtil = this.GetUtility<MathUtility>();
        var playerModel = this.GetModel<PlayerModel>();
        
        // 使用 Utility 计算，但在 System 中处理业务逻辑
        float damage = mathUtil.CalculateDamage(e.AttackPower, playerModel.Defense.Value);
        playerModel.Health.Value -= (int)damage;
        
        _activeCombats.Add(new CombatInstance { Damage = damage });
    }
}
```

## 最佳实践

1. **保持无状态** - Utility 不应存储业务状态
2. **纯函数优先** - 相同输入应产生相同输出
3. **单一职责** - 每个 Utility 专注于一类功能
4. **避免依赖** - 不依赖 Model、System 等架构组件
5. **可测试** - 易于单元测试的纯函数

## 错误示例

```csharp
// ❌ 错误：Utility 中存储状态
public class BadUtility : IUtility
{
    private int _counter = 0;  // 不应该有状态
    
    public int GetNextId()
    {
        return ++_counter;  // 依赖内部状态
    }
}

// ❌ 错误：Utility 中访问 Model
public class BadUtility2 : IUtility
{
    public void DoSomething()
    {
        // Utility 不应该访问架构组件
        var model = this.GetModel<PlayerModel>();  // 编译错误！
    }
}

// ✅ 正确：如果需要状态，应该使用 System
public class IdGeneratorSystem : AbstractSystem
{
    private int _counter = 0;
    
    protected override void OnInit() { }
    
    public int GetNextId()
    {
        return ++_counter;
    }
}
```

## 性能优化

### 1. 缓存计算结果

```csharp
public class PathfindingUtility : IUtility
{
    private Dictionary<(Vector3, Vector3), List<Vector3>> _pathCache = new();
    
    public List<Vector3> FindPath(Vector3 start, Vector3 end, bool useCache = true)
    {
        var key = (start, end);
        
        if (useCache && _pathCache.ContainsKey(key))
        {
            return _pathCache[key];
        }
        
        var path = CalculatePath(start, end);
        
        if (useCache)
        {
            _pathCache[key] = path;
        }
        
        return path;
    }
    
    private List<Vector3> CalculatePath(Vector3 start, Vector3 end)
    {
        // A* 算法等复杂计算...
        return new List<Vector3>();
    }
    
    public void ClearCache()
    {
        _pathCache.Clear();
    }
}
```

### 2. 对象复用

```csharp
public class CollectionUtility : IUtility
{
    private List<Vector3> _tempList = new();
    
    public List<Vector3> GetPointsInRadius(Vector3 center, float radius, List<Vector3> points)
    {
        _tempList.Clear();
        
        foreach (var point in points)
        {
            if (point.DistanceTo(center) <= radius)
            {
                _tempList.Add(point);
            }
        }
        
        return new List<Vector3>(_tempList); // 返回副本
    }
}
```

## 相关包

- [`system`](./system.md) - System 中使用 Utility
- [`command`](./command.md) - Command 中可以使用 Utility
- [`architecture`](./architecture.md) - 在架构中注册 Utility
- [`ioc`](./ioc.md) - Utility 通过 IoC 容器管理
- [`extensions`](./extensions.md) - 提供 GetUtility 扩展方法