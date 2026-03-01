using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.ecs;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.lifecycle;
using GFramework.Core.ecs;

namespace GFramework.Core.services.modules;

/// <summary>
///     ECS（Entity Component System）模块，用于注册、初始化和管理ECS相关服务。
///     该模块负责创建ECS世界和系统运行器，并将其注册到依赖注入容器中。
/// </summary>
public sealed class EcsServiceModule : IServiceModule
{
    private EcsSystemRunner? _ecsRunner;

    private EcsWorld? _ecsWorld;

    /// <summary>
    ///     构造函数，初始化ECS模块。
    /// </summary>
    /// <param name="enabled">指定模块是否启用，默认为 true。</param>
    public EcsServiceModule(bool enabled = true)
    {
        IsEnabled = enabled;
    }

    /// <summary>
    ///     获取模块名称。
    /// </summary>
    public string ModuleName => nameof(EcsServiceModule);

    /// <summary>
    ///     获取模块优先级，数值越小优先级越高。
    /// </summary>
    public int Priority => 100;

    /// <summary>
    ///     获取模块启用状态。
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    ///     注册ECS相关服务到依赖注入容器中。
    ///     包括ECS世界实例和系统运行器实例的注册。
    /// </summary>
    /// <param name="container">依赖注入容器实例。</param>
    public void Register(IIocContainer container)
    {
        if (!IsEnabled) return;

        _ecsWorld = new EcsWorld();
        container.Register(_ecsWorld);
        container.Register<IEcsWorld>(_ecsWorld);

        container.RegisterPlurality<EcsSystemRunner>();
        _ecsRunner = container.Get<EcsSystemRunner>();
    }

    /// <summary>
    ///     初始化ECS模块。
    ///     如果系统运行器实现了IInitializable接口，则调用其初始化方法。
    /// </summary>
    public void Initialize()
    {
        if (!IsEnabled || _ecsRunner == null) return;

        if (_ecsRunner is IInitializable initializable)
        {
            initializable.Initialize();
        }
    }

    /// <summary>
    ///     异步销毁ECS模块并释放相关资源。
    ///     包括销毁系统运行器和释放ECS世界资源。
    /// </summary>
    /// <returns>表示异步操作完成的任务。</returns>
    public async ValueTask DestroyAsync()
    {
        if (!IsEnabled) return;

        if (_ecsRunner is IDestroyable destroyable)
        {
            destroyable.Destroy();
        }

        _ecsRunner = null;

        if (_ecsWorld != null)
        {
            _ecsWorld.Dispose();
            _ecsWorld = null;
        }

        await ValueTask.CompletedTask;
    }

    /// <summary>
    ///     获取ECS世界实例。
    /// </summary>
    /// <returns>ECS世界实例，如果未启用则返回 null。</returns>
    public IEcsWorld? GetEcsWorld() => _ecsWorld;

    /// <summary>
    ///     获取ECS系统运行器实例（内部使用）。
    /// </summary>
    /// <returns>ECS系统运行器实例，如果未启用则返回 null。</returns>
    internal EcsSystemRunner? GetEcsRunner() => _ecsRunner;
}