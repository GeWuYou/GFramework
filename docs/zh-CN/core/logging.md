# Logging 包使用说明

## 概述

Logging 包提供了灵活的日志系统，支持多级别日志记录。默认日志级别为 `Info`，确保框架的关键操作都能被记录下来。

## 核心接口

### [ILogger](../GFramework.Core.Abstractions/logging/ILogger.cs)

日志记录器接口，定义了日志记录的基本功能。

**核心方法：**

```csharp
// 日志级别检查
bool IsTraceEnabled();
bool IsDebugEnabled();
bool IsInfoEnabled();
bool IsWarnEnabled();
bool IsErrorEnabled();
bool IsFatalEnabled();

// 记录日志
void Trace(string msg);
void Trace(string format, object arg);
void Trace(string format, object arg1, object arg2);
void Trace(string format, params object[] arguments);
void Trace(string msg, Exception t);

void Debug(string msg);
void Debug(string format, object arg);
void Debug(string format, object arg1, object arg2);
void Debug(string format, params object[] arguments);
void Debug(string msg, Exception t);

void Info(string msg);
void Info(string format, object arg);
void Info(string format, object arg1, object arg2);
void Info(string format, params object[] arguments);
void Info(string msg, Exception t);

void Warn(string msg);
void Warn(string format, object arg);
void Warn(string format, object arg1, object arg2);
void Warn(string format, params object[] arguments);
void Warn(string msg, Exception t);

void Error(string msg);
void Error(string format, object arg);
void Error(string format, object arg1, object arg2);
void Error(string format, params object[] arguments);
void Error(string msg, Exception t);

void Fatal(string msg);
void Fatal(string format, object arg);
void Fatal(string format, object arg1, object arg2);
void Fatal(string format, params object[] arguments);
void Fatal(string msg, Exception t);

// 获取日志器名称
string Name();
```

### [ILoggerFactory](../GFramework.Core.Abstractions/logging/ILoggerFactory.cs)

日志工厂接口，用于创建日志记录器实例。

**核心方法：**

```csharp
ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info);
```

### [ILoggerFactoryProvider](../GFramework.Core.Abstractions/logging/ILoggerFactoryProvider.cs)

日志工厂提供程序接口，用于获取日志工厂。

**核心方法：**

```csharp
ILoggerFactory GetLoggerFactory();
ILogger CreateLogger(string name);
```

### [LogLevel](../GFramework.Core.Abstractions/logging/LogLevel.cs)

日志级别枚举。

```csharp
public enum LogLevel
{
    Trace = 0,   // 最详细的跟踪信息
    Debug = 1,   // 调试信息
    Info = 2,    // 一般信息（默认级别）
    Warning = 3, // 警告信息
    Error = 4,   // 错误信息
    Fatal = 5    // 致命错误
}
```

## 核心类

### [AbstractLogger](AbstractLogger.cs)

抽象日志基类，封装了日志级别判断、格式化与异常处理逻辑。平台日志器只需实现 `Write` 方法即可。

**使用示例：**

```csharp
public class CustomLogger : AbstractLogger
{
    public CustomLogger(string? name = null, LogLevel minLevel = LogLevel.Info) 
        : base(name, minLevel)
    {
    }

    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        // 自定义日志输出逻辑
        var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        if (exception != null)
            logMessage += $"\n{exception}";
        
        Console.WriteLine(logMessage);
    }
}
```

### [ConsoleLogger](ConsoleLogger.cs)

控制台日志记录器实现，支持彩色输出。

**使用示例：**

```csharp
// 创建控制台日志记录器
var logger = new ConsoleLogger("MyLogger", LogLevel.Debug);

// 记录不同级别的日志
logger.Info("应用程序启动");
logger.Debug("调试信息");
logger.Warn("警告信息");
logger.Error("错误信息");
logger.Fatal("致命错误");
```

**输出格式：**

```
[2025-01-09 01:40:00.000] INFO     [MyLogger] 应用程序启动
[2025-01-09 01:40:01.000] DEBUG    [MyLogger] 调试信息
[2025-01-09 01:40:02.000] WARN     [MyLogger] 警告信息
```

**日志级别颜色：**

- **Trace**: 深灰色
- **Debug**: 青色
- **Info**: 白色
- **Warning**: 黄色
- **Error**: 红色
- **Fatal**: 洋红色

**构造函数参数：**

- `name`：日志器名称，默认为 "ROOT"
- `minLevel`：最低日志级别，默认为 LogLevel.Info
- `writer`：TextWriter 输出流，默认为 Console.Out
- `useColors`：是否使用颜色，默认为 true（仅在输出到控制台时生效）

### [ConsoleLoggerFactory](ConsoleLoggerFactory.cs)

控制台日志工厂，用于创建控制台日志记录器实例。

**使用示例：**

```csharp
var factory = new ConsoleLoggerFactory();
var logger = factory.GetLogger("MyModule", LogLevel.Debug);
logger.Info("日志记录器创建成功");
```

