using GFramework.Core.Abstractions.Ioc;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
///     服务模块管理器接口，用于管理架构中的服务模块。
/// </summary>
public interface IServiceModuleManager
{
    /// <summary>
    ///     注册一个服务模块。
    /// </summary>
    /// <param name="module">要注册的服务模块实例。</param>
    void RegisterModule(IServiceModule module);

    /// <summary>
    ///     注册内置的服务模块。
    /// </summary>
    /// <param name="container">IoC容器实例，用于解析依赖。</param>
    void RegisterBuiltInModules(IIocContainer container);

    /// <summary>
    ///     获取所有已注册的服务模块。
    /// </summary>
    /// <returns>只读的服务模块列表。</returns>
    IReadOnlyList<IServiceModule> GetModules();

    /// <summary>
    ///     异步初始化所有已注册的服务模块。
    /// </summary>
    /// <param name="asyncMode">是否以异步模式初始化模块。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task InitializeAllAsync(bool asyncMode);

    /// <summary>
    ///     异步销毁所有已注册的服务模块。
    /// </summary>
    /// <returns>表示异步操作的值任务。</returns>
    ValueTask DestroyAllAsync();
}