using GFramework.Core.Abstractions.state;
using GFramework.Core.state;

namespace GFramework.Game.state;

/// <summary>
/// 游戏状态机类，继承自ContextAwareStateMachine，用于管理游戏中的各种状态
/// </summary>
public sealed class GameStateMachineSystem : StateMachineSystem
{
    /// <summary>
    /// 检查当前状态是否为指定类型的状态
    /// </summary>
    /// <typeparam name="T">要检查的状态类型，必须实现IState接口</typeparam>
    /// <returns>如果当前状态是指定类型则返回true，否则返回false</returns>
    public bool IsIn<T>() where T : IState
        => Current is T;

    /// <summary>
    /// 获取当前状态的实例，如果当前状态是指定类型则进行类型转换
    /// </summary>
    /// <typeparam name="T">要获取的状态类型，必须是引用类型并实现IState接口</typeparam>
    /// <returns>如果当前状态是指定类型则返回转换后的实例，否则返回null</returns>
    public T? Get<T>() where T : class, IState
        => Current as T;
}