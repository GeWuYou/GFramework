using GFramework.Game.Abstractions.enums;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
///     UI页面行为接口，定义了UI页面的生命周期方法和状态管理
/// </summary>
public interface IUiPageBehavior
{
    /// <summary>
    ///     获取当前UI层的实例。
    /// </summary>
    /// <remarks>
    ///     此属性用于访问与当前上下文关联的UI层对象。
    /// </remarks>
    UiLayer Layer { get; }

    /// <summary>
    ///     获取一个布尔值，指示当前操作是否为重入操作。
    /// </summary>
    /// <remarks>
    ///     重入操作通常指在同一个执行上下文中多次调用相同的方法或逻辑。
    ///     此属性可用于检测并避免重复执行可能导致异常或不一致状态的操作。
    /// </remarks>
    bool IsReentrant { get; }


    /// <summary>
    ///     获取页面视图对象。
    /// </summary>
    /// <returns>页面视图实例。</returns>
    object View { get; }


    /// <summary>
    ///     获取键值
    /// </summary>
    /// <value>返回当前对象的键标识符</value>
    string Key { get; }


    /// <summary>
    ///     获取页面是否处于活动状态
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    ///     获取页面是否可见
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    ///     获取页面是否为模态页面
    /// </summary>
    bool IsModal { get; }

    /// <summary>
    ///     获取页面是否阻断下层交互
    /// </summary>
    bool BlocksInput { get; }

    /// <summary>
    ///     页面进入时调用的方法
    /// </summary>
    /// <param name="param">页面进入时传递的参数，可为空</param>
    void OnEnter(IUiPageEnterParam? param);

    /// <summary>
    ///     页面退出时调用的方法
    /// </summary>
    void OnExit();

    /// <summary>
    ///     页面暂停时调用的方法
    /// </summary>
    void OnPause();

    /// <summary>
    ///     页面恢复时调用的方法
    /// </summary>
    void OnResume();

    /// <summary>
    ///     页面被覆盖时调用（不销毁）
    /// </summary>
    void OnHide();

    /// <summary>
    ///     页面重新显示时调用的方法
    /// </summary>
    void OnShow();
}