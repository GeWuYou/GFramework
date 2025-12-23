namespace GFramework.Core.logging;

/// <summary>
/// 日志记录的静态类，提供全局日志记录功能
/// </summary>
public static class Log
{
    private static ILoggerFactory? _factory;
    private static LogConfig? _config;

    /// <summary>
    /// 获取当前的日志记录器实例
    /// </summary>
    public static ILog Instance { get; private set; } = new ConsoleLogger(null, LogLevel.Info);

    /// <summary>
    /// 获取当前的日志配置
    /// </summary>
    public static LogConfig Config => _config ??= new LogConfig();

    /// <summary>
    /// 设置日志记录器实例
    /// </summary>
    /// <param name="logger">要设置的日志记录器，如果为 null 则使用默认的ConsoleLogger</param>
    public static void SetLogger(ILog? logger)
    {
        Instance = logger ?? new ConsoleLogger(null, LogLevel.Info);
    }

    /// <summary>
    /// 使用日志工厂创建日志记录器
    /// </summary>
    /// <param name="factory">日志工厂实例</param>
    public static void SetLoggerFactory(ILoggerFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Instance = _factory.CreateGlobalLogger();
    }

    /// <summary>
    /// 使用日志配置初始化日志系统
    /// </summary>
    /// <param name="config">日志配置</param>
    public static void Initialize(LogConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _factory = new LoggerFactory(config);
        Instance = _factory.CreateGlobalLogger();
    }

    /// <summary>
    /// 快速配置日志系统
    /// </summary>
    /// <param name="minLevel">最小日志级别（默认为Info）</param>
    /// <param name="enableConsole">是否启用控制台输出（默认为true）</param>
    /// <param name="useColors">是否使用彩色输出（默认为true）</param>
    /// <param name="enableFile">是否启用文件输出（默认为false）</param>
    /// <param name="logFilePath">日志文件路径（可选）</param>
    public static void Configure(
        LogLevel minLevel = LogLevel.Info,
        bool enableConsole = true,
        bool useColors = true,
        bool enableFile = false,
        string? logFilePath = null)
    {
        var config = new LogConfig
        {
            DefaultMinLevel = minLevel,
            EnableConsole = enableConsole,
            UseColors = useColors,
            EnableFile = enableFile,
            LogFilePath = logFilePath
        };

        Initialize(config);
    }

    /// <summary>
    /// 创建指定类别的日志记录器
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <returns>日志记录器实例</returns>
    public static ILog CreateLogger(string category)
    {
        if (_factory != null)
        {
            return _factory.Create(category);
        }

        // 如果没有设置工厂，使用默认配置
        return new ConsoleLogger(category, Config.GetCategoryLevel(category));
    }

    /// <summary>
    /// 检查指定日志级别是否启用
    /// </summary>
    /// <param name="level">要检查的日志级别</param>
    /// <returns>如果指定级别已启用则返回 true，否则返回 false</returns>
    public static bool IsEnabled(LogLevel level) => Instance.IsEnabled(level);

    /// <summary>
    /// 记录信息级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Info(string msg, object? ctx = null)
        => Instance.Log(LogLevel.Info, msg, null, ctx);

    /// <summary>
    /// 记录错误级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ex">相关异常对象（可选）</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Error(string msg, Exception? ex = null, object? ctx = null)
        => Instance.Log(LogLevel.Error, msg, ex, ctx);

    /// <summary>
    /// 记录调试级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Debug(string msg, object? ctx = null)
        => Instance.Log(LogLevel.Debug, msg, null, ctx);

    /// <summary>
    /// 记录跟踪级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Trace(string msg, object? ctx = null)
        => Instance.Log(LogLevel.Trace, msg, null, ctx);

    /// <summary>
    /// 记录警告级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Warn(string msg, object? ctx = null)
        => Instance.Log(LogLevel.Warning, msg, null, ctx);

    /// <summary>
    /// 记录致命错误级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Fatal(string msg, object? ctx = null)
        => Instance.Log(LogLevel.Fatal, msg, null, ctx);
}
