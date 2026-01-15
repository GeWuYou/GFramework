namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI页面生命周期接口
/// 定义了UI页面的各种状态转换方法，用于管理UI页面的进入、退出、暂停、恢复、显示和隐藏等生命周期事件
/// </summary>
public interface IUiPage
{
    /// <summary>
    /// 页面进入时调用的方法
    /// </summary>
    /// <param name="param">页面进入参数，可能为空</param>
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
    /// 页面显示时调用的方法
    /// </summary>
    void OnShow();

    /// <summary>
    /// 页面隐藏时调用的方法
    /// </summary>
    void OnHide();
}