using Arch.Core;
using GFramework.Core.Abstractions.ecs;
using GFramework.Core.extensions;
using GFramework.Core.system;

namespace GFramework.Core.ecs;

/// <summary>
///     ECS系统基类，继承自AbstractSystem以集成到现有架构
/// </summary>
public abstract class EcsSystemBase : AbstractSystem, IEcsSystem
{
    /// <summary>
    ///     ECS世界实例
    /// </summary>
    protected EcsWorld EcsWorld { get; private set; } = null!;

    /// <summary>
    ///     快捷访问内部World
    /// </summary>
    protected World World => EcsWorld.InternalWorld;

    /// <summary>
    ///     系统优先级，默认为0
    /// </summary>
    public virtual int Priority => 0;

    /// <summary>
    ///     每帧更新（子类实现）
    /// </summary>
    public abstract void Update(float deltaTime);

    /// <summary>
    ///     系统初始化
    /// </summary>
    protected override void OnInit()
    {
        EcsWorld = this.GetService<EcsWorld>() ?? throw new InvalidOperationException(
            "EcsWorld not found in context. Make sure ECS is properly initialized.");

        OnEcsInit();
    }

    /// <summary>
    ///     系统销毁
    /// </summary>
    protected override void OnDestroy()
    {
        OnEcsDestroy();
    }

    /// <summary>
    ///     ECS系统初始化（子类实现）
    /// </summary>
    protected abstract void OnEcsInit();

    /// <summary>
    ///     ECS系统销毁（子类可选实现）
    /// </summary>
    protected virtual void OnEcsDestroy()
    {
    }
}