namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI页面行为接口，定义了UI页面的生命周期方法和状态管理
/// </summary>
public interface IUiPageBehavior
{
    /// <summary>
    /// 获取页面视图对象
    /// </summary>
    /// <returns>页面视图实例</returns>
    object View { get; }

    /// <summary>
    /// 获取页面是否处于活动状态
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// 页面进入时调用的方法
    /// </summary>
    /// <param name="param">页面进入时传递的参数，可为空</param>
    void OnEnter(IUiPageEnterParam? param);

    /// <summary>
    /// 页面退出时调用的方法
    /// </summary>
    void OnExit();

    /// <summary>
    /// 页面暂停时调用的方法
    /// </summary>
    void OnPause();

    /// <summary>
    /// 页面恢复时调用的方法
    /// </summary>
    void OnResume();

    /// <summary>
    /// 页面被覆盖时调用（不销毁）
    /// </summary>
    void OnHide();

    /// <summary>
    /// 页面重新显示时调用的方法
    /// </summary>
    void OnShow();
}