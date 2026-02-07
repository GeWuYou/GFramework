using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.ui;
using GFramework.Godot.extensions;
using Godot;

namespace GFramework.Godot.ui;

/// <summary>
///     控制 UI 页面行为的泛型行为类，
///     支持所有继承自 CanvasItem 的节点
/// </summary>
/// <typeparam name="T">CanvasItem 类型的视图节点</typeparam>
public class CanvasItemUiPageBehavior<T>(T owner, string key) : IUiPageBehavior
    where T : CanvasItem
{
    private readonly IUiPage? _page = owner as IUiPage;

    /// <summary>
    ///     获取当前UI层的类型。
    ///     返回值表示当前页面所属的UI层级，此处固定返回UiLayer.Page。
    /// </summary>
    public UiLayer Layer => UiLayer.Page;

    /// <summary>
    ///     判断当前Page是否允许重入。
    ///     返回值为false，表示该Page不支持重入操作。
    /// </summary>
    public bool IsReentrant => false;


    /// <summary>
    ///     获取页面视图对象
    /// </summary>
    /// <returns>返回与当前实例关联的视图对象</returns>
    public object View => owner;

    /// <summary>
    ///     获取当前实例的键值标识符
    /// </summary>
    /// <returns>返回用于标识当前实例的键字符串</returns>
    public string Key => key;


    /// <summary>
    ///     获取页面是否存活状态
    /// </summary>
    /// <returns>如果页面节点有效则返回true，否则返回false</returns>
    public bool IsAlive => owner.IsValidNode();

    /// <summary>
    ///     获取页面是否可见状态
    /// </summary>
    /// <returns>如果页面可见则返回true，否则返回false</returns>
    public bool IsVisible => owner.Visible;

    /// <summary>
    ///     页面进入时调用
    /// </summary>
    /// <param name="param">页面进入参数</param>
    public void OnEnter(IUiPageEnterParam? param)
    {
        _page?.OnEnter(param);
    }

    /// <summary>
    ///     页面退出时调用
    /// </summary>
    public void OnExit()
    {
        _page?.OnExit();
        owner.QueueFreeX();
    }

    /// <summary>
    ///     页面暂停时调用
    /// </summary>
    public void OnPause()
    {
        _page?.OnPause();

        // 暂停节点的处理、物理处理和输入处理
        if (!BlocksInput) return;
        owner.SetProcess(false);
        owner.SetPhysicsProcess(false);
        owner.SetProcessInput(false);
    }

    /// <summary>
    ///     页面恢复时调用
    /// </summary>
    public void OnResume()
    {
        if (owner.IsInvalidNode())
        {
            return;
        }

        _page?.OnResume();

        // 恢复节点的处理、物理处理和输入处理
        owner.SetProcess(true);
        owner.SetPhysicsProcess(true);
        owner.SetProcessInput(true);
    }

    /// <summary>
    ///     页面隐藏时调用
    /// </summary>
    public void OnHide()
    {
        _page?.OnHide();
        owner.Hide();
    }

    /// <summary>
    ///     页面显示时调用
    /// </summary>
    public void OnShow()
    {
        _page?.OnShow();
        owner.Show();
        OnResume();
    }

    /// <summary>
    ///     获取或设置页面是否为模态对话框
    /// </summary>
    public bool IsModal { get; set; }

    /// <summary>
    ///     获取或设置页面是否阻止输入
    /// </summary>
    public bool BlocksInput { get; set; } = true;
}