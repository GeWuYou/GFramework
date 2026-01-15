namespace GFramework.Core.Abstractions.state;

/// <summary>
/// 状态机状态接口，定义了状态的基本行为和转换规则
/// </summary>
public interface IState
{
    /// <summary>
    /// 当状态被激活进入时调用
    /// </summary>
    /// <param name="from">从哪个状态转换而来，可能为null表示初始状态</param>
    void OnEnter(IState? from);

    /// <summary>
    /// 当状态退出时调用
    /// </summary>
    /// <param name="to">将要转换到的目标状态，可能为null表示结束状态</param>
    void OnExit(IState? to);

    /// <summary>
    /// 判断当前状态是否可以转换到目标状态
    /// </summary>
    /// <param name="target">目标状态</param>
    /// <returns>如果可以转换则返回true，否则返回false</returns>
    bool CanTransitionTo(IState target);
}