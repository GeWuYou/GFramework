using GFramework.Core.logging;

namespace GFramework.Core.architecture;

/// <summary>
/// 默认架构配置类，实现IArchitectureConfiguration接口
/// 提供日志工厂、日志级别和架构选项的默认配置
/// </summary>
public class DefaultArchitectureConfiguration: IArchitectureConfiguration
{
    /// <summary>
    /// 获取或设置日志工厂实例
    /// 默认使用控制台日志工厂
    /// </summary>
    public ILoggerFactory LoggerFactory { get; set; } = new ConsoleLoggerFactory();
    
    /// <summary>
    /// 获取或设置日志级别
    /// 默认设置为Info级别
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
    
    /// <summary>
    /// 获取或设置架构选项
    /// 默认创建新的ArchitectureOptions实例
    /// </summary>
    public ArchitectureOptions Options { get; set; } = new();
}
