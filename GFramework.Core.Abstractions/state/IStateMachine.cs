using System;
using System.Collections.Generic;

namespace GFramework.Core.Abstractions.state;

/// <summary>
/// 状态机接口，用于管理状态的注册、切换和验证
/// </summary>
public interface IStateMachine
{
    /// <summary>
    /// 获取当前激活的状态
    /// </summary>
    IState? Current { get; }

    /// <summary>
    /// 注册一个状态到状态机中
    /// </summary>
    /// <param name="state">要注册的状态实例</param>
    IStateMachine Register(IState state);

    /// <summary>
    /// 从状态机中注销指定类型的状态
    /// </summary>
    /// <typeparam name="T">要注销的状态类型，必须实现IState接口</typeparam>
    IStateMachine Unregister<T>() where T : IState;

    /// <summary>
    /// 检查是否可以切换到指定类型的状态
    /// </summary>
    /// <typeparam name="T">目标状态类型，必须实现IState接口</typeparam>
    /// <returns>如果可以切换则返回true，否则返回false</returns>
    bool CanChangeTo<T>() where T : IState;

    /// <summary>
    /// 切换到指定类型的状态
    /// </summary>
    /// <typeparam name="T">要切换到的状态类型，必须实现IState接口</typeparam>
    /// <returns>如果成功切换则返回true，否则返回false</returns>
    bool ChangeTo<T>() where T : IState;

    /// <summary>
    /// 检查指定类型的状态是否已注册
    /// </summary>
    /// <typeparam name="T">要检查的状态类型</typeparam>
    /// <returns>如果状态已注册则返回true，否则返回false</returns>
    bool IsRegistered<T>() where T : IState;

    /// <summary>
    /// 获取指定类型的已注册状态实例
    /// </summary>
    /// <typeparam name="T">要获取的状态类型</typeparam>
    /// <returns>如果状态存在则返回对应实例，否则返回null</returns>
    T? GetState<T>() where T : class, IState;

    /// <summary>
    /// 获取所有已注册状态的类型集合
    /// </summary>
    /// <returns>包含所有已注册状态类型的枚举器</returns>
    IEnumerable<Type> GetRegisteredStateTypes();

    /// <summary>
    /// 获取上一个状态
    /// </summary>
    /// <returns>如果历史记录存在则返回上一个状态，否则返回null</returns>
    IState? GetPreviousState();

    /// <summary>
    /// 获取状态历史记录
    /// </summary>
    /// <returns>状态历史记录的只读副本</returns>
    IReadOnlyList<IState> GetStateHistory();

    /// <summary>
    /// 回退到上一个状态
    /// </summary>
    /// <returns>如果成功回退则返回true，否则返回false</returns>
    bool GoBack();

    /// <summary>
    /// 清空状态历史记录
    /// </summary>
    void ClearHistory();
}