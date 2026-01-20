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

    #region 路由守卫

    /// <summary>
    /// 注册路由守卫
    /// </summary>
    /// <param name="guard">守卫实例</param>
    void AddGuard(IUiRouteGuard guard);

    /// <summary>
    /// 移除路由守卫
    /// </summary>
    /// <param name="guard">守卫实例</param>
    void RemoveGuard(IUiRouteGuard guard);

    /// <summary>
    /// 注册路由守卫（泛型方法）
    /// </summary>
    /// <typeparam name="T">守卫类型，必须实现 IUiRouteGuard 且有无参构造函数</typeparam>
    void AddGuard<T>() where T : IUiRouteGuard, new();

    #endregion
}