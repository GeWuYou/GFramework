using GFramework.Core.events;
using GFramework.Core.extensions;
using GFramework.Core.system;
using Godot;

namespace GFramework.Core.Godot.system;


/// <summary>
/// 资源工厂系统抽象基类，用于统一管理各类资源的创建与预加载逻辑。
/// 提供注册场景和资源的方法，并通过依赖的资源加载系统和资产目录系统完成实际资源的获取与构造。
/// </summary>
public abstract class AbstractResourceFactorySystem : AbstractSystem, IResourceFactorySystem
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
        // 注册资源
        RegisterResources();
        // 监听架构初始化事件
        this.RegisterEvent<ArchitectureEvents.ArchitectureInitializedEvent>(_ =>
        {
            // 预加载所有资源
            _registry.PreloadAll();
        });
    }

    /// <summary>
    /// 注册系统所需的各种资源类型。由子类实现具体注册逻辑。
    /// </summary>
    protected abstract void RegisterResources();

    /// <summary>
    /// 获取指定类型的资源工厂函数。
    /// </summary>
    /// <typeparam name="T">要获取工厂的资源类型</typeparam>
    /// <returns>返回创建指定类型资源的工厂函数</returns>
    public Func<T> Get<T>() => _registry!.Resolve<T>();
    
    #region Register Helpers（声明式）

    /// <summary>
    /// 注册场景资源工厂。
    /// 根据场景键名获取场景ID，并将场景加载工厂注册到注册表中。
    /// </summary>
    /// <typeparam name="T">场景节点类型，必须继承自Node</typeparam>
    /// <param name="sceneKey">场景在资产目录中的键名</param>
    /// <param name="preload">是否需要预加载该场景资源</param>
    protected void RegisterScene<T>(
        string sceneKey,
        bool preload = false)
        where T : Node
    {
        var id = _assetCatalogSystem!.GetScene(sceneKey);

        _registry!.Register(
            _resourceLoadSystem!.GetOrRegisterSceneFactory<T>(id),
            preload
        );
    }

    /// <summary>
    /// 注册普通资源工厂。
    /// 根据资源键名获取资源ID，并将资源加载工厂注册到注册表中。
    /// </summary>
    /// <typeparam name="T">资源类型，必须继承自Resource</typeparam>
    /// <param name="resourceKey">资源在资产目录中的键名</param>
    /// <param name="duplicate">是否需要复制资源实例</param>
    /// <param name="preload">是否需要预加载该资源</param>
    protected void RegisterResource<T>(
        string resourceKey,
        bool duplicate = false,
        bool preload = false)
        where T : Resource
    {
        var id = _assetCatalogSystem!.GetResource(resourceKey);

        _registry!.Register(
            _resourceLoadSystem!.GetOrRegisterResourceFactory<T>(id, duplicate),
            preload
        );
    }

    #endregion
}
