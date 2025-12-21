using GFramework.Core.architecture;
using GFramework.Core.events;
using GFramework.Core.extensions;
using GFramework.Core.system;
using GFramework.Game.assets;
using Godot;

namespace GFramework.Godot.system;

/// <summary>
/// 资源工厂系统抽象基类，用于统一管理各类资源的创建与预加载逻辑。
/// 提供注册场景和资源的方法，并通过依赖的资源加载系统和资产目录系统完成实际资源的获取与构造。
/// </summary>
public abstract class AbstractResourceFactorySystem : AbstractSystem, IResourceFactorySystem, IArchitectureLifecycle
{
    private ResourceFactory.Registry? _registry;
    private IResourceLoadSystem? _resourceLoadSystem;
    private IAssetCatalogSystem? _assetCatalogSystem;

    /// <summary>
    /// 系统初始化方法，在系统启动时执行一次。
    /// 初始化资源注册表，并获取依赖的资源加载系统和资产目录系统。
    /// 最后执行所有已注册资源的预加载操作。
    /// </summary>
    protected override void OnInit()
    {
        _registry = new ResourceFactory.Registry();
        _resourceLoadSystem = this.GetSystem<IResourceLoadSystem>();
        _assetCatalogSystem = this.GetSystem<IAssetCatalogSystem>();
    }

    /// <summary>
    /// 架构阶段回调，在架构就绪时注册和预加载资源
    /// </summary>
    /// <param name="phase">当前架构阶段</param>
    /// <param name="arch">架构实例</param>
    public void OnPhase(ArchitecturePhase phase, IArchitecture arch)
    {
        if (phase == ArchitecturePhase.Ready)
        {
            // 注册资源
            RegisterResources();
            // 预加载所有资源
            _registry!.PreloadAll();
        }
    }

    /// <summary>
    /// 注册系统所需的各种资源类型。由子类实现具体注册逻辑。
    /// </summary>
    protected abstract void RegisterResources();


    /// <summary>
    /// 根据指定的键获取资源工厂函数。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="key">资源键</param>
    /// <returns>返回创建指定类型资源的工厂函数</returns>
    public Func<T> GetFactory<T>(string key) => _registry!.ResolveFactory<T>(key);

    /// <summary>
    /// 根据资产目录映射信息获取资源工厂函数。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="mapping">资产目录映射信息</param>
    /// <returns>返回创建指定类型资源的工厂函数</returns>
    public Func<T> GetFactory<T>(AssetCatalog.AssetCatalogMapping mapping) => _registry!.ResolveFactory<T>(mapping.Key);


    #region Register Helpers（声明式）

    /// <summary>
    /// 注册游戏单位资源到资源管理系统中
    /// </summary>
    /// <typeparam name="T">游戏单位类型，必须继承自Node</typeparam>
    /// <param name="sceneKey">场景键值，用于标识特定的游戏单位资源</param>
    /// <param name="preload">是否预加载该资源，默认为false</param>
    public void RegisterGameUnit<T>(
        string sceneKey,
        bool preload = false)
        where T : Node
    {
        var id = _assetCatalogSystem!.GetSceneUnit(sceneKey);

        _registry!.Register(
            sceneKey,
            _resourceLoadSystem!.GetOrRegisterGameUnitFactory<T>(id),
            preload
        );
    }

    /// <summary>
    /// 注册模板资源到资源管理系统中
    /// </summary>
    /// <typeparam name="T">模板类型，必须继承自Node</typeparam>
    /// <param name="templateKey">模板键值，用于标识特定的模板资源</param>
    /// <param name="preload">是否预加载该资源，默认为false</param>
    public void RegisterTemplate<T>(
        string templateKey,
        bool preload = false)
        where T : Node
    {
        var id = _assetCatalogSystem!.GetScenePage(templateKey);

        _registry!.Register(
            templateKey,
            _resourceLoadSystem!.GetOrRegisterTemplateFactory<T>(id),
            preload
        );
    }

    /// <summary>
    /// 注册通用资产资源到资源管理系统中
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