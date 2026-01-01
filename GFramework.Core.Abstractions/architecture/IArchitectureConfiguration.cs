using GFramework.Core.Abstractions.properties;

namespace GFramework.Core.Abstractions.architecture;

/// <summary>
///     定义架构配置的接口，提供日志工厂、日志级别和架构选项的配置功能
/// </summary>
public interface IArchitectureConfiguration
{
    /// <summary>
    ///     获取或设置日志选项，包含日志相关的配置参数
    /// </summary>
    LoggerProperties LoggerProperties { get; set; }

    /// <summary>
    ///     获取或设置架构选项，包含架构相关的配置参数
    /// </summary>
    ArchitectureProperties ArchitectureProperties { get; set; }
}