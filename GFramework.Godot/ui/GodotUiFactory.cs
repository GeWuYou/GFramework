using GFramework.Core.extensions;
using GFramework.Core.utility;
using GFramework.Game.Abstractions.ui;

namespace GFramework.Godot.ui;

/// <summary>
/// Godot UI工厂类，用于创建UI页面实例
/// 继承自AbstractContextUtility并实现IUiFactory接口
/// </summary>
public class GodotUiFactory : AbstractContextUtility, IUiFactory
{
    /// <summary>
    /// UI注册表，用于存储和获取PackedScene类型的UI资源
    /// </summary>
    private IGodotUiRegistry _registry = null!;

    /// <summary>
    /// 根据指定的UI键创建UI页面实例
    /// </summary>
    /// <param name="uiKey">UI资源的唯一标识键</param>
    /// <returns>实现IUiPage接口的UI页面实例</returns>
    /// <exception cref="InvalidCastException">当UI场景没有继承IUiPage接口时抛出</exception>
    public IUiPageBehavior Create(string uiKey)
    {
        // 从注册表中获取对应的场景资源
        var scene = _registry.Get(uiKey);

        // 实例化场景节点
        var node = scene.Instantiate();

        // 验证节点是否实现了IUiPageProvider接口
        if (node is not IUiPageBehaviorProvider provider)
            throw new InvalidCastException(
                $"UI scene {uiKey} must implement IUiPageBehaviorProvider");

        // 获取并返回页面行为实例
        return provider.GetPage();
    }

    /// <summary>
    /// 初始化方法，获取UI注册表实例
    /// </summary>
    protected override void OnInit()
    {
        _registry = this.GetUtility<IGodotUiRegistry>()!;
    }
}