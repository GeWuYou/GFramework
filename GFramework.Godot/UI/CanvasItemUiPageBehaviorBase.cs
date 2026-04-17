// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.UI;

namespace GFramework.Godot.UI;

/// <summary>
///     UI 页面行为基类，封装通用的生命周期管理逻辑。
///     提供对 CanvasItem 类型视图节点的统一管理，包括显示、隐藏、进入、退出等操作。
/// </summary>
/// <typeparam name="T">CanvasItem 类型的视图节点。</typeparam>
public abstract class CanvasItemUiPageBehaviorBase<T> : IUiPageBehavior
    where T : CanvasItem
{
    /// <summary>
    ///     UI 的唯一标识键。
    /// </summary>
    private readonly string _key;

    /// <summary>
    ///     IUiPage 接口引用（如果视图实现了该接口）。
    /// </summary>
    private readonly IUiPage? _page;

    /// <summary>
    ///     视图可选提供的交互配置提供者。
    /// </summary>
    private readonly IUiInteractionProfileProvider? _profileProvider;

    /// <summary>
    ///     视图可选提供的 UI 语义动作处理器。
    /// </summary>
    private readonly IUiActionHandler? _uiActionHandler;

    /// <summary>
    ///     视图节点的所有者实例。
    /// </summary>
    protected readonly T Owner;

    /// <summary>
    ///     初始化 CanvasItemUiPageBehaviorBase 实例。
    /// </summary>
    /// <param name="owner">视图节点的所有者实例。</param>
    /// <param name="key">UI 的唯一标识键。</param>
    protected CanvasItemUiPageBehaviorBase(T owner, string key)
    {
        Owner = owner;
        _key = key;
        _page = owner as IUiPage;
        _profileProvider = owner as IUiInteractionProfileProvider;
        _uiActionHandler = owner as IUiActionHandler;
    }

    #region 抽象属性 - 子类必须实现

    /// <summary>
    ///     获取或设置当前UI句柄。
    /// </summary>
    /// <value>
    ///     表示当前UI句柄的可空类型 <see cref="UiHandle"/>。
    /// </value>
    /// <remarks>
    ///     此属性允许获取或设置与当前上下文关联的UI句柄。若未设置，则其值为 null。不可重入的ui句柄通常为null
    /// </remarks>
    public UiHandle? Handle { get; set; }

    /// <summary>
    ///     获取 UI 所属的层级。
    ///     该属性由子类实现并指定具体的层级值。
    ///     层级用于确定 UI 元素在界面中的显示顺序和逻辑分组。
    /// </summary>
    public abstract UiLayer Layer { get; }

    /// <summary>
    ///     获取是否支持重入。
    ///     由子类指定具体值。
    /// </summary>
    public abstract bool IsReentrant { get; }

    /// <summary>
    ///     获取是否为模态窗口。
    ///     由子类指定默认值。
    /// </summary>
    public abstract bool IsModal { get; }

    /// <summary>
    ///     获取是否阻止下层输入。
    ///     由子类指定默认值。
    /// </summary>
    public abstract bool BlocksInput { get; }

    #endregion

    #region 基础属性

    /// <summary>
    ///     获取视图节点实例。
    /// </summary>
    public object View => Owner;

    /// <summary>
    ///     获取 UI 的唯一标识键。
    /// </summary>
    public string Key => _key;

    /// <summary>
    ///     获取视图节点是否有效。
    /// </summary>
    public bool IsAlive => Owner.IsValidNode();

    /// <summary>
    ///     获取视图节点是否可见。
    /// </summary>
    public bool IsVisible => Owner.Visible;

    /// <summary>
    ///     获取页面当前的交互配置。
    ///     若页面未提供自定义配置，则回退到层级默认值。
    /// </summary>
    public UiInteractionProfile InteractionProfile => _profileProvider?.GetUiInteractionProfile(Layer)
                                                      ?? UiInteractionProfiles.CreateDefault(Layer);

    #endregion

    #region 生命周期管理

    /// <summary>
    ///     当 UI 进入时调用。
    ///     默认调用 IUiPage 接口的 OnEnter 方法（如果存在）。
    /// </summary>
    /// <param name="param">进入参数。</param>
    public virtual void OnEnter(IUiPageEnterParam? param)
    {
        _page?.OnEnter(param);
    }

    /// <summary>
    ///     当 UI 退出时调用。
    ///     默认调用 IUiPage 接口的 OnExit 方法（如果存在），并释放视图节点。
    /// </summary>
    public virtual void OnExit()
    {
        _page?.OnExit();
        Owner.QueueFreeX();
    }

    /// <summary>
    ///     当 UI 暂停时调用。
    ///     默认调用 IUiPage 接口的 OnPause 方法（如果存在），并根据 BlocksInput 决定是否暂停处理逻辑。
    /// </summary>
    public virtual void OnPause()
    {
        _page?.OnPause();

        // 只有阻止输入的 UI 才需要暂停处理
        if (!BlocksInput) return;

        Owner.SetProcess(false);
        Owner.SetPhysicsProcess(false);
        Owner.SetProcessInput(false);
        Owner.SetProcessUnhandledInput(false);
        Owner.SetProcessUnhandledKeyInput(false);
    }

    /// <summary>
    ///     当 UI 恢复时调用。
    ///     默认调用 IUiPage 接口的 OnResume 方法（如果存在），并恢复处理逻辑。
    /// </summary>
    public virtual void OnResume()
    {
        if (Owner.IsInvalidNode())
            return;

        _page?.OnResume();

        ApplyPauseAwareProcessingMode();

        // 恢复处理
        Owner.SetProcess(true);
        Owner.SetPhysicsProcess(true);
        Owner.SetProcessInput(true);
        Owner.SetProcessUnhandledInput(true);
        Owner.SetProcessUnhandledKeyInput(true);
    }

    /// <summary>
    ///     当 UI 隐藏时调用。
    ///     默认调用 IUiPage 接口的 OnHide 方法（如果存在），并隐藏视图节点。
    /// </summary>
    public virtual void OnHide()
    {
        _page?.OnHide();
        Owner.Hide();
    }

    /// <summary>
    ///     当 UI 显示时调用。
    ///     默认调用 IUiPage 接口的 OnShow 方法（如果存在），并显示视图节点，同时触发恢复逻辑。
    /// </summary>
    public virtual void OnShow()
    {
        _page?.OnShow();
        Owner.Show();
        OnResume();
    }

    /// <summary>
    ///     尝试处理一个路由仲裁后的 UI 语义动作。
    /// </summary>
    /// <param name="action">当前动作。</param>
    /// <returns>如果视图显式处理了该动作则返回 <see langword="true" />。</returns>
    public virtual bool TryHandleUiAction(UiInputAction action)
    {
        return _uiActionHandler?.TryHandleUiAction(action) ?? false;
    }

    /// <summary>
    ///     根据交互配置调整节点在暂停态下的处理模式。
    /// </summary>
    private void ApplyPauseAwareProcessingMode()
    {
        Owner.ProcessMode = InteractionProfile.ContinueProcessingWhenPaused
            ? Node.ProcessModeEnum.Always
            : Node.ProcessModeEnum.Pausable;
    }

    #endregion
}
