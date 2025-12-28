using GFramework.Core.system;
using GFramework.Game.assets;
using GFramework.Godot.Abstractions.assets;
using Godot;

namespace GFramework.Godot.system;

/// <summary>
///     资源加载系统，用于统一管理和缓存Godot资源（如场景、纹理等）的加载与实例化。
///     提供基础加载、场景实例化、资源工厂注册以及缓存管理功能。
/// </summary>
public class ResourceLoadSystem : AbstractSystem, IResourceLoadSystem
{
    /// <summary>
    ///     已加载的资源缓存字典，键为资源路径，值为已加载的Resource对象。
    /// </summary>
    private readonly Dictionary<string, Resource> _loadedResources = new();

    /// <summary>
    ///     资源获取/复制工厂委托缓存，键为资源路径，值为获取或复制资源的Func委托。
    /// </summary>
    private readonly Dictionary<string, Delegate> _resourceFactories = new();

    /// <summary>
    ///     场景实例化工厂委托缓存，键为场景路径，值为创建该场景实例的Func委托。
    /// </summary>
    private readonly Dictionary<string, Delegate> _sceneFactories = new();

    /// <summary>
    ///     场景懒加载器缓存，键为场景路径，值为延迟加载的PackedScene对象。
    /// </summary>
    private readonly Dictionary<string, Lazy<PackedScene>> _sceneLoaders = new();

    /// <summary>
    ///     根据给定路径加载场景，并创建其节点实例。
    /// </summary>
    /// <typeparam name="T">期望返回的节点类型，必须是Node的子类。</typeparam>
    /// <param name="path">场景文件的相对路径。</param>
    /// <returns>新创建的场景根节点实例；如果加载失败则返回null。</returns>
    public T? CreateInstance<T>(string path) where T : Node
    {
        var scene = GetSceneLoader(path).Value;
        return scene.Instantiate<T>();
    }

    public Func<T> GetOrRegisterGameUnitFactory<T>(AssetCatalog.SceneUnitId id) where T : Node
    {
        var path = id.Path;
        if (_sceneFactories.TryGetValue(path, out var d))
            return d as Func<T> ??
                   throw new InvalidCastException($"Factory for path '{path}' is not of type Func<{typeof(T)}>");

        var factory = () =>
        {
            var scene = GetSceneLoader(path).Value
                        ?? throw new InvalidOperationException($"Scene not loaded: {path}");

            return scene.Instantiate<T>()
                   ?? throw new InvalidOperationException($"Instantiate failed: {path}");
        };

        _sceneFactories[path] = factory;
        return factory;
    }

    public Func<T> GetOrRegisterTemplateFactory<T>(AssetCatalog.ScenePageId id) where T : Node
    {
        var path = id.Path;
        if (_sceneFactories.TryGetValue(path, out var d))
            return d as Func<T> ??
                   throw new InvalidCastException($"Factory for path '{path}' is not of type Func<{typeof(T)}>");

        var factory = () =>
        {
            var scene = GetSceneLoader(path).Value
                        ?? throw new InvalidOperationException($"Scene not loaded: {path}");

            return scene.Instantiate<T>()
                   ?? throw new InvalidOperationException($"Instantiate failed: {path}");
        };

        _sceneFactories[path] = factory;
        return factory;
    }

    public Func<T> GetOrRegisterAssetFactory<T>(AssetCatalog.AssetId id, bool duplicate = false) where T : Resource
    {
        var path = id.Path;
        if (_resourceFactories.TryGetValue(path, out var d))
            return d as Func<T> ??
                   throw new InvalidCastException($"Factory for path '{path}' is not of type Func<{typeof(T)}>");

        var factory = () =>
        {
            var res = LoadResource<T>(path)
                      ?? throw new InvalidOperationException($"Load failed: {path}");

            if (!duplicate) return res;

            return res.Duplicate() as T ?? res;
        };

        _resourceFactories[path] = factory;
        return factory;
    }

    /// <summary>
    ///     初始化方法，在系统初始化时打印日志信息。
    /// </summary>
    protected override void OnInit()
    {
    }

    #region 基础加载

    /// <summary>
    ///     加载指定类型的资源并进行缓存。如果资源已经加载过则直接从缓存中返回。
    /// </summary>
    /// <typeparam name="T">要加载的资源类型，必须继承自Resource。</typeparam>
    /// <param name="path">资源在项目中的相对路径。</param>
    /// <returns>成功加载的资源对象；若路径无效或加载失败则返回null。</returns>
    public T? LoadResource<T>(string path) where T : Resource
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (_loadedResources.TryGetValue(path, out var cached))
            return cached as T;

        var res = GD.Load<T>(path);
        if (res == null)
        {
            GD.PrintErr($"[ResourceLoadSystem] Load failed: {path}");
            return null;
        }

        _loadedResources[path] = res;
        return res;
    }

    /// <summary>
    ///     获取一个场景的懒加载器，用于按需加载PackedScene资源。
    ///     若对应路径尚未注册加载器，则会自动创建一个新的Lazy实例。
    /// </summary>
    /// <param name="path">场景文件的相对路径。</param>
    /// <returns>表示该场景懒加载逻辑的Lazy&lt;PackedScene&gt;对象。</returns>
    public Lazy<PackedScene> GetSceneLoader(string path)
    {
        if (_sceneLoaders.TryGetValue(path, out var loader))
            return loader;

        loader = new Lazy<PackedScene>(() =>
        {
            var scene = LoadResource<PackedScene>(path);
            return scene ?? throw new InvalidOperationException($"Failed to load scene: {path}");
        });

        _sceneLoaders[path] = loader;
        return loader;
    }

    #endregion

    #region 缓存管理

    /// <summary>
    ///     预加载一组资源和场景到内存中以提升后续访问速度。
    /// </summary>
    /// <param name="paths">待预加载的资源路径集合。</param>
    public void Preload(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            GetSceneLoader(path);
            LoadResource<Resource>(path);
        }
    }

    /// <summary>
    ///     清除指定路径的所有相关缓存数据，包括资源、场景加载器及各类工厂。
    /// </summary>
    /// <param name="path">要卸载的资源路径。</param>
    public void Unload(string path)
    {
        _loadedResources.Remove(path);
        _sceneLoaders.Remove(path);
        _sceneFactories.Remove(path);
        _resourceFactories.Remove(path);
    }

    /// <summary>
    ///     清空所有当前系统的资源缓存、加载器和工厂列表。
    /// </summary>
    public void ClearAll()
    {
        _loadedResources.Clear();
        _sceneLoaders.Clear();
        _sceneFactories.Clear();
        _resourceFactories.Clear();
    }

    #endregion
}