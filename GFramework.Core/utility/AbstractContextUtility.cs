using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.utility;
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
    void IContextUtility.Init()
    {
        // 获取上下文中的日志记录器
        Logger = Context.LoggerFactory.GetLogger(nameof(AbstractContextUtility));
        Logger.Debug($"Initializing Context Utility: {GetType().Name}");

        // 执行子类实现的初始化逻辑
        OnInit();

        // 记录初始化完成信息
        Logger.Info($"Context Utility initialized: {GetType().Name}");
    }

    /// <summary>
    ///     抽象初始化方法，由子类实现具体的初始化逻辑
    /// </summary>
    protected abstract void OnInit();
}