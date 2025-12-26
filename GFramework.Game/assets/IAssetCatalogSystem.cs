using GFramework.Core.system;

namespace GFramework.Game.assets;

/// <summary>
///     资源目录系统接口，用于管理场景和资源的获取与查询
/// </summary>
public interface IAssetCatalogSystem : ISystem
{
    /// <summary>
    ///     根据指定的键获取场景单元标识符
    /// </summary>
    /// <param name="key">用于查找场景单元的键值</param>
    /// <returns>返回与指定键对应的场景单元标识符</returns>
    AssetCatalog.SceneUnitId GetSceneUnit(string key);

    /// <summary>
    ///     根据指定的键获取场景页面标识符
    /// </summary>
    /// <param name="key">用于查找场景页面的键值</param>
    /// <returns>返回与指定键对应的场景页面标识符</returns>
    AssetCatalog.ScenePageId GetScenePage(string key);


    /// <summary>
    ///     根据指定的键获取资源ID
    /// </summary>
    /// <param name="key">用于查找资源的键值</param>
    /// <returns>返回对应的资源ID，如果未找到则返回默认值</returns>
    AssetCatalog.AssetId GetAsset(string key);

    /// <summary>
    ///     注册场景单元到资产目录中
    /// </summary>
    /// <param name="key">场景单元的唯一标识键</param>
    /// <param name="path">场景单元资源的路径</param>
    /// <exception cref="InvalidOperationException">当指定的键已存在时抛出异常</exception>
    public void RegisterSceneUnit(string key, string path);

    /// <summary>
    ///     通过资产目录映射注册场景单元
    /// </summary>
    /// <param name="mapping">包含场景单元信息的资产目录映射对象</param>
    /// <exception cref="InvalidOperationException">当映射ID不是SceneUnitId类型或键已存在时抛出异常</exception>
    public void RegisterSceneUnit(AssetCatalog.AssetCatalogMapping mapping);

    /// <summary>
    ///     注册场景页面模板
    /// </summary>
    /// <param name="key">场景页面的唯一标识键</param>
    /// <param name="path">场景页面资源路径</param>
    /// <exception cref="InvalidOperationException">当键已存在时抛出异常</exception>
    void RegisterScenePage(string key, string path);

    /// <summary>
    ///     通过资产目录映射注册场景页面
    /// </summary>
    /// <param name="mapping">包含场景页面信息的资产目录映射对象</param>
    /// <exception cref="InvalidOperationException">当映射ID不是ScenePageId类型或键已存在时抛出异常</exception>
    void RegisterScenePage(AssetCatalog.AssetCatalogMapping mapping);

    /// <summary>
    ///     注册普通资产资源到资产目录中
    /// </summary>
    /// <param name="key">资产的唯一标识键值</param>
    /// <param name="path">资产资源的路径</param>
    void RegisterAsset(string key, string path);

    /// <summary>
    ///     根据映射配置注册普通资产资源到资产目录中
    /// </summary>
    /// <param name="mapping">包含键值和路径映射关系的配置对象</param>
    void RegisterAsset(AssetCatalog.AssetCatalogMapping mapping);


    /// <summary>
    ///     检查是否存在指定键的场景单元
    /// </summary>
    /// <param name="key">用于查找场景单元的键值</param>
    /// <returns>存在返回true，否则返回false</returns>
    bool HasSceneUnit(string key);


    /// <summary>
    ///     检查是否存在指定键的场景页面
    /// </summary>
    /// <param name="key">用于查找场景页面的键值</param>
    /// <returns>存在返回true，否则返回false</returns>
    bool HasScenePage(string key);

    /// <summary>
    ///     检查是否存在指定键的资源
    /// </summary>
    /// <param name="key">用于查找资源的键值</param>
    /// <returns>存在返回true，否则返回false</returns>
    bool HasAsset(string key);
}