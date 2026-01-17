using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.utility;
using GFramework.Core.logging;
using GFramework.Core.rule;

namespace GFramework.Core.utility;

/// <summary>
///     抽象上下文工具类，提供上下文相关的通用功能实现
///     继承自ContextAwareBase并实现IContextUtility接口
/// </summary>
public abstract class AbstractContextUtility : ContextAwareBase, IContextUtility
{
    protected ILogger Logger = null !;

    /// <summary>
    ///     初始化上下文工具类
    /// </summary>
    public void Init()
    {
        var name = GetType().Name;
        // 获取上下文中的日志记录器
        Logger = LoggerFactoryResolver.Provider.CreateLogger(name);
        Logger.Debug($"Initializing Context Utility: {name}");

        // 执行子类实现的初始化逻辑
        OnInit();

        // 记录初始化完成信息
        Logger.Info($"Context Utility initialized: {name}");
    }

    /// <summary>
    ///     销毁上下文工具类实例
    /// </summary>
    public void Destroy()
    {
        var name = GetType().Name;
        Logger.Debug($"Destroying Context Utility: {name}");
        OnDestroy();
        Logger.Info($"Context Utility destroyed: {name}");
    }

    /// <summary>
    ///     抽象初始化方法，由子类实现具体的初始化逻辑
    /// </summary>
    protected abstract void OnInit();

    /// <summary>
    ///     虚拟销毁方法，可由子类重写以实现自定义销毁逻辑
    /// </summary>
    protected virtual void OnDestroy()
    {
    }
}