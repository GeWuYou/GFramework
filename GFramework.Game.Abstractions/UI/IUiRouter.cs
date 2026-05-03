// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Systems;
using GFramework.Game.Abstractions.Enums;

namespace GFramework.Game.Abstractions.UI;

/// <summary>
///     UI路由管理器接口，用于管理UI界面的导航和切换操作
/// </summary>
public interface IUiRouter : ISystem
{
    /// <summary>
    ///     获取当前UI栈深度
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     绑定UI根节点
    /// </summary>
    /// <param name="root">UI根节点接口实例</param>
    void BindRoot(IUiRoot root);

    /// <summary>
    ///     将指定的UI界面压入路由栈，显示新的UI界面
    /// </summary>
    /// <param name="uiKey">UI界面的唯一标识符</param>
    /// <param name="param">进入界面的参数，可为空</param>
    /// <param name="policy">界面切换策略，默认为Exclusive（独占）</param>
    ValueTask PushAsync(string uiKey, IUiPageEnterParam? param = null,
        UiTransitionPolicy policy = UiTransitionPolicy.Exclusive);


    /// <summary>
    ///     将已存在的UI页面压入路由栈
    ///     用于预挂载节点或调试场景
    /// </summary>
    /// <param name="page">已创建的UI页面行为实例</param>
    /// <param name="param">进入界面的参数,可为空</param>
    /// <param name="policy">界面切换策略,默认为Exclusive(独占)</param>
    ValueTask PushAsync(IUiPageBehavior page, IUiPageEnterParam? param = null,
        UiTransitionPolicy policy = UiTransitionPolicy.Exclusive);


    /// <summary>
    ///     弹出路由栈顶的UI界面，返回到上一个界面
    /// </summary>
    /// <param name="policy">界面弹出策略，默认为Destroy（销毁）</param>
    ValueTask PopAsync(UiPopPolicy policy = UiPopPolicy.Destroy);

    /// <summary>
    ///     替换当前所有页面为新页面（基于uiKey）
    /// </summary>
    /// <param name="uiKey">新UI页面标识符</param>
    /// <param name="param">页面进入参数，可为空</param>
    /// <param name="popPolicy">弹出页面时的销毁策略，默认为销毁</param>
    /// <param name="pushPolicy">推入页面时的过渡策略，默认为独占</param>
    public ValueTask ReplaceAsync(
        string uiKey,
        IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive);

    /// <summary>
    ///     替换当前所有页面为已存在的页面（基于实例）
    /// </summary>
    /// <param name="page">已创建的UI页面行为实例</param>
    /// <param name="param">页面进入参数，可为空</param>
    /// <param name="popPolicy">弹出页面时的销毁策略，默认为销毁</param>
    /// <param name="pushPolicy">推入页面时的过渡策略，默认为独占</param>
    public ValueTask ReplaceAsync(
        IUiPageBehavior page,
        IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive);

    /// <summary>
    ///     清空所有UI界面，重置路由状态
    /// </summary>
    ValueTask ClearAsync();

    /// <summary>
    ///     注册UI切换处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    /// <param name="options">执行选项</param>
    void RegisterHandler(IUiTransitionHandler handler, UiTransitionHandlerOptions? options = null);

    /// <summary>
    ///     注销UI切换处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    void UnregisterHandler(IUiTransitionHandler handler);

    /// <summary>
    ///     获取当前栈顶UI的Key
    /// </summary>
    /// <returns>当前UI Key，如果栈为空返回空字符串</returns>
    string PeekKey();

    /// <summary>
    ///     获取当前栈顶的UI页面行为对象
    /// </summary>
    /// <returns>栈顶的IUiPageBehavior对象，如果栈为空则返回null</returns>
    IUiPageBehavior? Peek();


    /// <summary>
    ///     判断指定UI是否为当前栈顶UI
    /// </summary>
    bool IsTop(string uiKey);

    /// <summary>
    ///     判断指定UI是否存在于UI栈中
    /// </summary>
    bool Contains(string uiKey);

    #region Layer UI

    /// <summary>
    ///     在指定层级显示UI（Overlay / Modal / Toast等）
    /// </summary>
    /// <param name="uiKey">要显示的UI页面的唯一标识符</param>
    /// <param name="layer">UI显示的层级，例如 Overlay、Modal 或 Toast</param>
    /// <param name="param">可选参数，用于传递给UI页面的初始化数据</param>
    UiHandle Show(
        string uiKey,
        UiLayer layer,
        IUiPageEnterParam? param = null);

