# GFramework.Core 日志系统

## 概述

GFramework.Core 提供了一个灵活、可配置的日志系统，支持多级别、多类别和多种输出方式的日志记录。默认日志级别为 `Info`，确保框架的关键操作都能被记录下来。

## 主要特性

- **多级别日志支持**: Trace、Debug、Info、Warning、Error、Fatal
- **类别化日志**: 支持按模块分类记录日志
- **可配置输出**: 支持控制台输出和文件输出
- **彩色控制台输出**: 提供不同级别的颜色区分
- **灵活的级别控制**: 可全局设置或按类别设置不同的日志级别

## 快速开始

### 1. 基本配置

```csharp
using GFramework.Core.logging;

// 快速配置（推荐）
Log.Configure(
    minLevel: LogLevel.Info,      // 默认级别为Info
    enableConsole: true,          // 启用控制台输出
    useColors: true,              // 使用彩色输出
    enableFile: false,            // 不启用文件输出
    logFilePath: null             // 无文件输出
);

// 或者使用详细的配置
var config = new LogConfig
{
    DefaultMinLevel = LogLevel.Info,
    EnableConsole = true,
    UseColors = true,
    EnableFile = true,
    LogFilePath = "logs/gframework.log"
};

// 设置特定模块的日志级别
config.SetCategoryLevel("Architecture", LogLevel.Debug);
config.SetCategoryLevel("IOC", LogLevel.Debug);
config.SetCategoryLevel("Event", LogLevel.Info);

Log.Initialize(config);
```

### 2. 使用日志记录

#### 基本使用示例

```csharp
// 使用全局日志实例
Log.Info("应用程序启动");
Log.Warn("这是一个警告");
Log.Error("这是一个错误信息");

// 创建特定类别的日志记录器
var userLogger = Log.CreateLogger("UserService");
userLogger.Info("用户服务初始化完成", new { ServiceVersion = "1.0" });

var dbLogger = Log.CreateLogger("Database");
dbLogger.Debug("连接数据库", new { Server = "localhost", Database = "MyApp" });
dbLogger.Info("数据库连接成功");
```

#### 详细使用示例

```csharp
// 创建特定类别的日志记录器
var logger = Log.CreateLogger("MyModule");

logger.Debug("调试信息：连接数据库");
logger.Info("用户登录成功", new { UserId = 123, UserName = "张三" });
logger.Warn("连接超时，正在重试", new { RetryCount = 2 });
logger.Error("数据库连接失败", ex, new { DatabaseUrl = "localhost:1433" });
logger.Fatal("系统发生严重错误", new { SystemState = "Critical" });
```

#### 框架组件日志示例

```csharp
// 在架构中使用
var architecture = ExampleArchitecture.Instance;
architecture.RegisterSystem(new ExampleSystem());
architecture.RegisterModel(new ExampleModel());
architecture.RegisterUtility(new ExampleUtility());

// 在系统中使用
public class ExampleSystem : AbstractSystem
{
    protected override void OnInit()
    {
        Log.CreateLogger("System").Info("ExampleSystem 初始化完成");
    }
    
    protected override void OnDestroy()
    {
        Log.CreateLogger("System").Info("ExampleSystem 销毁");
    }
}

// 在模型中使用
public class ExampleModel : AbstractModel
{
    protected override void OnInit()
    {
        Log.CreateLogger("Model").Info("ExampleModel 初始化完成");
    }
}

// 在工具中使用
public class ExampleUtility : IUtility
{
    public void DoSomething()
    {
        Log.CreateLogger("Utility").Debug("ExampleUtility 执行操作");
    }
}

// 在事件中使用
architecture.RegisterEvent<ExampleEvent>(evt =>
{
    Log.CreateLogger("EventListener").Info($"收到事件: {evt.Message}");
});
```

### 3. 高级配置

