using GFramework.Core.Abstractions.ecs;
using GFramework.Core.extensions;
using GFramework.Core.system;

namespace GFramework.Core.ecs;

/// <summary>
///     ECS系统调度器，负责管理和更新所有ECS系统
/// </summary>
public sealed class EcsSystemRunner : AbstractSystem
{
    private readonly List<IEcsSystem> _systems = new();
    private bool _isRunning;

    /// <summary>
    ///     初始化调度器，从DI容器获取所有ECS系统
    /// </summary>
    protected override void OnInit()
    {
        // 从容器获取所有已注册的ECS系统
        var systemsList = this.GetService<IReadOnlyList<IEcsSystem>>();
        if (systemsList is { Count: > 0 })
        {
            // 按优先级排序
            _systems.AddRange(systemsList.OrderBy(s => s.Priority));
        }
    }

    /// <summary>
    ///     更新所有ECS系统
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    public void Update(float deltaTime)
    {
        if (!_isRunning) return;

        foreach (var system in _systems)
        {
            system.Update(deltaTime);
        }
    }

    /// <summary>
    ///     启动调度器
    /// </summary>
    public void Start()
    {
        _isRunning = true;
    }

    /// <summary>
    ///     停止调度器
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
    }

    /// <summary>
    ///     销毁调度器
    /// </summary>
    protected override void OnDestroy()
    {
        Stop();
        _systems.Clear();
    }
}