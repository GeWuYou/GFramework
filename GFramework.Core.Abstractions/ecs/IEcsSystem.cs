using GFramework.Core.Abstractions.system;

namespace GFramework.Core.Abstractions.ecs;

/// <summary>
///     ECS系统接口，继承自ISystem以集成到现有架构
/// </summary>
public interface IEcsSystem : ISystem
{
    /// <summary>
    ///     系统优先级，数值越小越先执行
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     每帧更新
    /// </summary>
    /// <param name="deltaTime">帧间隔时间（秒）</param>
    void Update(float deltaTime);
}