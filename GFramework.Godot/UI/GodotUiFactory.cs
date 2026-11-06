// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Extensions;
using GFramework.Core.Utility;
using GFramework.Game.Abstractions.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Godot.UI;

/// <summary>
///     Godot UI工厂类，用于创建UI页面实例。
///     继承自AbstractContextUtility并实现IUiFactory接口。
/// </summary>
public class GodotUiFactory : AbstractContextUtility, IUiFactory
{
    /// <summary>
    ///     构造注入的标准日志记录器；未注入时为 null。
    /// </summary>
    private readonly ILogger<GodotUiFactory>? _injectedLogger;

    /// <summary>
    ///     获取当前生效的日志记录器：优先构造注入，其次回退到上下文提供的记录器，最后退化为空记录器。
    /// </summary>
    private ILogger Log =>
        (ILogger?)_injectedLogger
        ?? (Logger is { } contextLogger ? contextLogger : NullLogger<GodotUiFactory>.Instance);

    /// <summary>
    ///     UI注册表，用于管理UI场景资源。
    /// </summary>
    private IGodotUiRegistry _registry = null!;

    /// <summary>
    ///     创建Godot UI工厂实例。
    /// </summary>
    /// <param name="logger">标准日志记录器；依赖注入容器会自动提供，手动创建时可省略。</param>
    public GodotUiFactory(ILogger<GodotUiFactory>? logger = null)
    {
        _injectedLogger = logger;
    }

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
        Log.LogDebug("Created UI instance: {UiKey}", uiKey);
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