```csharp
// 创建自定义配置的日志记录器
var config = new LogConfig
{
    DefaultMinLevel = LogLevel.Warning,  // 默认只显示Warning及以上
    EnableConsole = true,
    UseColors = true,
    EnableFile = true,
    LogFilePath = "logs/app-{yyyy-MM-dd}.log"
};

// 为不同模块设置不同的日志级别
config.SetCategoryLevel("Architecture", LogLevel.Info);     // 架构信息
config.SetCategoryLevel("IOC", LogLevel.Debug);             // IOC详细日志
config.SetCategoryLevel("Event", LogLevel.Info);            // 事件日志
config.SetCategoryLevel("System", LogLevel.Info);           // 系统日志
config.SetCategoryLevel("Database", LogLevel.Debug);        // 数据库日志
config.SetCategoryLevel("Network", LogLevel.Trace);         // 网络日志（最详细）

Log.Initialize(config);

// 演示不同级别的日志记录
var networkLogger = Log.CreateLogger("Network");
networkLogger.Trace("网络请求开始", new { Url = "https://api.example.com/users", Method = "GET" });
networkLogger.Debug("添加请求头", new { Headers = new[] { "Authorization: Bearer xxx", "Content-Type: application/json" } });
networkLogger.Info("网络请求完成", new { StatusCode = 200, ResponseTime = "120ms" });

var userServiceLogger = Log.CreateLogger("UserService");
userServiceLogger.Debug("查询用户信息", new { UserId = 123 });
userServiceLogger.Info("用户登录成功", new { UserId = 123, UserName = "张三", LoginTime = DateTime.Now });
```

### 4. 异常日志记录

```csharp
var serviceLogger = Log.CreateLogger("UserService");

try
{
    // 模拟业务逻辑中的异常
    throw new InvalidOperationException("用户数据验证失败");
}
catch (Exception ex)
{
    serviceLogger.Error("用户注册失败", ex, new 
    { 
        Operation = "UserRegistration", 
        UserEmail = "user@example.com",
        ValidationErrors = new[] { "邮箱格式无效", "密码强度不足" }
    });
}

// 演示Fatal级别的使用
try
{
    throw new SystemException("数据库连接完全中断");
}
catch (Exception ex)
{
    serviceLogger.Fatal("系统发生致命错误", ex, new 
    { 
        ErrorCode = "DB_CONNECTION_FAILED",
        RetryAttempts = 3,
        LastRetryTime = DateTime.Now.AddMinutes(-5)
    });
}
```

### 5. 生产环境配置

```csharp
// 生产环境推荐配置
var productionConfig = new LogConfig
{
    DefaultMinLevel = LogLevel.Info,        // 只记录Info及以上级别
    EnableConsole = false,                  // 生产环境通常不输出到控制台
    UseColors = false,
    EnableFile = true,                      // 启用文件日志
    LogFilePath = "logs/production-{yyyy-MM-dd}.log"
};

// 为关键模块设置更详细的日志级别
productionConfig.SetCategoryLevel("Architecture", LogLevel.Info);
productionConfig.SetCategoryLevel("Security", LogLevel.Debug);    // 安全相关日志更详细
productionConfig.SetCategoryLevel("Payment", LogLevel.Debug);     // 支付相关日志更详细
productionConfig.SetCategoryLevel("IOC", LogLevel.Warning);       // 减少IOC调试日志

Log.Initialize(productionConfig);

var securityLogger = Log.CreateLogger("Security");
securityLogger.Info("用户登录尝试", new { UserId = 456, IpAddress = "192.168.1.100" });
securityLogger.Warn("密码错误", new { UserId = 456, FailedAttempts = 2 });
securityLogger.Info("登录成功", new { UserId = 456, SessionId = "sess_abc123" });

var paymentLogger = Log.CreateLogger("Payment");
paymentLogger.Debug("开始处理支付", new { OrderId = "ORD_789", Amount = 99.99m, Currency = "CNY" });
paymentLogger.Info("支付成功", new { OrderId = "ORD_789", TransactionId = "txn_456" });
```

