using GFramework.Core.system;

namespace GFramework.Game.assets;

/// <summary>
/// 资源目录系统抽象基类，用于管理和注册游戏中的场景和资源。
/// 提供了统一的接口来注册和查询不同类型的资产（如游戏单元、模板、普通资源）。
/// 子类需要实现 <see cref="RegisterAssets"/> 方法以完成具体资产的注册逻辑。
/// </summary>
public abstract class AbstractAssetCatalogSystem : AbstractSystem, IAssetCatalogSystem
{
    private readonly Dictionary<string, AssetCatalog.SceneUnitId> _sceneUnits = new();
    private readonly Dictionary<string, AssetCatalog.ScenePageId> _scenePages = new();
    private readonly Dictionary<string, AssetCatalog.AssetId> _assets = new();


    /// <summary>
    /// 系统初始化时调用，用于触发资产注册流程。
    /// 此方法会调用抽象方法 <see cref="RegisterAssets"/>，由子类提供实际注册逻辑。
    /// </summary>
    protected override void OnInit()
    {
        RegisterAssets();
    }

    /// <summary>
    /// 抽象方法，必须在子类中重写。用于定义具体的资产注册逻辑。
    /// 在此方法中应通过调用各种 Register 方法将资产信息添加到系统中。
    /// </summary>
    protected abstract void RegisterAssets();

    #region Register（内部 or Module 使用）


    /// <summary>
    /// 注册场景单元到资产目录中
    /// </summary>
    /// <param name="key">场景单元的唯一标识键</param>
    /// <param name="path">场景单元资源的路径</param>
    /// <exception cref="InvalidOperationException">当指定的键已存在时抛出异常</exception>
    public void RegisterSceneUnit(string key, string path)
    {
        // 尝试添加场景单元，如果键已存在则抛出异常
        if (!_sceneUnits.TryAdd(key, new AssetCatalog.SceneUnitId(path)))
            throw new InvalidOperationException($"SceneUnit key duplicated: {key}");
    }

    /// <summary>
    /// 通过资产目录映射注册场景单元
    /// </summary>
    /// <param name="mapping">包含场景单元信息的资产目录映射对象</param>
    /// <exception cref="InvalidOperationException">当映射ID不是SceneUnitId类型或键已存在时抛出异常</exception>
    public void RegisterSceneUnit(AssetCatalog.AssetCatalogMapping mapping)
    {
        // 验证映射ID是否为SceneUnitId类型
        if (mapping.Id is not AssetCatalog.SceneUnitId sceneId)
            throw new InvalidOperationException("Mapping ID is not a SceneUnitId");

        // 尝试添加场景单元，如果键已存在则抛出异常
        if (!_sceneUnits.TryAdd(mapping.Key, sceneId))
            throw new InvalidOperationException($"Scene key duplicated: {mapping.Key}");
    }

    /// <summary>
    /// 注册场景页面模板
    /// </summary>
    /// <param name="key">场景页面的唯一标识键</param>
    /// <param name="path">场景页面资源路径</param>
    /// <exception cref="InvalidOperationException">当键已存在时抛出异常</exception>
    public void RegisterScenePage(string key, string path)
    {
        if (!_scenePages.TryAdd(key, new AssetCatalog.ScenePageId(path)))
            throw new InvalidOperationException($"Template key duplicated: {key}");
    }
    
    /// <summary>
    /// 通过资产目录映射注册场景页面
    /// </summary>
    /// <param name="mapping">包含场景页面信息的资产目录映射对象</param>
    /// <exception cref="InvalidOperationException">当映射ID不是ScenePageId类型或键已存在时抛出异常</exception>
    public void RegisterScenePage(AssetCatalog.AssetCatalogMapping mapping)
    {
        // 验证映射ID是否为ScenePageId类型
        if (mapping.Id is not AssetCatalog.ScenePageId templateId)
            throw new InvalidOperationException("Mapping ID is not a ScenePageId");

        // 尝试添加场景页面，如果键已存在则抛出异常
        if (!_scenePages.TryAdd(mapping.Key, templateId))
            throw new InvalidOperationException($"Template key duplicated: {mapping.Key}");
    }


    /// <summary>
    /// 注册一个通用资源（Asset），使用指定的键和路径。
    /// </summary>
    /// <param name="key">唯一标识该资源的字符串键。</param>
    /// <param name="path">该资源对应的资源路径。</param>
    /// <exception cref="InvalidOperationException">当键已存在时抛出异常。</exception>
    public void RegisterAsset(string key, string path)
    {
        if (!_assets.TryAdd(key, new AssetCatalog.AssetId(path)))
            throw new InvalidOperationException($"Asset key duplicated: {key}");
    }

    /// <summary>
    /// 根据映射对象注册一个通用资源（Asset）。
    /// </summary>
    /// <param name="mapping">包含键与ID映射关系的对象。</param>
    /// <exception cref="InvalidOperationException">
    /// 当映射ID不是 <see cref="AssetCatalog.AssetId"/> 类型或键重复时抛出异常。
    /// </exception>
    public void RegisterAsset(AssetCatalog.AssetCatalogMapping mapping)
    {
        if (mapping.Id is not AssetCatalog.AssetId assetId)
            throw new InvalidOperationException("Mapping ID is not a AssetId");

        if (!_assets.TryAdd(mapping.Key, assetId))
            throw new InvalidOperationException($"Asset key duplicated: {mapping.Key}");
    }
    #endregion

    #region Query（对外）

    /// <summary>
    /// 根据指定的键获取场景单元标识符
    /// </summary>
    /// <param name="key">用于查找场景单元的键值</param>
    /// <returns>返回与指定键对应的场景单元标识符</returns>
    public AssetCatalog.SceneUnitId GetSceneUnit(string key) => _sceneUnits[key];

    /// <summary>
    /// 根据指定的键获取场景页面标识符
    /// </summary>
    /// <param name="key">用于查找场景页面的键值</param>
    /// <returns>返回与指定键对应的场景页面标识符</returns>
    public AssetCatalog.ScenePageId GetScenePage(string key) => _scenePages[key];

    /// <summary>
    /// 获取指定键对应的通用资源ID。
    /// </summary>
    /// <param name="key">要查找的通用资源键。</param>
    /// <returns>对应的通用资源ID。</returns>
    /// <exception cref="KeyNotFoundException">如果未找到指定键则抛出异常。</exception>
    public AssetCatalog.AssetId GetAsset(string key) => _assets[key];

    /// <summary>
    /// 检查是否存在指定键的场景单元
    /// </summary>
    /// <param name="key">用于查找场景单元的键值</param>
    /// <returns>存在返回true，否则返回false</returns>
    public bool HasSceneUnit(string key) => _sceneUnits.ContainsKey(key);

    /// <summary>
    /// 检查是否存在指定键的场景页面
    /// </summary>
    /// <param name="key">用于查找场景页面的键值</param>
    /// <returns>存在返回true，否则返回false</returns>
    public bool HasScenePage(string key) => _scenePages.ContainsKey(key);

    /// <summary>
    /// 判断是否存在指定键的通用资源。
    /// </summary>
    /// <param name="key">要检查的通用资源键。</param>
    /// <returns>若存在返回 true，否则返回 false。</returns>
    public bool HasAsset(string key) => _assets.ContainsKey(key);
    #endregion
}
