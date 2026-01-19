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
    void Push(string uiKey, IUiPageEnterParam? param = null, UiTransitionPolicy policy = UiTransitionPolicy.Exclusive);


    /// <summary>
    /// 弹出路由栈顶的UI界面，返回到上一个界面
    /// </summary>
    /// <param name="policy">界面弹出策略，默认为Destroy（销毁）</param>
    void Pop(UiPopPolicy policy = UiPopPolicy.Destroy);

    /// <summary>
    /// 替换当前UI界面为指定的新界面
    /// </summary>
    /// <param name="uiKey">新UI界面的唯一标识符</param>
    /// <param name="param">进入界面的参数，可为空</param>
    /// <param name="popPolicy">界面弹出策略，默认为销毁当前界面</param>
    /// <param name="pushPolicy">界面过渡策略，默认为独占模式</param>
    void Replace(
        string uiKey,
        IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive
    );


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
    string Peek();

    /// <summary>
    /// 判断指定UI是否为当前栈顶UI
    /// </summary>
    bool IsTop(string uiKey);

    /// <summary>
    /// 判断指定UI是否存在于UI栈中
    /// </summary>
    bool Contains(string uiKey);
}