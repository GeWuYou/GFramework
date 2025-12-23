namespace GFramework.Core.logging;

/// <summary>
/// 日志配置类，用于配置日志记录器的行为
/// </summary>
public sealed class LogConfig
{
    /// <summary>
    /// 获取或设置默认的最小日志级别（默认为Info）
    /// </summary>
    public LogLevel DefaultMinLevel { get; set; } = LogLevel.Info;

    /// <summary>
    /// 获取或设置是否启用控制台输出（默认为true）
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// 获取或设置是否使用彩色输出（默认为true）
    /// </summary>
    public bool UseColors { get; set; } = true;

    /// <summary>
    /// 获取或设置是否启用文件输出（默认为false）
    /// </summary>
    public bool EnableFile { get; set; }

    /// <summary>
    /// 获取或设置日志文件路径（当EnableFile为true时使用）
    /// </summary>
    public string? LogFilePath { get; set; }

    /// <summary>
    /// 取特定类别的日志级别配置
    /// </summary>
    private readonly Dictionary<string, LogLevel> _categoryLevels = new();

    /// <summary>
    /// 设置特定类别的日志级别
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <param name="level">日志级别</param>
    public void SetCategoryLevel(string category, LogLevel level)
    {
        _categoryLevels[category] = level;
    }

    /// <summary>
    /// 获取类别的日志级别，如果没有特定配置则返回默认级别
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <returns>日志级别</returns>
    public LogLevel GetCategoryLevel(string? category)
    {
        if (category != null && _categoryLevels.TryGetValue(category, out var level))
        {
            return level;
        }

        return DefaultMinLevel;
    }

    /// <summary>
    /// 创建控制台日志记录器
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <returns>日志记录器实例</returns>
    public ILog CreateConsoleLogger(string? category = null)
    {
        var level = GetCategoryLevel(category);
        return new ConsoleLogger(category, level, null, UseColors);
    }

    /// <summary>
    /// 创建文件日志记录器
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <returns>日志记录器实例</returns>
    public ILog CreateFileLogger(string? category = null)
    {
        if (!EnableFile || string.IsNullOrEmpty(LogFilePath))
            return new NullLogger();

        try
        {
            var level = GetCategoryLevel(category);
            var directory = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var writer = new StreamWriter(LogFilePath, append: true);
            return new ConsoleLogger(category, level, writer, useColors: false);
        }
        catch
        {
            // 如果创建文件失败，返回空日志记录器
            return new NullLogger();
        }
    }
}