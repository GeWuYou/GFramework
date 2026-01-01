namespace GFramework.Core.Abstractions.logging;

/// <summary>
/// 定义日志工厂提供者的接口，用于创建具有指定名称和最小日志级别的日志记录器
/// </summary>
public interface ILoggerFactoryProvider
{
    /// <summary>
    /// 获取或设置日志记录器的最小日志级别，低于此级别的日志将被忽略
    /// </summary>
    public LogLevel MinLevel { get; set; }

    /// <summary>
    /// 创建一个日志记录器实例
    /// </summary>
    /// <param name="name">日志记录器的名称，用于标识特定的日志源</param>
    /// <returns>配置了指定名称和最小日志级别的ILogger实例</returns>
    ILogger CreateLogger(string name);
}