namespace GFramework.Core.logging;

/// <summary>
///     控制台日志提供程序，用于创建控制台日志记录器实例
/// </summary>
public class ConsoleLoggerFactory : ILoggerFactory
{
    /// <summary>
    ///     获取指定名称的控制台日志记录器实例
    /// </summary>
    /// <param name="name">日志记录器的名称</param>
    /// <returns>控制台日志记录器实例</returns>
    public ILogger GetLogger(string name)
    {
        return new ConsoleLogger(name);
    }
}