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

```csharp
// 使用全局日志实例
Log.Info("应用程序启动完成");
Log.Warn("这是一个警告信息");
Log.Error("这是一个错误信息", exception);

// 创建特定类别的日志记录器
var logger = Log.CreateLogger("MyModule");

logger.Debug("调试信息：连接数据库");
logger.Info("用户登录成功", new { UserId = 123, UserName = "张三" });
logger.Warn("连接超时，正在重试", new { RetryCount = 2 });
logger.Error("数据库连接失败", ex, new { DatabaseUrl = "localhost:1433" });
logger.Fatal("系统发生严重错误", new { SystemState = "Critical" });
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