using GFramework.Core.Abstractions.system;

namespace GFramework.Game.Abstractions.coroutine;

/// <summary>
/// 协程系统接口，继承自ISystem，用于管理游戏中的协程执行
/// </summary>
public interface ICoroutineSystem : ISystem
{
    /// <summary>
    /// 更新协程系统，在每一帧调用以处理协程逻辑
    /// </summary>
    /// <param name="deltaTime">自上一帧以来的时间间隔（秒）</param>
    void OnUpdate(float deltaTime);
}