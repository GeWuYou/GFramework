using GFramework.Core.Abstractions.Architecture;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Events;

namespace GFramework.Core.Services.Modules;

/// <summary>
///     事件总线模块，用于注册和管理事件总线服务。
///     该模块负责将事件总线注册到依赖注入容器中，并提供初始化和销毁功能。
/// </summary>
public sealed class EventBusModule : IServiceModule
{
    /// <summary>
    ///     获取模块名称。
    /// </summary>
    public string ModuleName => nameof(EventBusModule);

    /// <summary>
    ///     获取模块优先级，数值越小优先级越高。
    /// </summary>
    public int Priority => 10;

    /// <summary>
    ///     获取模块启用状态，始终返回 true 表示该模块默认启用。
    /// </summary>
    public bool IsEnabled => true;

    /// <summary>
    ///     注册事件总线到依赖注入容器。
    ///     创建事件总线实例并将其注册为多例服务。
    /// </summary>
    /// <param name="container">依赖注入容器实例。</param>
    public void Register(IIocContainer container)
    {
        container.RegisterPlurality(new EventBus());
    }

    /// <summary>
    ///     初始化模块。
    ///     当前实现为空，因为事件总线无需额外初始化逻辑。
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    ///     异步销毁模块。
    ///     当前实现为空，因为事件总线无需特殊销毁逻辑。
    /// </summary>
    /// <returns>表示异步操作完成的任务。</returns>
    public ValueTask DestroyAsync()
    {
        return ValueTask.CompletedTask;
    }
}