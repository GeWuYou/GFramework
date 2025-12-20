using GFramework.Core.system;

namespace GFramework.Game.assets;

/// <summary>
/// 资源目录系统抽象基类，用于管理和注册游戏中的场景和资源。
/// 提供了统一的接口来注册和查询不同类型的资产（如游戏单元、模板、普通资源）。
/// 子类需要实现 <see cref="RegisterAssets"/> 方法以完成具体资产的注册逻辑。
/// </summary>
public abstract class AbstractAssetCatalogSystem : AbstractSystem, IAssetCatalogSystem
{
    private readonly Dictionary<string, AssetCatalog.GameUnitId> _gameUnits = new();
    private readonly Dictionary<string, AssetCatalog.TemplateId> _templates = new();
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
    /// 注册一个游戏单元（GameUnit），使用指定的键和路径。
    /// </summary>
    /// <param name="key">唯一标识该游戏单元的字符串键。</param>
    /// <param name="path">该游戏单元对应的资源路径。</param>
    /// <exception cref="InvalidOperationException">当键已存在时抛出异常。</exception>
    public void RegisterGameUnit(string key, string path)
    {
        if (!_gameUnits.TryAdd(key, new AssetCatalog.GameUnitId(path)))
            throw new InvalidOperationException($"GameUnit key duplicated: {key}");
    }

    /// <summary>
    /// 根据映射对象注册一个游戏单元（GameUnit）。
    /// </summary>
    /// <param name="mapping">包含键与ID映射关系的对象。</param>
    /// <exception cref="InvalidOperationException">
    /// 当映射ID不是 <see cref="AssetCatalog.GameUnitId"/> 类型或键重复时抛出异常。
    /// </exception>
    public void RegisterGameUnit(AssetCatalog.AssetCatalogMapping mapping)
    {
        if (mapping.Id is not AssetCatalog.GameUnitId sceneId)
            throw new InvalidOperationException("Mapping ID is not a GameUnitId");

        if (!_gameUnits.TryAdd(mapping.Key, sceneId))
            throw new InvalidOperationException($"Scene key duplicated: {mapping.Key}");
    }

    /// <summary>
    /// 注册一个模板资源（Template），使用指定的键和路径。
    /// </summary>
    /// <param name="key">唯一标识该模板的字符串键。</param>
    /// <param name="path">该模板对应的资源路径。</param>
    /// <exception cref="InvalidOperationException">当键已存在时抛出异常。</exception>
    public void RegisterTemplate(string key, string path)
    {
        if (!_templates.TryAdd(key, new AssetCatalog.TemplateId(path)))
            throw new InvalidOperationException($"Template key duplicated: {key}");
    }

    /// <summary>
    /// 根据映射对象注册一个模板资源（Template）。
    /// </summary>
    /// <param name="mapping">包含键与ID映射关系的对象。</param>
    /// <exception cref="InvalidOperationException">
    /// 当映射ID不是 <see cref="AssetCatalog.TemplateId"/> 类型或键重复时抛出异常。
    /// </exception>
    public void RegisterTemplate(AssetCatalog.AssetCatalogMapping mapping)
    {
        if (mapping.Id is not AssetCatalog.TemplateId templateId)
            throw new InvalidOperationException("Mapping ID is not a TemplateId");

        if (!_templates.TryAdd(mapping.Key, templateId))
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
    /// 获取指定键对应的游戏单元ID。
    /// </summary>
    /// <param name="key">要查找的游戏单元键。</param>
    /// <returns>对应的游戏单元ID。</returns>
    /// <exception cref="KeyNotFoundException">如果未找到指定键则抛出异常。</exception>
    public AssetCatalog.GameUnitId GetGameUnit(string key) => _gameUnits[key];

    /// <summary>
    /// 获取指定键对应的模板资源ID。
    /// </summary>
    /// <param name="key">要查找的模板资源键。</param>
    /// <returns>对应的模板资源ID。</returns>
    /// <exception cref="KeyNotFoundException">如果未找到指定键则抛出异常。</exception>
    public AssetCatalog.TemplateId GetTemplate(string key) => _templates[key];

    /// <summary>
    /// 获取指定键对应的通用资源ID。
    /// </summary>
    /// <param name="key">要查找的通用资源键。</param>
    /// <returns>对应的通用资源ID。</returns>
    /// <exception cref="KeyNotFoundException">如果未找到指定键则抛出异常。</exception>
    public AssetCatalog.AssetId GetAsset(string key) => _assets[key];

    /// <summary>
    /// 判断是否存在指定键的游戏单元。
    /// </summary>
    /// <param name="key">要检查的游戏单元键。</param>
    /// <returns>若存在返回 true，否则返回 false。</returns>
    public bool HasGameUnit(string key) => _gameUnits.ContainsKey(key);

    /// <summary>
    /// 判断是否存在指定键的模板资源。
    /// </summary>
    /// <param name="key">要检查的模板资源键。</param>
    /// <returns>若存在返回 true，否则返回 false。</returns>
    public bool HasTemplate(string key) => _templates.ContainsKey(key);

    /// <summary>
    /// 判断是否存在指定键的通用资源。
    /// </summary>
    /// <param name="key">要检查的通用资源键。</param>
    /// <returns>若存在返回 true，否则返回 false。</returns>
    public bool HasAsset(string key) => _assets.ContainsKey(key);
    #endregion
}