### [ConsoleLoggerFactoryProvider](ConsoleLoggerFactoryProvider.cs)

控制台日志工厂提供程序实现。

**使用示例：**

```csharp
var provider = new ConsoleLoggerFactoryProvider();
provider.MinLevel = LogLevel.Debug;  // 设置最低日志级别
var logger = provider.CreateLogger("MyApp");
logger.Info("应用程序启动");
```

### [LoggerFactoryResolver](LoggerFactoryResolver.cs)

日志工厂提供程序解析器，用于管理和提供日志工厂提供程序实例。

**使用示例：**

```csharp
// 设置日志工厂提供程序
LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();

// 设置最小日志级别
LoggerFactoryResolver.MinLevel = LogLevel.Debug;

// 获取日志记录器
var logger = LoggerFactoryResolver.Provider.CreateLogger("MyApp");
logger.Info("应用程序启动");
```

## 在架构中使用日志

### 1. 在 Architecture 中使用

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        var logger = LoggerFactoryResolver.Provider.CreateLogger("GameArchitecture");
        logger.Info("游戏架构初始化开始");

        RegisterModel(new PlayerModel());
        RegisterSystem(new GameSystem());

        logger.Info("游戏架构初始化完成");
    }
}
```

### 2. 在 System 中使用

```csharp
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        var logger = LoggerFactoryResolver.Provider.CreateLogger("CombatSystem");
        logger.Info("战斗系统初始化完成");
    }
    
    protected override void OnDestroy()
    {
        var logger = LoggerFactoryResolver.Provider.CreateLogger("CombatSystem");
        logger.Info("战斗系统已销毁");
    }
}
```

### 3. 在 Model 中使用

```csharp
public class PlayerModel : AbstractModel
{
    protected override void OnInit()
    {
        var logger = LoggerFactoryResolver.Provider.CreateLogger("PlayerModel");
        logger.Info("玩家模型初始化完成");
    }
}
```

### 4. 自定义日志级别

```
public class DebugLogger : AbstractLogger
{
    public DebugLogger() : base("Debug", LogLevel.Debug)
    {
    }

    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        // 只输出调试及更高级别的日志
        if (level >= LogLevel.Debug)
        {
            Console.WriteLine($"[{level}] {message}");
            if (exception != null)
                Console.WriteLine(exception);
        }
    }
}
```

## 日志级别说明

| 级别          | 说明       | 使用场景              |
|-------------|----------|-------------------|
| **Trace**   | 最详细的跟踪信息 | 调试复杂的执行流程，记录函数调用等 |
| **Debug**   | 调试信息     | 开发阶段，记录变量值、流程分支等  |
| **Info**    | 一般信息     | 记录重要的业务流程和系统状态    |
| **Warning** | 警告信息     | 可能的问题但不中断程序执行     |
| **Error**   | 错误信息     | 影响功能但不致命的问题       |
| **Fatal**   | 致命错误     | 导致程序无法继续运行的严重错误   |

## 最佳实践

1. **使用合适的日志级别**：
    - 使用 `Info` 记录重要业务流程
    - 使用 `Debug` 记录调试信息
    - 使用 `Warning` 记录异常情况
    - 使用 `Error` 记录错误但不影响程序运行
    - 使用 `Fatal` 记录严重错误

2. **提供上下文信息**：
   ```csharp
   logger.Info($"用户登录成功: UserId={userId}, UserName={userName}");
   ```

3. **异常日志记录**：
   ```csharp
   try 
   {
       // 业务逻辑
   }
   catch (Exception ex)
   {
       logger.Error("数据库操作失败", ex);
   }
   ```

4. **分类使用日志**：
   ```csharp
   var dbLogger = LoggerFactoryResolver.Provider.CreateLogger("Database");
   var netLogger = LoggerFactoryResolver.Provider.CreateLogger("Network");
   
   dbLogger.Info("查询用户数据");
   netLogger.Debug("发送HTTP请求");
   ```

5. **在框架组件中合理使用日志**：
   ```csharp
   // 在系统初始化时记录
   var logger = LoggerFactoryResolver.Provider.CreateLogger("System");
   logger.Info("系统初始化完成");
   ```

## 注意事项

1. **日志级别检查**：
    - 每个日志方法都会自动检查日志级别
    - 如果当前级别低于最小级别，不会输出日志

2. **格式化参数**：
    - 支持字符串格式化参数
    - 支持异常信息传递

3. **ConsoleLogger 的额外参数**：
    - ConsoleLogger 现在支持自定义TextWriter输出流
    - 支持禁用颜色输出的功能（useColors参数）

## 相关包

- [architecture](./architecture.md) - 架构核心，使用日志系统记录生命周期事件
- [property](./property.md) - 可绑定属性基于事件系统实现
- [extensions](./extensions.md) - 提供便捷的扩展方法

---

**许可证**: Apache 2.0