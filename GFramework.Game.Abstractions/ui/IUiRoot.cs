using GFramework.Game.Abstractions.enums;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
///     UI根节点接口，定义了UI页面容器的基本操作
/// </summary>
public interface IUiRoot
{
    /// <summary>
    ///     向UI根节点添加子页面
    /// </summary>
    /// <param name="child">要添加的UI页面子节点</param>
    void AddUiPage(IUiPageBehavior child);

    /// <summary>
    ///     向UI根节点添加子页面到指定层级
    /// </summary>
    /// <param name="child">要添加的UI页面子节点</param>
    /// <param name="layer">层级</param>
    /// <param name="orderInLayer">层级内排序</param>
    void AddUiPage(IUiPageBehavior child, UiLayer layer, int orderInLayer = 0);

    /// <summary>
    ///     从UI根节点移除子页面
    /// </summary>
    /// <param name="child">要移除的UI页面子节点</param>
    void RemoveUiPage(IUiPageBehavior child);
}