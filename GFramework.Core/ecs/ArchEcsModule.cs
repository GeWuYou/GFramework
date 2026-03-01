using Arch.Core;
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.ioc;

namespace GFramework.Core.ecs;

/// <summary>
///     Arch ECS 模块 - 核心适配器，桥接 Arch 到框架生命周期
/// </summary>
public sealed class ArchEcsModule : IServiceModule
{
    private readonly List<ArchSystemAdapter<float>> _systems = [];
    private IIocContainer? _container;
    private World? _world;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="enabled">是否启用模块</param>
    public ArchEcsModule(bool enabled = true)
    {
        IsEnabled = enabled;
    }

    /// <summary>
    ///     模块名称
    /// </summary>
    public string ModuleName => nameof(ArchEcsModule);

    /// <summary>
    ///     模块优先级
    /// </summary>
    public int Priority => 50;

    /// <summary>
    ///     是否启用
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    ///     注册服务 - 创建并注册 World
    /// </summary>
    public void Register(IIocContainer container)
    {
        if (!IsEnabled) return;

        _container = container;

        // 创建并注册 World
        _world = World.Create();
        container.Register(_world);
    }

    /// <summary>
    ///     初始化 - 从容器获取所有适配器并初始化
    /// </summary>
    public void Initialize()
    {
        if (!IsEnabled || _world == null || _container == null) return;

        // 从容器获取所有适配器
        var adapters = _container.GetAll<ArchSystemAdapter<float>>();
        if (adapters.Count > 0)
        {
            _systems.AddRange(adapters);

            // 初始化所有系统（会调用 Arch 系统的 Initialize）
            foreach (var system in _systems)
            {
                system.Initialize();
            }
        }
    }

    /// <summary>
    ///     异步销毁
    /// </summary>
    public async ValueTask DestroyAsync()
    {
        if (!IsEnabled) return;

        // 销毁所有系统
        foreach (var system in _systems)
        {
            system.Destroy();
        }

        _systems.Clear();

        // 销毁 World
        if (_world != null)
        {
            World.Destroy(_world);
            _world = null;
        }

        await ValueTask.CompletedTask;
    }

    /// <summary>
    ///     更新所有 ECS 系统
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    public void Update(float deltaTime)
    {
        if (!IsEnabled) return;

        // 调用所有系统的更新
        foreach (var system in _systems)
        {
            system.Update(deltaTime);
        }
    }
}