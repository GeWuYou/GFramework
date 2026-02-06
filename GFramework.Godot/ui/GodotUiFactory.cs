using GFramework.Core.Abstractions.logging;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.utility;
using GFramework.Game.Abstractions.ui;

namespace GFramework.Godot.ui;

/// <summary>
///     Godot UI工厂类，用于创建UI页面实例。
///     继承自AbstractContextUtility并实现IUiFactory接口。
/// </summary>
public class GodotUiFactory : AbstractContextUtility, IUiFactory
{
    /// <summary>
    ///     日志记录器，用于记录调试信息。
    /// </summary>
    private static readonly ILogger Log =
        LoggerFactoryResolver.Provider.CreateLogger("GodotUiFactory");

    /// <summary>
    ///     UI注册表，用于管理UI场景资源。
    /// </summary>
    private IGodotUiRegistry _registry = null!;

    /// <summary>
    ///     根据指定的UI键创建UI页面实例。
    /// </summary>
    /// <param name="uiKey">UI页面的唯一标识符。</param>
    /// <returns>返回创建的UI页面行为实例。</returns>
    /// <exception cref="InvalidCastException">
    ///     当UI场景未实现IUiPageBehaviorProvider接口时抛出异常。
    /// </exception>
    public IUiPageBehavior Create(string uiKey)
    {
        // 从注册表中获取指定UI键对应的场景
        var scene = _registry.Get(uiKey);

        // 实例化场景节点
        var node = scene.Instantiate();

        // 检查节点是否实现了IUiPageBehaviorProvider接口
        if (node is not IUiPageBehaviorProvider provider)
            throw new InvalidCastException(
                $"UI scene {uiKey} must implement IUiPageBehaviorProvider");

        // 获取页面行为实例
        var page = provider.GetPage();

        // 记录调试日志
        Log.Debug("Created UI instance: {0}", uiKey);
        return page;
    }

    /// <summary>
    ///     初始化方法，在对象初始化时调用。
    ///     获取并设置UI注册表实例。
    /// </summary>
    protected override void OnInit()
    {
        _registry = this.GetUtility<IGodotUiRegistry>()!;
    }
}