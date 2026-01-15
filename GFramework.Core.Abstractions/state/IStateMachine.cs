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
    void Register(IState state);

    /// <summary>
    /// 从状态机中注销指定类型的状态
    /// </summary>
    /// <typeparam name="T">要注销的状态类型，必须实现IState接口</typeparam>
    void Unregister<T>() where T : IState;

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
    void ChangeTo<T>() where T : IState;
}