    /// <summary>
    ///     在指定层级显示UI（基于已存在实例）
    /// </summary>
    UiHandle Show(IUiPageBehavior page, UiLayer layer);

    /// <summary>
    ///     隐藏指定层级的UI。
    /// </summary>
    /// <param name="handle">UI句柄，用于标识具体的UI实例。</param>
    /// <param name="layer">指定UI所在的层级。</param>
    /// <param name="destroy">是否销毁UI对象，默认为false，表示仅隐藏而不销毁。</param>
    void Hide(UiHandle handle, UiLayer layer, bool destroy = false);

    /// <summary>
    ///     恢复指定层级的UI显示。
    /// </summary>
    /// <param name="handle">UI句柄，用于标识具体的UI实例。</param>
    /// <param name="layer">指定UI所在的层级。</param>
    void Resume(UiHandle handle, UiLayer layer);

    /// <summary>
    ///     清空指定层级的所有UI。
    /// </summary>
    /// <param name="layer">要清空的UI层级。</param>
    /// <param name="destroy">是否销毁UI实例。如果为true，则会销毁UI实例；否则仅从层级中移除。</param>
    void ClearLayer(UiLayer layer, bool destroy = false);

    /// <summary>
    ///     从指定层级获取UI实例。
    /// </summary>
    /// <param name="handle">UI句柄，用于标识具体的UI实例。</param>
    /// <param name="layer">要查询的UI层级。</param>
    /// <returns>返回与指定键关联的UI行为接口实例；如果未找到则返回null。</returns>
    UiHandle? GetFromLayer(UiHandle handle, UiLayer layer);

    /// <summary>
    ///     从指定层级获取所有与给定UI键关联的UI实例。
    /// </summary>
    /// <param name="uiKey">用于标识UI实例的键。</param>
    /// <param name="layer">要查询的UI层级。</param>
    /// <returns>返回一个只读列表，包含所有与指定键和层级关联的UI句柄；如果未找到则返回空列表。</returns>
    IReadOnlyList<UiHandle> GetAllFromLayer(string uiKey, UiLayer layer);

    /// <summary>
    ///     判断指定层级是否存在可见UI。
    /// </summary>
    /// <param name="handle">UI句柄，用于标识具体的UI实例。</param>
    /// <param name="layer">要检查的UI层级。</param>
    /// <returns>如果在指定层级中存在可见的UI，则返回true；否则返回false。</returns>
    bool HasVisibleInLayer(UiHandle handle, UiLayer layer);

    /// <summary>
    ///     根据UI键隐藏指定层级中的UI。
    /// </summary>
    /// <param name="uiKey">UI的唯一标识键。</param>
    /// <param name="layer">要操作的UI层级。</param>
    /// <param name="destroy">是否销毁UI实例，默认为false。</param>
    /// <param name="hideAll">是否隐藏所有匹配的UI实例，默认为false。</param>
    void HideByKey(string uiKey, UiLayer layer, bool destroy = false, bool hideAll = false);

    /// <summary>
    ///     查询当前对指定 UI 语义动作拥有最高优先级捕获权的页面。
    /// </summary>
    /// <param name="action">要查询的动作。</param>
    /// <returns>动作所有者；如果当前没有页面声明捕获该动作则返回 <see langword="null" />。</returns>
    IUiPageBehavior? GetUiActionOwner(UiInputAction action);

    /// <summary>
    ///     尝试把语义动作分发给当前拥有该动作捕获权的页面。
    /// </summary>
    /// <param name="action">当前动作。</param>
    /// <returns>如果该动作已被某个页面捕获并完成分发，则返回 <see langword="true" />。</returns>
    bool TryDispatchUiAction(UiInputAction action);

    /// <summary>
    ///     尝试把语义动作分发给当前拥有该动作捕获权的页面。
    /// </summary>
    /// <param name="action">当前动作。</param>
    /// <returns>如果该动作已被某个页面捕获并完成分发，则返回 <see langword="true" />。</returns>
    [Obsolete(
        "Use TryDispatchUiAction(UiInputAction action) to emphasize dispatch semantics instead of handler success.")]
    bool TryHandleUiAction(UiInputAction action);

    /// <summary>
    ///     判断当前可见 UI 是否阻断 World 指针输入。
    /// </summary>
    /// <returns>如果 World 指针输入应被阻断则返回 <see langword="true" />。</returns>
    bool BlocksWorldPointerInput();

    /// <summary>
    ///     判断当前可见 UI 是否阻断 World 语义动作输入。
    /// </summary>
    /// <returns>如果 World 动作输入应被阻断则返回 <see langword="true" />。</returns>
    bool BlocksWorldActionInput();

    #endregion
}
