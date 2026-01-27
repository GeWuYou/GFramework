namespace GFramework.Game.Abstractions.ui;

/// <summary>
///     UI路由守卫接口
///     用于拦截和处理UI路由切换，实现业务逻辑解耦
/// </summary>
public interface IUiRouteGuard
{
    /// <summary>
    ///     守卫优先级，数值越小越先执行
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     是否可中断后续守卫
    ///     如果返回 true，当该守卫返回 false 时，将停止执行后续守卫
    /// </summary>
    bool CanInterrupt { get; }

    /// <summary>
    ///     进入UI前的检查
    /// </summary>
    /// <param name="uiKey">目标UI标识符</param>
    /// <param name="param">进入参数</param>
    /// <returns>true表示允许进入，false表示拦截</returns>
    Task<bool> CanEnterAsync(string uiKey, IUiPageEnterParam? param);

    /// <summary>
    ///     离开UI前的检查
    /// </summary>
    /// <param name="uiKey">当前UI标识符</param>
    /// <returns>true表示允许离开，false表示拦截</returns>
    Task<bool> CanLeaveAsync(string uiKey);
}