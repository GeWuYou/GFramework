using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Ecs.Arch.Extensions;

/// <summary>
///     Arch ECS 扩展方法
/// </summary>
public static class ArchExtensions
{
    /// <summary>
    ///     添加 Arch ECS 支持到架构中
    /// </summary>
    /// <typeparam name="TArchitecture">架构类型</typeparam>
    /// <param name="architecture">架构实例</param>
    /// <param name="configure">可选的配置委托</param>
    /// <returns>架构实例，支持链式调用</returns>
    public static TArchitecture UseArch<TArchitecture>(
        this TArchitecture architecture,
        Action<ArchOptions>? configure = null)
        where TArchitecture : IArchitecture
    {
        // 配置选项
        var options = new ArchOptions();
        configure?.Invoke(options);

        // 注册模块（传递配置选项）
        ArchitectureModuleRegistry.Register(() => new ArchEcsModule(options, enabled: true));

        return architecture;
    }
}