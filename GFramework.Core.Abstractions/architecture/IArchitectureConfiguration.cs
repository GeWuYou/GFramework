using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.Abstractions.architecture;

/// <summary>
///     定义架构配置的接口，提供日志工厂、日志级别和架构选项的配置功能
/// </summary>
public interface IArchitectureConfiguration
{
    /// <summary>
    ///     获取或设置日志工厂，用于创建日志记录器实例
    /// </summary>
    ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    ///     获取或设置架构选项，包含架构相关的配置参数
    /// </summary>
    ArchitectureOptions Options { get; set; }
}