## 日志级别说明

- **Trace**: 最详细的跟踪信息，用于调试复杂的执行流程
- **Debug**: 调试信息，用于开发和测试阶段
- **Info**: 一般信息，记录重要的业务流程和系统状态
- **Warning**: 警告信息，可能的问题但不中断程序执行
- **Error**: 错误信息，影响功能但不致命
- **Fatal**: 致命错误，导致程序无法继续运行

## 框架内部日志

GFramework.Core 在以下关键位置自动添加了日志记录：

### 架构模块 (Architecture)
- 架构初始化流程
- 组件注册和初始化
- 生命周期阶段变更
- 模块安装和卸载

### IOC容器 (IOC)
- 对象注册和获取
- 容器冻结操作
- 重复注册检测

### 事件系统 (Event)
- 事件发送和接收
- 事件处理器注册和注销

### 系统模块 (System)
- 系统初始化和销毁
- 组件生命周期管理

## 输出格式

### 控制台输出示例
```
[2025-12-23 12:34:56.789] INFO     Architecture Architecture initialized
[2025-12-23 12:34:56.790] INFO     Architecture Initializing 3 systems
[2025-12-23 12:34:56.791] DEBUG    Architecture   Initializing system: GameSystem
[2025-12-23 12:34:56.792] INFO     Architecture   System registered: GameSystem
[2025-12-23 12:34:56.793] INFO     Architecture Architecture is ready - all components initialized
```

### 日志输出级别颜色
- **Trace**: 灰色
- **Debug**: 青色
- **Info**: 白色
- **Warning**: 黄色
- **Error**: 红色
- **Fatal**: 洋红色

## 最佳实践

1. **使用合适的日志级别**:
   - 使用 `Info` 记录重要业务流程
   - 使用 `Debug` 记录调试信息
   - 使用 `Warning` 记录异常情况
   - 使用 `Error` 记录错误但不影响程序运行
   - 使用 `Fatal` 记录严重错误

2. **提供上下文信息**:
   ```csharp
   Log.Info("用户登录成功", new { UserId = userId, UserName = userName });
   ```

3. **异常日志记录**:
   ```csharp
   try 
   {
       // 业务逻辑
   }
   catch (Exception ex)
   {
       Log.Error("数据库操作失败", ex, new { Operation = "InsertUser", UserId = userId });
   }
   ```

4. **分类使用日志**:
   ```csharp
   var dbLogger = Log.CreateLogger("Database");
   var netLogger = Log.CreateLogger("Network");
   
   dbLogger.Info("查询用户数据");
   netLogger.Debug("发送HTTP请求");
   ```

5. **在框架组件中合理使用日志**:
   ```csharp
   // 在系统初始化时记录
   protected override void OnInit()
   {
       Log.CreateLogger("System").Info("系统初始化完成");
   }
   
   // 在事件处理中记录
   architecture.RegisterEvent<UserLoginEvent>(evt =>
   {
       Log.CreateLogger("Event").Info("用户登录事件", new { UserId = evt.UserId });
   });
   ```

## 配置建议

### 开发环境
```csharp
Log.Configure(
    minLevel: LogLevel.Debug,
    enableConsole: true,
    useColors: true,
    enableFile: false
);
```

### 生产环境
```csharp
var config = new LogConfig
{
    DefaultMinLevel = LogLevel.Info,
    EnableConsole = false,           // 生产环境通常不需要控制台输出
    UseColors = false,
    EnableFile = true,
    LogFilePath = "logs/production-{yyyy-MM-dd}.log"
};

config.SetCategoryLevel("Architecture", LogLevel.Info);
config.SetCategoryLevel("IOC", LogLevel.Warning);    // 减少IOC日志
config.SetCategoryLevel("Event", LogLevel.Info);

Log.Initialize(config);
```

通过这个日志系统，你可以全面追踪GFramework.Core的执行过程，快速定位问题，并监控应用程序的运行状态。