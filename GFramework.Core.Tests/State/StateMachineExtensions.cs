using System.Reflection;
using GFramework.Core.Abstractions.State;
using GFramework.Core.State;

namespace GFramework.Core.Tests.State;

/// <summary>
///     为状态机测试提供仅限断言场景使用的反射辅助方法。
/// </summary>
public static class StateMachineExtensions
{
    /// <summary>
    ///     检查状态机内部缓存中是否已注册指定类型的状态。
    /// </summary>
    /// <typeparam name="T">要检查的状态类型。</typeparam>
    /// <param name="stateMachine">待检查的状态机实例。</param>
    /// <returns>找到对应状态类型时返回 <see langword="true" />，否则返回 <see langword="false" />。</returns>
    public static bool ContainsState<T>(this StateMachine stateMachine) where T : IState
    {
        return stateMachine.GetType().GetField("States", BindingFlags.NonPublic | BindingFlags.Instance)?
                   .GetValue(stateMachine) is Dictionary<Type, IState> states &&
               states.ContainsKey(typeof(T));
    }
}
