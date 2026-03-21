using GFramework.Game.Abstractions.Routing;

namespace GFramework.Game.Abstractions.UI;

/// <summary>
/// UI路由守卫接口
/// 用于拦截和处理UI路由切换，实现业务逻辑解耦
/// </summary>
public interface IUiRouteGuard : IRouteGuard<IUiPageBehavior>
{
    /// <summary>
    /// 进入UI前的检查
    /// </summary>
    /// <param name="uiKey">目标UI标识符</param>
    /// <param name="param">进入参数</param>
    /// <returns>true表示允许进入，false表示拦截</returns>
    ValueTask<bool> CanEnterAsync(string uiKey, IUiPageEnterParam? param);

    /// <summary>
    /// 离开UI前的检查。
    /// 该成员显式细化了通用路由守卫的离开检查，使 UI 守卫在 API 文档中保持 UI 语义。
    /// </summary>
    /// <param name="uiKey">当前UI标识符</param>
    /// <returns>true表示允许离开，false表示拦截</returns>
    new ValueTask<bool> CanLeaveAsync(string uiKey);
}