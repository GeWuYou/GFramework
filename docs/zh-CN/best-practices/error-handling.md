# 错误处理最佳实践

> 本指南介绍 GFramework 中的错误处理模式和最佳实践，帮助你构建健壮、可维护的游戏应用。

## 📋 目录

- [概述](#概述)
- [核心概念](#核心概念)
- [Result 模式](#result-模式)
- [Option 模式](#option-模式)
- [异常处理](#异常处理)
- [日志记录](#日志记录)
- [用户反馈](#用户反馈)
- [错误恢复](#错误恢复)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

## 概述

### 为什么错误处理很重要

良好的错误处理是构建健壮应用的基础：

- **提高稳定性**：优雅地处理异常情况，避免崩溃
- **改善用户体验**：提供清晰的错误提示和恢复选项
- **简化调试**：记录详细的错误信息，快速定位问题
- **增强可维护性**：使用类型安全的错误处理模式

### GFramework 的错误处理策略

GFramework 提供多种错误处理机制：

1. **Result&lt;T&gt; 模式**：函数式错误处理，类型安全
2. **Option&lt;T&gt; 模式**：处理可空值，避免 null 引用
3. **异常处理**：处理不可恢复的错误
4. **日志系统**：记录错误信息和调试数据

## 核心概念

### 错误类型

根据错误的性质和处理方式，可以分为以下几类：

**1. 可预期的错误**

这类错误是业务逻辑的一部分，应该使用 Result 或 Option 模式处理：

```csharp
// 示例：用户输入验证
public Result&lt;PlayerData&gt; ValidatePlayerName(string name)
{
    if (string.IsNullOrWhiteSpace(name))
        return Result&lt;PlayerData&gt;.Failure("玩家名称不能为空");

    if (name.Length &lt; 3)
        return Result&lt;PlayerData&gt;.Failure("玩家名称至少需要 3 个字符");

    if (name.Length &gt; 20)
        return Result&lt;PlayerData&gt;.Failure("玩家名称不能超过 20 个字符");

    return Result&lt;PlayerData&gt;.Success(new PlayerData { Name = name });
}
```

**2. 不可预期的错误**

这类错误通常是程序错误或系统故障，应该使用异常处理：

```csharp
// 示例：文件系统错误
public async Task&lt;SaveData&gt; LoadSaveFileAsync(string path)
{
    try
    {
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize&lt;SaveData&gt;(json);
    }
    catch (FileNotFoundException ex)
    {
        _logger.Error($"存档文件不存在: {path}", ex);
        throw new SaveSystemException("无法加载存档文件", ex);
    }
    catch (JsonException ex)
    {
        _logger.Error($"存档文件格式错误: {path}", ex);
        throw new SaveSystemException("存档文件已损坏", ex);
    }
}
```

**3. 可空值**

使用 Option 模式处理可能不存在的值：

```csharp
// 示例：查找玩家
public Option&lt;Player&gt; FindPlayerById(string playerId)
{
    var player = _players.FirstOrDefault(p =&gt; p.Id == playerId);
    return player != null ? Option&lt;Player&gt;.Some(player) : Option&lt;Player&gt;.None;
}
```

### 错误传播

错误应该在合适的层级处理，不要过早捕获：

```csharp
// ✅ 好的做法：让错误向上传播
public class InventorySystem : AbstractSystem
{
    public Result&lt;Item&gt; AddItem(string itemId, int quantity)
    {
        // 验证参数
        var validationResult = ValidateAddItem(itemId, quantity);
        if (validationResult.IsFaulted)
            return validationResult;

        // 检查空间
        var spaceResult = CheckInventorySpace(quantity);
        if (spaceResult.IsFaulted)
            return Result&lt;Item&gt;.Failure(spaceResult.Exception);

        // 添加物品
        var item = CreateItem(itemId, quantity);
        _items.Add(item);

        return Result&lt;Item&gt;.Success(item);
    }
}

// ❌ 避免：过早捕获错误
public class InventorySystem : AbstractSystem
{
    public Result&lt;Item&gt; AddItem(string itemId, int quantity)
    {
        try
        {
            // 所有操作都在 try 块中
            ValidateAddItem(itemId, quantity);
            CheckInventorySpace(quantity);
            var item = CreateItem(itemId, quantity);
            _items.Add(item);
            return Result&lt;Item&gt;.Success(item);
        }
        catch (Exception ex)
        {
            // 捕获所有异常，丢失了错误的具体信息
            return Result&lt;Item&gt;.Failure("添加物品失败");
        }
    }
}
```

### 错误恢复

设计错误恢复策略，提供降级方案：

```csharp
// 示例：多级降级策略
public async Task&lt;GameConfig&gt; LoadConfigAsync()
{
    // 1. 尝试从云端加载
    var cloudResult = await TryLoadFromCloudAsync();
    if (cloudResult.IsSuccess)
    {
        _logger.Info("从云端加载配置成功");
        return cloudResult.Match(succ: c =&gt; c, fail: _ =&gt; null);
    }

    // 2. 尝试从本地缓存加载
    var cacheResult = await TryLoadFromCacheAsync();
    if (cacheResult.IsSuccess)
    {
        _logger.Warn("云端加载失败，使用本地缓存");
        return cacheResult.Match(succ: c =&gt; c, fail: _ =&gt; null);
    }

    // 3. 使用默认配置
    _logger.Error("所有配置源加载失败，使用默认配置");
    return GetDefaultConfig();
}
```

## Result 模式

### 基本用法

Result&lt;T&gt; 是一个函数式类型，表示操作可能成功（包含值）或失败（包含异常）：

```csharp
// 创建成功结果
var successResult = Result&lt;int&gt;.Success(42);

// 创建失败结果
var failureResult = Result&lt;int&gt;.Failure(new Exception("操作失败"));
var failureResult2 = Result&lt;int&gt;.Failure("操作失败"); // 使用字符串

// 检查状态
if (result.IsSuccess)
{
    var value = result.Match(succ: v =&gt; v, fail: _ =&gt; 0);
}

if (result.IsFaulted)
{
    var error = result.Exception;
}
```

### 链式操作

Result 支持函数式编程的链式操作：

```csharp
// Map：转换成功值
var result = Result&lt;int&gt;.Success(42)
    .Map(x =&gt; x * 2)           // Result&lt;int&gt;(84)
    .Map(x =&gt; x.ToString());   // Result&lt;string&gt;("84")

// Bind：链式转换，可能失败
var result = Result&lt;string&gt;.Success("42")
    .Bind(s =&gt; int.TryParse(s, out var i)
        ? Result&lt;int&gt;.Success(i)
        : Result&lt;int&gt;.Failure("解析失败"))
    .Map(i =&gt; i * 2);

// Ensure：验证条件
var result = Result&lt;int&gt;.Success(42)
    .Ensure(x =&gt; x &gt; 0, "值必须为正数")
    .Ensure(x =&gt; x &lt; 100, "值必须小于 100");

// OnSuccess/OnFailure：执行副作用
result
    .OnSuccess(value =&gt; _logger.Info($"成功: {value}"))
    .OnFailure(ex =&gt; _logger.Error($"失败: {ex.Message}"));
```

### 实际应用示例

**示例 1：用户注册**

```csharp
public class UserRegistrationSystem : AbstractSystem
{
    public Result&lt;User&gt; RegisterUser(string username, string email, string password)
    {
        return ValidateUsername(username)
            .Bind(_ =&gt; ValidateEmail(email))
            .Bind(_ =&gt; ValidatePassword(password))
            .Bind(_ =&gt; CheckUsernameAvailability(username))
            .Bind(_ =&gt; CheckEmailAvailability(email))
            .Map(_ =&gt; CreateUser(username, email, password))
            .OnSuccess(user =&gt; {
                _logger.Info($"用户注册成功: {username}");
                this.SendEvent(new UserRegisteredEvent { User = user });
            })
            .OnFailure(ex =&gt; _logger.Warn($"用户注册失败: {ex.Message}"));
    }

    private Result&lt;string&gt; ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result&lt;string&gt;.Failure("用户名不能为空");

        if (username.Length &lt; 3 || username.Length &gt; 20)
            return Result&lt;string&gt;.Failure("用户名长度必须在 3-20 个字符之间");

        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
            return Result&lt;string&gt;.Failure("用户名只能包含字母、数字和下划线");

        return Result&lt;string&gt;.Success(username);
    }

    private Result&lt;string&gt; ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result&lt;string&gt;.Failure("邮箱不能为空");

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return Result&lt;string&gt;.Failure("邮箱格式不正确");

        return Result&lt;string&gt;.Success(email);
    }

    private Result&lt;string&gt; ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Result&lt;string&gt;.Failure("密码不能为空");

        if (password.Length &lt; 8)
            return Result&lt;string&gt;.Failure("密码长度至少为 8 个字符");

        if (!passwordchar.IsUpper))
            return Result&lt;string&gt;.Failure("密码必须包含至少一个大写字母");

        if (!password.Any(char.IsDigit))
            return Result&lt;string&gt;.Failure("密码必须包含至少一个数字");

        return Result&lt;string&gt;.Success(password);
    }

    private Result&lt;bool&gt; CheckUsernameAvailability(string username)
    {
        var exists = _userRepository.ExistsByUsername(username);
        return exists
            ? Result&lt;bool&gt;.Failure("用户名已被使用")
            : Result&lt;bool&gt;.Success(true);
    }

    private Result&lt;bool&gt; CheckEmailAvailability(string email)
    {
    var exists = _userRepository.ExistsByEmail(email);
        return exists
            ? Result&lt;bool&gt;.Failure("邮箱已被注册")
            : Result&lt;bool&gt;.Success(true);
    }

    private User CreateUser(string username, string email, string password)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = username,
            Email = email,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.Add(user);
        return user;
    }
}
```

**示例 2：物品交易**

```csharp
public class TradingSystem : AbstractSystem
{
    public Result&lt;Trade&gt; ExecuteTrade(string sellerId, string buyerId, string itemId, int price)
    {
        return ValidateTradeParticipants(sellerId, buyerId)
            .Bind(_ =&gt; ValidateItem(sellerId, itemId))
            .Bind(_ =&gt; ValidateBuyerFunds(buyerId, price))
            .Bind(_ =&gt; TransferItem(sellerId, buyerId, itemId))
            .Bind(_ =&gerCurrency(buyerId, sellerId, price))
            .Map(_ =&gt; CreateTradeRecord(sellerId, buyerId, itemId, price))
            .OnSuccess(trade =&gt; {
                _logger.Info($"交易完成: {trade.Id}");
                this.SendEvent(new TradeCompletedEvent { Trade = trade });
            })
            .OnFailure(ex =&gt; {
                _logger.Error($"交易失败: {ex.Message}");
                RollbackTrade(sellerId, buyerId, itemId, price);
            });
    }

    private Result&lt;bool&gt; ValidateTradeParticipants(string sellerId, string buyerId)
    {
        if (sellerId == buyerId)
            return Result&lt;bool&gt;.Failure("不能与自己交易");

        var seller = _playerRepository.GetById(sellerId);
        if (seller == null)
            return Result&lt;bool&gt;.Failure("卖家不存在");

        var buyer = _playerRepository.GetById(buyerId);
        if (buyer == null)
            return Result&lt;bool&gt;.Failure("买家不存在");

        return Result&lt;bool&gt;.Success(true);
    }

    private Result&lt;bool&gt; ValidateItem(string sellerId, string itemId)
    {
        var inventory = _inventoryRepository.GetByPlayerId(sellerId);
        var item = inventory.Items.FirstOrDefault(i =&gt; i.Id == itemId);

        if (item == null)
            return Result&lt;bool&gt;.Failure("物品不存在");

        if (!item.IsTradeable)
            return Result&lt;bool&gt;.Failure("该物品不可交易");

        return Result&lt;bool&gt;.Success(true);
    }

    private Result&lt;bool&gt; ValidateBuyerFunds(string buyerId, int price)
    {
        var buyer = _playerRepository.GetById(buyerId);
        if (buyer.Currency &lt; price)
            return Result&lt;bool&gt;.Failure($"金币不足，需要 {price}，当前 {buyer.Currency}");

        return Result&lt;bool&gt;.Success(true);
    }
}
```

### 异步操作

Result 支持异步操作：

```csharp
// 异步 Map
var result = await Result&lt;int&gt;.Success(42)
    .MapAsync(async x =&gt; await GetUserDataAsync(x));

// 异步 Bind
var result = await Result&lt;string&gt;.Success("user123")
    .BindAsync(async userId =&gt; await LoadUserAsync(userId));

// TryAsync：安全执行异步操作
var result = await ResultExtensions.TryAsync(async () =&gt;
{
    var data = await LoadDataAsync();
    return ProcessData(data);
});
```

### 组合多个 Result

```csharp
// 组合多个结果，全部成功才返回成功
var results = new[]
{
    ValidateUsername("player1"),
    ValidateEmail("player1@example.com"),
    ValidatePassword("Password123")
};

var combinedResult = results.Combine(); // Result&lt;List&lt;string&gt;&gt;

if (combinedResult.IsSuccess)
{
    _logger.Info("所有验证通过");
}
else
{
    _logger.Error($"验证失败: {combinedResult.Exception.Message}");
}
```

## Option 模式

### 基本用法

Option&lt;T&gt; 用于表示可能存在或不存在的值，避免 null 引用异常：

```csharp
// 创建 Some（有值）
var someOption = Option&lt;int&gt;.Some(42);

// 创建 None（无值）
var noneOption = Option&lt;int&gt;.None;

// 检查状态
if (option.IsSome)
{
    var value = option.GetOrElse(0);
}

if (option.IsNone)
{
    _logger.Warn("值不存在");
}
```

### 常用操作

```csharp
// GetOrElse：获取值或返回默认值
var value = option.GetOrElse(0);
var value2 = option.GetOrElse(() => GetDefaultValue());

// Map：转换值
var mapped = option.Map(x => x * 2);

// Bind：链式转换
var bound = option.Bind(x => x > 0
    ? Option&lt;string&gt;.Some(x.ToString())
    : Option&lt;string&gt;.None);

// Filter：过滤值
var filtered = option.Filter(x => x > 0);

// Match：模式匹配
var result = option.Match(
    some: value => $"值: {value}",
    none: () => "无值"
);
```

### 实际应用示例

**示例 1：查找玩家**

```csharp
public class PlayerSystem : AbstractSystem
{
    public Option&lt;Player&gt; FindPlayerById(string playerId)
    {
        var player = _players.FirstOrDefault(p => p.Id == playerId);
        return player != null ? Option&lt;Player&gt;.Some(player) : Option&lt;Player&gt;.None;
    }

    public Option&lt;Player&gt; FindPlayerByName(string playerName)
    {
        var player = _players.FirstOrDefault(p => p.Name == playerName);
        return player != null ? Option&lt;Player&gt;.Some(player) : Option&lt;Player&gt;.None;
    }

    public void UpdatePlayerScore(string playerId, int score)
    {
        FindPlayerById(playerId).Match(
            some: player => {
                player.Score += score;
                _logger.Info($"玩家 {player.Name} 得分更新: {player.Score}");
                this.SendEvent(new PlayerScoreUpdatedEvent { Player = player });
            },
            none: () => _logger.Warn($"玩家不存在: {playerId}")
        );
    }
}
```

**示例 2：配置管理**

```csharp
public class ConfigurationSystem : AbstractSystem
{
    private readonly Dictionary&lt;string, string&gt; _config = new();

    public Option&lt;string&gt; GetConfig(string key)
    {
        return _config.TryGetValue(key, out var value)
            ? Option&lt;string&gt;.Some(value)
            : Option&lt;string&gt;.None;
    }

    public Option&lt;int&gt; GetIntConfig(string key)
    {
        return GetConfig(key)
            .Bind(value => int.TryParse(value, out var result)
                ? Option&lt;int&gt;.Some(result)
                : Option&lt;int&gt;.None);
    }

    public Option&lt;bool&gt; GetBoolConfig(string key)
    {
        return GetConfig(key)
            .Bind(value => bool.TryParse(value, out var result)
                ? Option&lt;bool&gt;.Some(result)
                : Option&lt;bool&gt;.None);
    }

    public void ApplyGraphicsSettings()
    {
        var width = GetIntConfig("screen_width").GetOrElse(1920);
        var height = GetIntConfig("screen_height").GetOrElse(1080);
        var fullscreen = GetBoolConfig("fullscreen").GetOrElse(false);
        var vsync = GetBoolConfig("vsync").GetOrElse(true);

        ApplyScreenSettings(width, height, fullscreen, vsync);
    }
}
```

**示例 3：物品查找**

```csharp
public class InventorySystem : AbstractSystem
{
    public Option&lt;Item&gt; FindItemById(string itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        return item != null ? Option&lt;Item&gt;.Some(item) : Option&lt;Item&gt;.None;
    }

    public Option&lt;Item&gt; FindItemByType(ItemType type)
    {
        var item = _items.FirstOrDefault(i => i.Type == type);
        return item != null ? Option&lt;Item&gt;.Some(item) : Option&lt;Item&gt;.None;
    }

    public Result&lt;Item&gt; UseItem(string itemId)
    {
        return FindItemById(itemId)
            .ToResult("物品不存在")
            .Bind(item => item.IsUsable
                ? Result&lt;Item&gt;.Success(item)
                : Result&lt;Item&gt;.Failure("物品不可使用"))
            .OnSuccess(item => {
                item.Use();
                _logger.Info($"使用物品: {item.Name}");
                this.SendEvent(new ItemUsedEvent { Item = item });
            });
    }
}
```

### Option 与 Result 的转换

```csharp
// Option 转 Result
var option = Option&lt;int&gt;.Some(42);
var result = option.ToResult("值不存在"); // Result&lt;int&gt;

// Result 转 Option（需要自定义扩展）
var result = Result&lt;int&gt;.Success(42);
var option = result.IsSuccess
    ? Option&lt;int&gt;.Some(result.Match(succ: v => v, fail: _ => 0))
    : Option&lt;int&gt;.None;
```

## 异常处理

### 何时使用异常

异常应该用于处理**不可预期的错误**和**不可恢复的错误**：

```csharp
// ✅ 适合使用异常的场景
public class SaveSystem : AbstractSystem
{
    public async Task SaveGameAsync(SaveData data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(_savePath, json);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Fatal("没有写入权限", ex);
            throw new SaveSystemException("无法保存游戏：权限不足", ex);
        }
        catch (IOException ex)
        {
            _logger.Error("IO 错误", ex);
            throw new SaveSystemException("无法保存游戏：磁盘错误", ex);
        }
    }
}

// ❌ 不适合使用异常的场景
public class PlayerSystem : AbstractSystem
{
    // 不要用异常处理业务逻辑
    public void UpdatePlayerHealth(string playerId, int damage)
    {
        var player = FindPlayerById(playerId);
        if (player == null)
            throw new PlayerNotFoundException(playerId); // ❌ 应该用 Option 或 Result

        player.Health -= damage;
    }
}
```

### 自定义异常

创建有意义的异常类型：

```csharp
// 基础游戏异常
public class GameException : Exception
{
    public string ErrorCode { get; }
    public Dictionary&lt;string, object&gt; Context { get; }

    public GameException(
        string message,
        string errorCode,
        Dictionary&lt;string, object&gt; context = null,
        Exception innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Context = context ?? new Dictionary&lt;string, object&gt;();
    }
}

// 存档系统异常
public class SaveSystemException : GameException
{
    public SaveSystemException(
        string message,
        Exception innerException = null)
        : base(message, "SAVE_ERROR", null, innerException)
    {
    }
}

// 网络异常
public class NetworkException : GameException
{
    public int StatusCode { get; }

    public NetworkException(
        string message,
        int statusCode,
        Exception innerException = null)
        : base(
            message,
            "NETWORK_ERROR",
            new Dictionary&lt;string, object&gt; { ["statusCode"] = statusCode },
            innerException)
    {
        StatusCode = statusCode;
    }
}

// 使用示例
public class NetworkSystem : AbstractSystem
{
    public async Task&lt;PlayerData&gt; FetchPlayerDataAsync(string playerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/players/{playerId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new NetworkException(
                    $"获取玩家数据失败: {playerId}",
                    (int)response.StatusCode
                );
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize&lt;PlayerData&gt;(json);
        }
        catch (HttpRequestException ex)
        {
            _logger.Error($"网络请求失败: {playerId}", ex);
            throw new NetworkException("网络连接失败", 0, ex);
        }
    }
}
```

### 异常处理最佳实践

**1. 捕获具体的异常类型**

```csharp
// ✅ 好的做法
try
{
    var data = await LoadDataAsync();
}
catch (FileNotFoundException ex)
{
    _logger.Warn("文件不存在", ex);
    return GetDefaultData();
}
catch (JsonException ex)
{
    _logger.Error("JSON 解析失败", ex);
    throw new DataException("数据格式错误", ex);
}
catch (IOException ex)
{
    _logger.Error("IO 错误", ex);
    throw new DataException("读取数据失败", ex);
}

// ❌ 避免：捕获所有异常
try
{
    var data = await LoadDataAsync();
}
catch (Exception ex) // 太宽泛
{
    _logger.Error("加载失败", ex);
    return null;
}
```

**2. 不要吞掉异常**

```csharp
// ❌ 避免：吞掉异常
try
{
    RiskyOperation();
}
catch (Exception ex)
{
    // 什么都不做，异常被吞掉了
}

// ✅ 好的做法：记录并重新抛出或处理
try
{
    RiskyOperation();
}
catch (Exception ex)
{
    _logger.Error("操作失败", ex);
    throw; // 重新抛出原始异常
}
```

**3. 使用 finally 清理资源**

```csharp
// ✅ 好的做法
FileStream stream = null;
try
{
    stream = File.OpenRead(path);
    // 处理文件
}
catch (IOException ex)
{
    _logger.Error("文件读取失败", ex);
    throw;
}
finally
{
    stream?.Dispose(); // 确保资源被释放
}

// 更好的做法：使用 using 语句
using (var stream = File.OpenRead(path))
{
    // 处理文件
} // 自动释放资源
```

## 日志记录

### 日志级别

GFramework 提供多个日志级别，根据错误严重程度选择合适的级别：

```csharp
// Trace：详细的调试信息
_logger.Trace("进入 ProcessInput 方法");

// Debug：调试信息
_logger.Debug($"玩家位置: {player.Position}");

// Info：一般信息
_logger.Info("游戏启动成功");

// Warning：警告信息
_logger.Warn("配置文件不存在，使用默认配置");

// Error：错误信息
_logger.Error("保存游戏失败", exception);

// Fatal：致命错误
_logger.Fatal("无法初始化渲染系统", exception);
```

### 结构化日志

使用结构化日志记录上下文信息：

```csharp
// 记录带上下文的日志
_logger.Log(
    LogLevel.Error,
    "玩家操作失败",
    ("playerId", playerId),
    ("action", "attack"),
    ("targetId", targetId),
    ("timestamp", DateTime.UtcNow)
);

// 记录带异常的结构化日志
_logger.Log(
    LogLevel.Error,
    "数据库操作失败",
    exception,
    ("operation", "insert"),
    ("table", "players"),
    ("retryCount", retryCount)
);
```

### 日志最佳实践

**1. 记录足够的上下文**

```csharp
// ❌ 避免：信息不足
_logger.Error("操作失败");

// ✅ 好的做法：包含上下文
_logger.Error(
    $"保存玩家数据失败: PlayerId={playerId}, Reason={ex.Message}",
    ex
);
```

**2. 不要记录敏感信息**

```csharp
// ❌ 避免：记录密码等敏感信息
_logger.Info($"用户登录: {username}, 密码: {password}");

// ✅ 好的做法：不记录敏感信息
_logger.Info($"用户登录: {username}");
```

**3. 使用合适的日志级别**

```csharp
public class SaveSystem : AbstractSystem
{
    public async Task&lt;Result&lt;SaveData&gt;&gt; LoadSaveAsync(string saveId)
    {
        _logger.Debug($"开始加载存档: {saveId}");

        try
        {
            var data = await _storage.LoadAsync&lt;SaveData&gt;(saveId);

            if (data == null)
            {
                _logger.Warn($"存档不存在: {saveId}");
                return Result&lt;SaveData&gt;.Failure("存档不存在");
            }

            _logger.Info($"存档加载成功: {saveId}");
            return Result&lt;SaveData&gt;.Success(data);
        }
        catch (Exception ex)
        {
            _logger.Error($"加载存档失败: {saveId}", ex);
            return Result&lt;SaveData&gt;.Failure(ex);
        }
    }
}
```

## 用户反馈

### 友好的错误提示

将技术错误转换为用户友好的消息：

```csharp
public class ErrorMessageService
{
    private readonly Dictionary&lt;string, string&gt; _errorMessages = new()
    {
        ["NETWORK_ERROR"] = "网络连接失败，请检查网络设置",
        ["SAVE_ERROR"] = "保存失败，请确保有足够的磁盘空间",
        ["INVALID_INPUT"] = "输入无效，请检查后重试",
        ["PERMISSION_DENIED"] = "权限不足，无法执行此操作",
        ["RESOURCE_NOT_FOUND"] = "资源不存在",
        ["TIMEOUT"] = "操作超时，请稍后重试"
    };

    public string GetUserFriendlyMessage(string errorCode)
    {
        return _errorMessages.TryGetValue(errorCode, out var message)
            ? message
            : "发生未知错误，请联系技术支持";
    }

    public string GetUserFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            GameException gameEx => GetUserFriendlyMessage(gameEx.ErrorCode),
            FileNotFoundException => "文件不存在",
            UnauthorizedAccessException => "权限不足",
            TimeoutException => "操作超时",
            _ => "发生未知错误"
        };
    }
}
```

### 错误提示 UI

```csharp
public class ErrorNotificationSystem : AbstractSystem
{
    private readonly ErrorMessageService _messageService;

    protected override void OnInit()
    {
        this.RegisterEvent&lt;ErrorOccurredEvent&gt;(OnErrorOccurred);
    }

    private void OnErrorOccurred(ErrorOccurredEvent e)
    {
        var userMessage = _messageService.GetUserFriendlyMessage(e.Exception);

        // 根据错误严重程度显示不同的 UI
        if (e.Exception is GameException gameEx)
        {
            switch (gameEx.ErrorCode)
            {
                case "NETWORK_ERROR":
                    ShowNetworkErrorDialog(userMessage);
                    break;

                case "SAVE_ERROR":
                    ShowSaveErrorDialog(userMessage);
                    break;

                default:
                    ShowGenericErrorToast(userMessage);
                    break;
            }
        }
        else
        {
            ShowGenericErrorDialog(userMessage);
        }
    }

    private void ShowNetworkErrorDialog(string message)
    {
        this.SendEvent(new ShowDialogEvent
        {
            Title = "网络错误",
            Message = message,
            Buttons = new[] { "重试", "取消" },
            OnButtonClicked = buttonIndex =>
            {
                if (buttonIndex == 0) // 重试
                {
                    this.SendEvent(new RetryNetworkOperationEvent());
                }
            }
        });
    }

    private void ShowSaveErrorDialog(string message)
    {
        this.SendEvent(new ShowDialogEvent
        {
            Title = "保存失败",
            Message = message,
            Buttons = new[] { "确定" }
        });
    }

    private void ShowGenericErrorToast(string message)
    {
        this.SendEvent(new ShowToastEvent
        {
            Message = message,
            Duration = 3000
        });
    }

    private void ShowGenericErrorDialog(string message)
    {
        this.SendEvent(new ShowDialogEvent
        {
            Title = "错误",
            Message = message,
            Buttons = new[] { "确定" }
        });
    }
}
```

## 错误恢复

### 重试机制

实现自动重试策略：

```csharp
public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
    public double BackoffMultiplier { get; set; } = 2.0;

    public async Task&lt;Result&lt;T&gt;&gt; ExecuteAsync&lt;T&gt;(
        Func&lt;Task&lt;T&gt;&gt; operation,
        Func&lt;Exception, bool&gt; shouldRetry = null)
    {
        var delay = InitialDelay;
        Exception lastException = null;

        for (int attempt = 0; attempt &lt;= MaxRetries; attempt++)
        {
            try
            {
                var result = await operation();
                return Result&lt;T&gt;.Success(result);
            }
            catch (Exception ex)
            {
                lastException = ex;

                // 检查是否应该重试
                if (shouldRetry != null && !shouldRetry(ex))
                {
                    break;
                }

                // 最后一次尝试失败
                if (attempt == MaxRetries)
                {
                    break;
                }

                // 等待后重试
                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * BackoffMultiplier);
            }
        }

        return Result&lt;T&gt;.Failure(lastException);
    }
}

// 使用示例
public class NetworkSystem : AbstractSystem
{
    private readonly RetryPolicy _retryPolicy = new()
    {
        MaxRetries = 3,
        InitialDelay = TimeSpan.FromSeconds(1),
        BackoffMultiplier = 2.0
    };

    public async Task&lt;Result&lt;PlayerData&gt;&gt; FetchPlayerDataAsync(string playerId)
    {
        return await _retryPolicy.ExecuteAsync(
            operation: async () =>
            {
                var response = await _httpClient.GetAsync($"/api/players/{playerId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize&lt;PlayerData&gt;(json);
            },
            shouldRetry: ex => ex is HttpRequestException || ex is TimeoutException
        );
    }
}
```

### 降级策略

提供多级降级方案：

```csharp
public class ConfigurationSystem : AbstractSystem
{
    public async Task&lt;GameConfig&gt; LoadConfigAsync()
    {
        // 1. 尝试从远程服务器加载
        var remoteResult = await TryLoadFromRemoteAsync();
        if (remoteResult.IsSuccess)
        {
            _logger.Info("从远程服务器加载配置成功");
            await CacheConfigAsync(remoteResult.Match(succ: c => c, fail: _ => null));
            return remoteResult.Match(succ: c => c, fail: _ => null);
        }

        _logger.Warn($"远程加载失败: {remoteResult.Exception.Message}");

        // 2. 尝试从本地缓存加载
        var cacheResult = await TryLoadFromCacheAsync();
        if (cacheResult.IsSuccess)
        {
            _logger.Info("从本地缓存加载配置成功");
            return cacheResult.Match(succ: c => c, fail: _ => null);
        }

        _logger.Warn($"缓存加载失败: {cacheResult.Exception.Message}");

        // 3. 使用内置默认配置
        _logger.Error("所有配置源加载失败，使用默认配置");
        return GetDefaultConfig();
    }

    private async Task&lt;Result&lt;GameConfig&gt;&gt; TryLoadFromRemoteAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/config");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var config = JsonSerializer.Deserialize&lt;GameConfig&gt;(json);
            return Result&lt;GameConfig&gt;.Success(config);
        }
        catch (Exception ex)
        {
            return Result&lt;GameConfig&gt;.Failure(ex);
        }
    }

    private async Task&lt;Result&lt;GameConfig&gt;&gt; TryLoadFromCacheAsync()
    {
        try
        {
            var json = await File.ReadAllTextAsync(_cachePath);
            var config = JsonSerializer.Deserialize&lt;GameConfig&gt;(json);
            return Result&lt;GameConfig&gt;.Success(config);
        }
        catch (Exception ex)
        {
            return Result&lt;GameConfig&gt;.Failure(ex);
        }
    }

    private async Task CacheConfigAsync(GameConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config);
            await File.WriteAllTextAsync(_cachePath, json);
        }
        catch (Exception ex)
        {
            _logger.Warn("缓存配置失败", ex);
        }
    }

    private GameConfig GetDefaultConfig()
    {
        return new GameConfig
        {
            ServerUrl = "https://default.example.com",
            Timeout = 30,
            MaxRetries = 3
        };
    }
}
```

### 断路器模式

防止级联失败：

```csharp
public class CircuitBreaker
{
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private CircuitState _state = CircuitState.Closed;

    public int FailureThreshold { get; set; } = 5;
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromMinutes(1);

    public async Task&lt;Result&lt;T&gt;&gt; ExecuteAsync&lt;T&gt;(Func&lt;Task&lt;T&gt;&gt; operation)
    {
        // 检查断路器状态
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime &gt; OpenDuration)
            {
                _state = CircuitState.HalfOpen;
            }
            else
            {
                return Result&lt;T&gt;.Failure("断路器已打开，操作被拒绝");
            }
        }

        try
        {
            var result = await operation();

            // 成功，重置计数器
            if (_state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Closed;
            }
            _failureCount = 0;

            return Result&lt;T&gt;.Success(result);
        }
        catch (Exception ex)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            // 达到阈值，打开断路器
            if (_failureCount &gt;= FailureThreshold)
            {
                _state = CircuitState.Open;
            }

            return Result&lt;T&gt;.Failure(ex);
        }
    }

    private enum CircuitState
    {
        Closed,   // 正常状态
        Open,     // 断路器打开，拒绝请求
        HalfOpen  // 半开状态，尝试恢复
    }
}
```

## 最佳实践

### 1. 选择合适的错误处理方式

根据错误类型选择处理方式：

| 错误类型      | 处理方式            | 示例          |
|-----------|-----------------|-------------|
| 可预期的业务错误  | Result&lt;T&gt; | 用户输入验证、权限检查 |
| 可能不存在的值   | Option&lt;T&gt; | 查找操作、配置读取   |
| 不可预期的系统错误 | Exception       | 文件 IO、网络错误  |
| 不可恢复的错误   | Exception + 日志  | 初始化失败、资源耗尽  |

### 2. 错误处理的层次结构

```csharp
// 底层：使用 Result 处理业务错误
public class InventoryRepository
{
    public Result&lt;Item&gt; GetItem(string itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        return item != null
            ? Result&lt;Item&gt;.Success(item)
            : Result&lt;Item&gt;.Failure("物品不存在");
    }
}

// 中层：组合多个操作
public class InventorySystem : AbstractSystem
{
    public Result&lt;Item&gt; UseItem(string itemId)
    {
        return _repository.GetItem(itemId)
            .Bind(item => ValidateItemUsage(item))
            .Bind(item => ConsumeItem(item))
            .OnSuccess(item => this.SendEvent(new ItemUsedEvent { Item = item }))
            .OnFailure(ex => _logger.Warn($"使用物品失败: {ex.Message}"));
    }
}

// 上层：处理用户交互
public class InventoryController : IController
{
    public void OnUseItemButtonClicked(string itemId)
    {
        var result = _inventorySystem.UseItem(itemId);

        result.Match(
            succ: item => ShowSuccessMessage($"使用了 {item.Name}"),
            fail: ex => ShowErrorMessage(ex.Message)
        );
    }
}
```

### 3. 避免过度使用异常

```csharp
// ❌ 避免：用异常处理正常流程
public Player GetPlayerById(string playerId)
{
    var player = _players.FirstOrDefault(p => p.Id == playerId);
    if (player == null)
        throw new PlayerNotFoundException(playerId); // 不应该用异常

    return player;
}

// ✅ 好的做法：使用 Option
public Option&lt;Player&gt; GetPlayerById(string playerId)
{
    var player = _players.FirstOrDefault(p => p.Id == playerId);
    return player != null
        ? Option&lt;Player&gt;.Some(player)
        : Option&lt;Player&gt;.None;
}
```

### 4. 提供有意义的错误消息

```csharp
// ❌ 避免：模糊的错误消息
return Result&lt;Item&gt;.Failure("错误");
return Result&lt;Item&gt;.Failure("操作失败");

// ✅ 好的做法：具体的错误消息
return Result&lt;Item&gt;.Failure("物品不存在");
return Result&lt;Item&gt;.Failure($"背包空间不足，需要 {required} 格，当前 {available} 格");
return Result&lt;Item&gt;.Failure($"物品 {itemName} 不可交易");
```

### 5. 记录错误上下文

```csharp
// ❌ 避免：缺少上下文
_logger.Error("保存失败", ex);

// ✅ 好的做法：包含完整上下文
_logger.Log(
    LogLevel.Error,
    "保存玩家数据失败",
    ex,
    ("playerId", playerId),
    ("saveSlot", saveSlot),
    ("dataSize", data.Length),
    ("timestamp", DateTime.UtcNow)
);
```

### 6. 优雅降级

```csharp
// ✅ 好的做法：提供降级方案
public async Task&lt;Texture&gt; LoadTextureAsync(string path)
{
    // 1. 尝试加载指定纹理
    var result = await TryLoadTextureAsync(path);
    if (result.IsSuccess)
        return result.Match(succ: t => t, fail: _ => null);

    // 2. 尝试加载备用纹理
    _logger.Warn($"加载纹理失败: {path}，使用备用纹理");
    var fallbackResult = await TryLoadTextureAsync(_fallbackTexturePath);
    if (fallbackResult.IsSuccess)
        return fallbackResult.Match(succ: t => t, fail: _ => null);

    // 3. 使用默认纹理
    _logger.Error("所有纹理加载失败，使用默认纹理");
    return _defaultTexture;
}
```

### 7. 测试错误处理

```csharp
[TestFixture]
public class InventorySystemTests
{
    [Test]
    public void UseItem_ItemNotFound_ShouldReturnFailure()
    {
        // Arrange
        var system = new InventorySystem();
        var invalidItemId = "invalid_item";

        // Act
        var result = system.UseItem(invalidItemId);

        // Assert
        Assert.That(result.IsFaulted, Is.True);
        Assert.That(result.Exception.Message, Contains.Substring("物品不存在"));
    }

    [Test]
    public void UseItem_ItemNotUsable_ShouldReturnFailure()
    {
        // Arrange
        var system = new InventorySystem();
        var item = new Item { Id = "item1", IsUsable = false };
        system.AddItem(item);

        // Act
        var result = system.UseItem("item1");

        // Assert
        Assert.That(result.IsFaulted, Is.True);
        Assert.That(result.Exception.Message, Contains.Substring("不可使用"));
    }

    [Test]
    public void UseItem_ValidItem_ShouldReturnSuccess()
    {
        // Arrange
        var system = new InventorySystem();
        var item = new Item { Id = "item1", IsUsable = true };
        system.AddItem(item);

        // Act
        var result = system.UseItem("item1");

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }
}
```

## 常见问题

### Q1: 何时使用 Result，何时使用 Option？

**A:**

- 使用 **Result&lt;T&gt;** 当操作可能失败，需要返回错误信息时
- 使用 **Option&lt;T&gt;** 当值可能不存在，但不需要错误信息时

```csharp
// 使用 Result：需要知道为什么失败
public Result&lt;User&gt; RegisterUser(string username, string password)
{
    if (string.IsNullOrEmpty(username))
        return Result&lt;User&gt;.Failure("用户名不能为空");

    if (password.Length &lt; 8)
        return Result&lt;User&gt;.Failure("密码长度至少为 8 个字符");

    // ...
}

// 使用 Option：只需要知道是否存在
public Option&lt;User&gt; FindUserById(string userId)
{
    var user = _users.FirstOrDefault(u => u.Id == userId);
    return user != null ? Option&lt;User&gt;.Some(user) : Option&lt;User&gt;.None;
}
```

### Q2: 如何处理异步操作中的错误？

**A:** 使用 Result 的异步扩展方法：

```csharp
public async Task&lt;Result&lt;PlayerData&gt;&gt; LoadPlayerDataAsync(string playerId)
{
    return await ResultExtensions.TryAsync(async () =>
    {
        var data = await _httpClient.GetStringAsync($"/api/players/{playerId}");
        return JsonSerializer.Deserialize&lt;PlayerData&gt;(data);
    });
}

// 链式异步操作
public async Task&lt;Result&lt;Player&gt;&gt; LoadAndValidatePlayerAsync(string playerId)
{
    var result = await LoadPlayerDataAsync(playerId);
    return await result.BindAsync(async data =>
    {
        var isValid = await ValidatePlayerDataAsync(data);
        return isValid
            ? Result&lt;Player&gt;.Success(CreatePlayer(data))
            : Result&lt;Player&gt;.Failure("玩家数据验证失败");
    });
}
```

### Q3: 如何在 Command 和 Query 中处理错误？

**A:** Command 和 Query 可以返回 Result：

```csharp
// Command 返回 Result
public class SaveGameCommand : AbstractCommand&lt;Result&lt;SaveData&gt;&gt;
{
    protected override Result&lt;SaveData&gt; OnDo()
    {
        try
        {
            var data = CollectSaveData();
            var saveSystem = this.GetSystem&lt;SaveSystem&gt;();
            return saveSystem.SaveGame(data);
        }
        catch (Exception ex)
        {
            this.GetUtility&lt;ILogger&gt;().Error("保存游戏失败", ex);
            return Result&lt;SaveData&gt;.Failure(ex);
        }
    }
}

// Query 返回 Option
public class GetPlayerQuery : AbstractQuery&lt;Option&lt;Player&gt;&gt;
{
    public string PlayerId { get; set; }

    protected override Option&lt;Player&gt; OnDo()
    {
        var playerSystem = this.GetSystem&lt;PlayerSystem&gt;();
        return playerSystem.FindPlayerById(PlayerId);
    }
}
```

### Q4: 如何处理多个可能失败的操作？

**A:** 使用 Result 的链式操作：

```csharp
public Result&lt;Trade&gt; ExecuteTrade(string sellerId, string buyerId, string itemId, int price)
{
    return ValidateSeller(sellerId)
        .Bind(_ => ValidateBuyer(buyerId))
        .Bind(_ => ValidateItem(itemId))
        .Bind(_ => ValidatePrice(price))
        .Bind(_ => TransferItem(sellerId, buyerId, itemId))
        .Bind(_ => TransferCurrency(buyerId, sellerId, price))
        .Map(_ => CreateTradeRecord(sellerId, buyerId, itemId, price));
}
```

### Q5: 如何避免 Result 嵌套过深？

**A:** 使用 Bind 扁平化嵌套：

```csharp
// ❌ 避免：嵌套过深
public Result&lt;string&gt; ProcessData(string input)
{
    var result1 = Step1(input);
    if (result1.IsSuccess)
    {
        var result2 = Step2(result1.Match(succ: v => v, fail: _ => ""));
        if (result2.IsSuccess)
        {
            var result3 = Step3(result2.Match(succ: v => v, fail: _ => ""));
            return result3;
        }
        return Result&lt;string&gt;.Failure(result2.Exception);
    }
    return Result&lt;string&gt;.Failure(result1.Exception);
}

// ✅ 好的做法：使用 Bind 扁平化
public Result&lt;string&gt; ProcessData(string input)
{
    return Step1(input)
        .Bind(Step2)
        .Bind(Step3);
}
```

### Q6: 如何在 UI 层处理错误？

**A:** 将错误转换为用户友好的消息：

```csharp
public class UIController : IController
{
    private readonly ErrorMessageService _errorMessageService;

    public void OnSaveButtonClicked()
    {
        var result = this.SendCommand(new SaveGameCommand());

        result.Match(
            succ: data => {
                ShowSuccessToast("游戏保存成功");
            },
            fail: ex => {
                var userMessage = _errorMessageService.GetUserFriendlyMessage(ex);
                ShowErrorDialog("保存失败", userMessage);
            }
        );
    }
}
```

---

## 总结

良好的错误处理是构建健壮应用的基础。遵循本指南的最佳实践：

- ✅ 使用 **Result&lt;T&gt;** 处理可预期的业务错误
- ✅ 使用 **Option&lt;T&gt;** 处理可能不存在的值
- ✅ 使用 **异常** 处理不可预期的系统错误
- ✅ 记录详细的 **日志** 信息
- ✅ 提供友好的 **用户反馈**
- ✅ 实现 **错误恢复** 和降级策略
- ✅ 编写 **测试** 验证错误处理逻辑

通过这些实践，你将构建出更加稳定、可维护、用户友好的游戏应用。

---

**相关文档**：

- [架构模式最佳实践](./architecture-patterns.md)
- [扩展方法使用指南](../core/extensions.md)
- [日志系统](../core/logging.md)

**文档版本**: 1.0.0
**更新日期**: 2026-03-07
