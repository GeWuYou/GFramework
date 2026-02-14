using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.properties;
using GFramework.Core.logging;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.architecture;

/// <summary>
///     默认架构配置类，实现IArchitectureConfiguration接口
///     提供日志工厂、日志级别和架构选项的默认配置
/// </summary>
public sealed class ArchitectureConfiguration : IArchitectureConfiguration
{
    /// <summary>
    ///     获取或设置日志选项
    ///     默认配置为Info级别日志，使用控制台日志工厂提供程序
    /// </summary>
    public LoggerProperties LoggerProperties { get; set; } = new()
    {
        LoggerFactoryProvider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Info
        }
    };

    /// <summary>
    ///     获取或设置架构选项
    ///     默认创建新的ArchitectureOptions实例
    /// </summary>
    public ArchitectureProperties ArchitectureProperties { get; set; } = new()
    {
        AllowLateRegistration = false,
        StrictPhaseValidation = true
    };

    /// <summary>
    ///     获取或设置Mediator配置委托
    ///     用于自定义Mediator框架的配置选项
    /// </summary>
    /// <returns>配置Mediator选项的委托函数，可为null</returns>
    public Action<MediatorOptions>? Configurator { get; set; } = options =>
    {
        options.Namespace = "GFramework.Core.Mediator";
        options.ServiceLifetime = ServiceLifetime.Singleton;
        options.GenerateTypesAsInternal = true;
        options.NotificationPublisherType = typeof(ForeachAwaitPublisher);
    };
}