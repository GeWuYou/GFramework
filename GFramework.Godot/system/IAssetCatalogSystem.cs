using GFramework.Core.system;

namespace GFramework.Godot.system;

/// <summary>
/// 资源目录系统接口，用于管理场景和资源的获取与查询
/// </summary>
public interface IAssetCatalogSystem : ISystem
{
    /// <summary>
    /// 根据键名获取场景标识符
    /// </summary>
    /// <param name="key">场景的唯一键名</param>
    /// <returns>返回对应的场景ID</returns>
    AssetCatalog.SceneId GetScene(string key);
    
    /// <summary>
    /// 根据键名获取资源标识符
    /// </summary>
    /// <param name="key">资源的唯一键名</param>
    /// <returns>返回对应的资源ID</returns>
    AssetCatalog.ResourceId GetResource(string key);

    /// <summary>
    /// 检查是否存在指定键名的场景
    /// </summary>
    /// <param name="key">要检查的场景键名</param>
    /// <returns>如果存在返回true，否则返回false</returns>
    bool HasScene(string key);
    
    /// <summary>
    /// 检查是否存在指定键名的资源
    /// </summary>
    /// <param name="key">要检查的资源键名</param>
    /// <returns>如果存在返回true，否则返回false</returns>
    bool HasResource(string key);
}
