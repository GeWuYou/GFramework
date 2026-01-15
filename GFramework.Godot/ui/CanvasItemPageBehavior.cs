using GFramework.Game.Abstractions.ui;
using GFramework.Godot.extensions;
using Godot;

namespace GFramework.Godot.ui;

/// <summary>
/// 控制 UI 页面行为的泛型行为类，
/// 支持所有继承自 CanvasItem 的节点
/// </summary>
/// <typeparam name="T">CanvasItem 类型的视图节点</typeparam>
public class CanvasItemPageBehavior<T>(T owner) : IPageBehavior
    where T : CanvasItem
{
    private readonly IUiPage? _page = owner as IUiPage;

    /// <summary>
    /// 页面视图对象
    /// </summary>
    public object View => owner;

    /// <summary>
    /// 获取页面是否存活状态
    /// </summary>
    public bool IsAlive => owner.IsValidNode();

    /// <summary>
    /// 页面进入时调用
    /// </summary>
    /// <param name="param">页面进入参数</param>
    public void OnEnter(IUiPageEnterParam? param)
    {
        _page?.OnEnter(param);
    }

    /// <summary>
    /// 页面退出时调用
    /// </summary>
    public void OnExit()
    {
        _page?.OnExit();
        owner.QueueFreeX();
    }

    /// <summary>
    /// 页面暂停时调用
    /// </summary>
    public void OnPause()
    {
        _page?.OnPause();

        // 暂停节点的处理、物理处理和输入处理
        owner.SetProcess(false);
        owner.SetPhysicsProcess(false);
        owner.SetProcessInput(false);
    }

    /// <summary>
    /// 页面恢复时调用
    /// </summary>
    public void OnResume()
    {
        _page?.OnResume();

        // 恢复节点的处理、物理处理和输入处理
        owner.SetProcess(true);
        owner.SetPhysicsProcess(true);
        owner.SetProcessInput(true);
    }

    /// <summary>
    /// 页面隐藏时调用
    /// </summary>
    public void OnHide()
    {
        _page?.OnHide();
        owner.Hide();
    }

    /// <summary>
    /// 页面显示时调用
    /// </summary>
    public void OnShow()
    {
        _page?.OnShow();
        owner.Show();
        OnResume();
    }
}