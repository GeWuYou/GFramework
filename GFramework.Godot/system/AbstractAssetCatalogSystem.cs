using GFramework.Core.system;

namespace GFramework.Godot.system;

/// <summary>
/// 资源目录系统抽象基类，用于管理和注册游戏中的场景和资源
/// </summary>
public abstract class AbstractAssetCatalogSystem : AbstractSystem, IAssetCatalogSystem
{
    private readonly Dictionary<string, AssetCatalog.SceneId> _scenes = new();
    private readonly Dictionary<string, AssetCatalog.ResourceId> _resources = new();

    /// <summary>
    /// 系统初始化时调用，用于注册所有资产
    /// </summary>
    protected override void OnInit()
    {
        RegisterAssets();
    }

    /// <summary>
    /// 抽象方法，由子类实现具体的资产注册逻辑
    /// </summary>
    protected abstract void RegisterAssets();

    #region Register（内部 or Module 使用）

    /// <summary>
    /// 注册场景资源
    /// </summary>
    /// <param name="key">场景的唯一标识键</param>
    /// <param name="path">场景资源的路径</param>
    /// <exception cref="InvalidOperationException">当场景键已存在时抛出异常</exception>
    public void RegisterScene(string key, string path)
    {
        if (_scenes.ContainsKey(key))
            throw new InvalidOperationException($"Scene key duplicated: {key}");

        _scenes[key] = new AssetCatalog.SceneId(path);
    }

    /// <summary>
    /// 注册场景资源
    /// </summary>
    /// <param name="mapping">包含键和场景标识符的映射对象</param>
    /// <exception cref="InvalidOperationException">当场景键已存在时抛出异常</exception>
    public void RegisterScene(AssetCatalog.AssetCatalogMapping mapping)
    {
        if (mapping.Id is not AssetCatalog.SceneId sceneId)
            throw new InvalidOperationException("Mapping ID is not a SceneId");

        if (!_scenes.TryAdd(mapping.Key, sceneId))
            throw new InvalidOperationException($"Scene key duplicated: {mapping.Key}");
    }

    /// <summary>
    /// 注册普通资源
    /// </summary>
    /// <param name="key">资源的唯一标识键</param>
    /// <param name="path">资源的路径</param>
    /// <exception cref="InvalidOperationException">当资源键已存在时抛出异常</exception>
    public void RegisterResource(string key, string path)
    {
        if (_resources.ContainsKey(key))
            throw new InvalidOperationException($"Resource key duplicated: {key}");

        _resources[key] = new AssetCatalog.ResourceId(path);
    }

    /// <summary>
    /// 注册普通资源
    /// </summary>
    /// <param name="mapping">包含键和资源标识符的映射对象</param>
    /// <exception cref="InvalidOperationException">当资源键已存在时抛出异常</exception>
    public void RegisterResource(AssetCatalog.AssetCatalogMapping mapping)
    {
        if (mapping.Id is not AssetCatalog.ResourceId resourceId)
            throw new InvalidOperationException("Mapping ID is not a ResourceId");

        if (!_resources.TryAdd(mapping.Key, resourceId))
            throw new InvalidOperationException($"Resource key duplicated: {mapping.Key}");
    }

    #endregion

    #region Query（对外）

    /// <summary>
    /// 根据键获取场景ID
    /// </summary>
    /// <param name="key">场景的唯一标识键</param>
    /// <returns>对应的场景ID</returns>
    public AssetCatalog.SceneId GetScene(string key) => _scenes[key];

    /// <summary>
    /// 根据键获取资源ID
    /// </summary>
    /// <param name="key">资源的唯一标识键</param>
    /// <returns>对应的资源ID</returns>
    public AssetCatalog.ResourceId GetResource(string key) => _resources[key];

    /// <summary>
    /// 检查是否存在指定键的场景
    /// </summary>
    /// <param name="key">场景的唯一标识键</param>
    /// <returns>如果存在返回true，否则返回false</returns>
    public bool HasScene(string key) => _scenes.ContainsKey(key);

    /// <summary>
    /// 检查是否存在指定键的资源
    /// </summary>
    /// <param name="key">资源的唯一标识键</param>
    /// <returns>如果存在返回true，否则返回false</returns>
    public bool HasResource(string key) => _resources.ContainsKey(key);

    #endregion
}
