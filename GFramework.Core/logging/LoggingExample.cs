using GFramework.Core.architecture;
using GFramework.Core.logging;
using GFramework.Core.system;
using GFramework.Core.model;
using GFramework.Core.events;
using GFramework.Core.utility;

namespace GFramework.Core.Examples;

/// <summary>
/// 日志系统使用示例
/// 演示如何在实际项目中使用GFramework.Core的日志功能
/// </summary>
public class LoggingExample
{
    /// <summary>
    /// 演示基本的日志配置和使用
    /// </summary>
    public static void DemonstrateBasicUsage()
    {
        Console.WriteLine("=== 基本日志使用示例 ===");
        
        // 1. 快速配置日志系统（默认Info级别）
        Log.Configure(
            minLevel: LogLevel.Info,
            enableConsole: true,
            useColors: true
        );
        
        // 2. 使用全局日志
        Log.Info("应用程序启动");
        Log.Warn("这是一个警告");
        Log.Error("这是一个错误信息");
        
        // 3. 创建特定类别的日志记录器
        var userLogger = Log.CreateLogger("UserService");
        userLogger.Info("用户服务初始化完成", new { ServiceVersion = "1.0" });
        
        var dbLogger = Log.CreateLogger("Database");
        dbLogger.Debug("连接数据库", new { Server = "localhost", Database = "MyApp" });
        dbLogger.Info("数据库连接成功");
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// 演示高级日志配置
    /// </summary>
    public static void DemonstrateAdvancedConfiguration()
    {
        Console.WriteLine("=== 高级日志配置示例 ===");
        
        // 创建详细的日志配置
        var config = new LogConfig
        {
            DefaultMinLevel = LogLevel.Info,
            EnableConsole = true,
            UseColors = true,
            EnableFile = true,
            LogFilePath = "logs/example-{yyyy-MM-dd}.log"
        };
        
        // 为不同模块设置不同的日志级别
        config.SetCategoryLevel("Architecture", LogLevel.Info);
        config.SetCategoryLevel("IOC", LogLevel.Debug);
        config.SetCategoryLevel("Event", LogLevel.Info);
        config.SetCategoryLevel("UserService", LogLevel.Debug);
        config.SetCategoryLevel("Database", LogLevel.Debug);
        config.SetCategoryLevel("Network", LogLevel.Trace); // 最详细的网络日志
        
        // 初始化日志系统
        Log.Initialize(config);
        
        // 演示不同级别的日志记录
        var networkLogger = Log.CreateLogger("Network");
        networkLogger.Trace("网络请求开始", new { Url = "https://api.example.com/users", Method = "GET" });
        networkLogger.Debug("添加请求头", new { Headers = new[] { "Authorization: Bearer xxx", "Content-Type: application/json" } });
        networkLogger.Info("网络请求完成", new { StatusCode = 200, ResponseTime = "120ms" });
        
        var userServiceLogger = Log.CreateLogger("UserService");
        userServiceLogger.Debug("查询用户信息", new { UserId = 123 });
        userServiceLogger.Info("用户登录成功", new { UserId = 123, UserName = "张三", LoginTime = DateTime.Now });
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// 演示在框架组件中使用日志
    /// </summary>
    public static void DemonstrateFrameworkLogging()
    {
        Console.WriteLine("=== 框架组件日志示例 ===");
        
        // 配置日志系统
        Log.Configure(
            minLevel: LogLevel.Info,
            enableConsole: true,
            useColors: true
        );
        
        // 创建示例架构
        var architecture = ExampleArchitecture.Instance;
        
        // 注册组件（会自动记录日志）
        architecture.RegisterSystem(new ExampleSystem());
        architecture.RegisterModel(new ExampleModel());
        architecture.RegisterUtility(new ExampleUtility());
        
        // 发送事件（会自动记录日志）
        architecture.SendEvent(new ExampleEvent { Message = "框架日志示例事件" });
        
        // 注册事件监听器（会自动记录日志）
        architecture.RegisterEvent<ExampleEvent>(evt =>
        {
            Log.CreateLogger("EventListener").Info($"收到事件: {evt.Message}");
        });
        
        // 再次发送事件
        architecture.SendEvent(new ExampleEvent { Message = "框架日志示例事件2" });
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// 演示异常日志记录
    /// </summary>
    public static void DemonstrateExceptionLogging()
    {
        Console.WriteLine("=== 异常日志记录示例 ===");
        
        Log.Configure(LogLevel.Debug, enableConsole: true, useColors: true);
        
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
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// 演示生产环境日志配置
    /// </summary>
    public static void DemonstrateProductionConfiguration()
    {
        Console.WriteLine("=== 生产环境日志配置示例 ===");
        
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
        
        Console.WriteLine("生产环境日志已配置完成，日志文件将保存到 logs/ 目录");
        Console.WriteLine();
    }
}

/// <summary>
/// 示例架构类
/// </summary>
public class ExampleArchitecture : Architecture<ExampleArchitecture>
{
    protected override void Init()
    {
        Log.CreateLogger("Architecture").Info("ExampleArchitecture 初始化");
    }
}

/// <summary>
/// 示例系统
/// </summary>
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

/// <summary>
/// 示例模型
/// </summary>
public class ExampleModel : AbstractModel
{
    protected override void OnInit()
    {
        Log.CreateLogger("Model").Info("ExampleModel 初始化完成");
    }
}

/// <summary>
/// 示例工具
/// </summary>
public class ExampleUtility : IUtility
{
    public void DoSomething()
    {
        Log.CreateLogger("Utility").Debug("ExampleUtility 执行操作");
    }
}

/// <summary>
/// 示例事件
/// </summary>
public class ExampleEvent
{
    public string Message { get; set; } = string.Empty;
}