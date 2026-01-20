using GFramework.Core.Abstractions.system;
using GFramework.Game.Abstractions.enums;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI路由管理器接口，用于管理UI界面的导航和切换操作
/// </summary>
public interface IUiRouter : ISystem
{
    /// <summary>
    /// 获取当前UI栈深度
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 绑定UI根节点
    /// </summary>
    /// <param name="root">UI根节点接口实例</param>
    void BindRoot(IUiRoot root);

    /// <summary>
    /// 将指定的UI界面压入路由栈，显示新的UI界面
    /// </summary>
    /// <param name="uiKey">UI界面的唯一标识符</param>
    /// <param name="param">进入界面的参数，可为空</param>
    /// <param name="policy">界面切换策略，默认为Exclusive（独占）</param>
    /// <param name="instancePolicy">实例管理策略，默认为Reuse（复用）</param>
    /// <param name="animationPolicy">动画策略，可为空</param>
    void Push(string uiKey, IUiPageEnterParam? param = null, UiTransitionPolicy policy = UiTransitionPolicy.Exclusive,
        UiInstancePolicy instancePolicy = UiInstancePolicy.Reuse, UiAnimationPolicy? animationPolicy = null);


    /// <summary>
    /// 将已存在的UI页面压入路由栈
    /// 用于预挂载节点或调试场景
    /// </summary>
    /// <param name="page">已创建的UI页面行为实例</param>
    /// <param name="param">进入界面的参数,可为空</param>
    /// <param name="policy">界面切换策略,默认为Exclusive(独占)</param>
    /// <param name="animationPolicy">动画策略，可为空</param>
    void Push(IUiPageBehavior page, IUiPageEnterParam? param = null,
        UiTransitionPolicy policy = UiTransitionPolicy.Exclusive, UiAnimationPolicy? animationPolicy = null);


    /// <summary>
    /// 弹出路由栈顶的UI界面，返回到上一个界面
    /// </summary>
    /// <param name="policy">界面弹出策略，默认为Destroy（销毁）</param>
    void Pop(UiPopPolicy policy = UiPopPolicy.Destroy);

    /// <summary>
    /// 替换当前所有页面为新页面（基于uiKey）
    /// </summary>
    /// <param name="uiKey">新UI页面标识符</param>
    /// <param name="param">页面进入参数，可为空</param>
    /// <param name="popPolicy">弹出页面时的销毁策略，默认为销毁</param>
    /// <param name="pushPolicy">推入页面时的过渡策略，默认为独占</param>
    /// <param name="instancePolicy">实例管理策略</param>
    /// <param name="animationPolicy">动画策略，可为空</param>
    public void Replace(
        string uiKey,
        IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive,
        UiInstancePolicy instancePolicy = UiInstancePolicy.Reuse,
        UiAnimationPolicy? animationPolicy = null);

    /// <summary>
    /// 替换当前所有页面为已存在的页面（基于实例）
    /// </summary>
    /// <param name="page">已创建的UI页面行为实例</param>
    /// <param name="param">页面进入参数，可为空</param>
    /// <param name="popPolicy">弹出页面时的销毁策略，默认为销毁</param>
    /// <param name="pushPolicy">推入页面时的过渡策略，默认为独占</param>
    /// <param name="animationPolicy">动画策略，可为空</param>
    public void Replace(
        IUiPageBehavior page,
        IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive,
        UiAnimationPolicy? animationPolicy = null);
    /// <summary>
    /// 清空所有UI界面，重置路由状态
    /// </summary>
    void Clear();

    /// <summary>
    /// 注册UI切换处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    /// <param name="options">执行选项</param>
    void RegisterHandler(IUiTransitionHandler handler, UiTransitionHandlerOptions? options = null);

    /// <summary>
    /// 注销UI切换处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    void UnregisterHandler(IUiTransitionHandler handler);

    /// <summary>
    /// 获取当前栈顶UI的Key
    /// </summary>
    /// <returns>当前UI Key，如果栈为空返回空字符串</returns>
    string PeekKey();

    /// <summary>
    /// 获取当前栈顶的UI页面行为对象
    /// </summary>
    /// <returns>栈顶的IUiPageBehavior对象，如果栈为空则返回null</returns>
    IUiPageBehavior Peek();


    /// <summary>
    /// 判断指定UI是否为当前栈顶UI
    /// </summary>
    bool IsTop(string uiKey);

    /// <summary>
    /// 判断指定UI是否存在于UI栈中
    /// </summary>
    bool Contains(string uiKey);

    #region 层级管理

    /// <summary>
    /// 在指定层级显示UI（非栈管理）
    /// </summary>
    /// <param name="uiKey">UI标识符</param>
    /// <param name="layer">UI层级</param>
    /// <param name="param">进入参数</param>
    /// <param name="instancePolicy">实例策略</param>
    void Show(
        string uiKey,
        UiLayer layer,
        IUiPageEnterParam? param = null,
        UiInstancePolicy instancePolicy = UiInstancePolicy.Reuse);

    /// <summary>
    /// 在指定层级显示UI（基于实例）
    /// </summary>
    /// <param name="page">UI页面实例</param>
    /// <param name="layer">UI层级</param>
    void Show(IUiPageBehavior page, UiLayer layer);

    /// <summary>
    /// 隐藏指定层级的UI
    /// </summary>
    /// <param name="uiKey">UI标识符</param>
    /// <param name="layer">UI层级</param>
    /// <param name="destroy">是否销毁实例</param>
    void Hide(string uiKey, UiLayer layer, bool destroy = false);

    /// <summary>
    /// 清空指定层级的所有UI
    /// </summary>
    /// <param name="layer">UI层级</param>
    /// <param name="destroy">是否销毁实例</param>
    void ClearLayer(UiLayer layer, bool destroy = false);

    /// <summary>
    /// 获取指定层级的UI实例
    /// </summary>
    /// <param name="uiKey">UI标识符</param>
    /// <param name="layer">UI层级</param>
    /// <returns>UI实例，不存在则返回null</returns>
    IUiPageBehavior? GetFromLayer(string uiKey, UiLayer layer);

    /// <summary>
    /// 判断指定层级是否有UI显示
    /// </summary>
    /// <param name="layer">UI层级</param>
    /// <returns>是否有UI显示</returns>
    bool HasVisibleInLayer(UiLayer layer);

    #endregion
}