using GFramework.Core.system;

namespace GFramework.Game.assets;

/// <summary>
/// 资源目录系统接口，用于管理场景和资源的获取与查询
/// </summary>
public interface IAssetCatalogSystem : ISystem
{
    /// <summary>
    /// 根据指定的键获取游戏单位ID
    /// </summary>
    /// <param name="key">用于查找游戏单位的键值</param>
    /// <returns>返回对应的游戏单位ID，如果未找到则返回默认值</returns>
    AssetCatalog.GameUnitId GetGameUnit(string key);

    /// <summary>
    /// 根据指定的键获取模板ID
    /// </summary>
    /// <param name="key">用于查找模板的键值</param>
    /// <returns>返回对应的模板ID，如果未找到则返回默认值</returns>
    AssetCatalog.TemplateId GetTemplate(string key);

    /// <summary>
    /// 根据指定的键获取资源ID
    /// </summary>
    /// <param name="key">用于查找资源的键值</param>
    /// <returns>返回对应的资源ID，如果未找到则返回默认值</returns>
    AssetCatalog.AssetId GetAsset(string key);

    /// <summary>
    /// 注册游戏单位资源到资产目录中
    /// </summary>
    /// <param name="key">游戏单位的唯一标识键值</param>
    /// <param name="path">游戏单位资源的路径</param>
    void RegisterGameUnit(string key, string path);
    
    /// <summary>
    /// 根据映射配置注册游戏单位资源到资产目录中
    /// </summary>
    /// <param name="mapping">包含键值和路径映射关系的配置对象</param>
    void RegisterGameUnit(AssetCatalog.AssetCatalogMapping mapping);
    
    /// <summary>
    /// 注册模板资源到资产目录中
    /// </summary>
    /// <param name="key">模板的唯一标识键值</param>
    /// <param name="path">模板资源的路径</param>
    void RegisterTemplate(string key, string path);
    
    /// <summary>
    /// 根据映射配置注册模板资源到资产目录中
    /// </summary>
    /// <param name="mapping">包含键值和路径映射关系的配置对象</param>
    void RegisterTemplate(AssetCatalog.AssetCatalogMapping mapping);
    
    /// <summary>
    /// 注册普通资产资源到资产目录中
    /// </summary>
    /// <param name="key">资产的唯一标识键值</param>
    /// <param name="path">资产资源的路径</param>
    void RegisterAsset(string key, string path);
    
    /// <summary>
    /// 根据映射配置注册普通资产资源到资产目录中
    /// </summary>
    /// <param name="mapping">包含键值和路径映射关系的配置对象</param>
    void RegisterAsset(AssetCatalog.AssetCatalogMapping mapping);
    
    /// <summary>
    /// 检查是否存在指定键的游戏单位
    /// </summary>
    /// <param name="key">用于查找游戏单位的键值</param>
    /// <returns>存在返回true，否则返回false</returns>
    bool HasGameUnit(string key);

    /// <summary>
    /// 检查是否存在指定键的模板
    /// </summary>
    /// <param name="key">用于查找模板的键值</param>
    /// <returns>存在返回true，否则返回false</returns>
    bool HasTemplate(string key);

    /// <summary>
    /// 检查是否存在指定键的资源
    /// </summary>
    /// <param name="key">用于查找资源的键值</param>
    /// <returns>存在返回true，否则返回false</returns>
    bool HasAsset(string key);

}
