using System;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.extensions;
using GFramework.Core.system;
using GFramework.Game.Abstractions.assets;
using Godot;

namespace GFramework.Godot.assets;

/// <summary>
///     资源工厂系统抽象基类，用于统一管理各类资源的创建与预加载逻辑。
///     提供注册场景和资源的方法，并通过依赖的资源加载系统和资产目录系统完成实际资源的获取与构造。
/// </summary>
public abstract class AbstractResourceFactorySystem : AbstractSystem, IResourceFactorySystem
{
    private IAssetCatalogSystem? _assetCatalogSystem;
    private ResourceFactory.Registry? _registry;
    private IResourceLoadSystem? _resourceLoadSystem;


    /// <summary>
    ///     在架构阶段发生变化时执行相应的处理逻辑。
    /// </summary>
    /// <param name="phase">当前的架构阶段</param>
    public override void OnArchitecturePhase(ArchitecturePhase phase)
    {
        if (phase == ArchitecturePhase.Ready)
        {
            // 在架构准备就绪阶段注册资源并预加载所有资源
            RegisterResources();
            _registry!.PreloadAll();
        }
    }


    /// <summary>
    ///     根据指定的键获取资源工厂函数。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="key">资源键</param>
    /// <returns>返回创建指定类型资源的工厂函数</returns>
    public Func<T> GetFactory<T>(string key)
    {
        return _registry!.ResolveFactory<T>(key);
    }


    /// <summary>
    ///     根据资产目录映射信息获取资源工厂函数。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="mapping">资产目录映射信息</param>
    /// <returns>返回创建指定类型资源的工厂函数</returns>
    public Func<T> GetFactory<T>(AssetCatalog.AssetCatalogMapping mapping)
    {
        return _registry!.ResolveFactory<T>(mapping.Key);
    }

    /// <summary>
    ///     系统初始化方法，在系统启动时执行一次。
    ///     初始化资源注册表，并获取依赖的资源加载系统和资产目录系统。
    ///     最后执行所有已注册资源的预加载操作。
    /// </summary>
    protected override void OnInit()
    {
        _registry = new ResourceFactory.Registry();
        _resourceLoadSystem = this.GetSystem<IResourceLoadSystem>();
        _assetCatalogSystem = this.GetSystem<IAssetCatalogSystem>();
    }

    /// <summary>
    ///     注册系统所需的各种资源类型。由子类实现具体注册逻辑。
    /// </summary>
    protected abstract void RegisterResources();


    #region Register Helpers（声明式）

    /// <summary>
    ///     注册场景单元到资源管理系统中
    /// </summary>
    /// <typeparam name="T">场景单元类型，必须继承自Node</typeparam>
    /// <param name="sceneUnitKey">场景单元键值，用于标识特定的场景单元资源</param>
    /// <param name="preload">是否预加载该资源，默认为false</param>
    public void RegisterSceneUnit<T>(
        string sceneUnitKey,
        bool preload = false)
        where T : Node
    {
        var id = _assetCatalogSystem!.GetSceneUnit(sceneUnitKey);

        _registry!.Register(
            sceneUnitKey,
            _resourceLoadSystem!.GetOrRegisterGameUnitFactory<T>(id),
            preload
        );
    }


    /// <summary>
    ///     注册场景页面到资源管理系统中
    /// </summary>
    /// <typeparam name="T">场景页面类型，必须继承自Node</typeparam>
    /// <param name="scenePageKey">场景页面键值，用于标识特定的场景页面资源</param>
    /// <param name="preload">是否预加载该资源，默认为false</param>
    public void RegisterScenePage<T>(
        string scenePageKey,
        bool preload = false)
        where T : Node
    {
        var id = _assetCatalogSystem!.GetScenePage(scenePageKey);

        _registry!.Register(
            scenePageKey,
            _resourceLoadSystem!.GetOrRegisterTemplateFactory<T>(id),
            preload
        );
    }

    /// <summary>
    ///     注册通用资产资源到资源管理系统中
    /// </summary>
    /// <typeparam name="T">资产类型，必须继承自Resource</typeparam>
    /// <param name="assetKey">资产键值，用于标识特定的资产资源</param>
    /// <param name="preload">是否预加载该资源，默认为false</param>
    public void RegisterAsset<T>(
        string assetKey,
        bool preload = false)
        where T : Resource
    {
        var id = _assetCatalogSystem!.GetAsset(assetKey);

        _registry!.Register(
            assetKey,
            _resourceLoadSystem!.GetOrRegisterAssetFactory<T>(id),
            preload
        );
    }

    #endregion
}