
namespace GFramework.Core.Godot.system;

/// <summary>
/// 资源目录类，用于定义和管理游戏中的场景和资源标识符
/// </summary>
public static class AssetCatalog
{
    /// <summary>
    /// 资源标识符接口，定义了资源路径的访问接口
    /// </summary>
    public interface IAssetId
    {
        /// <summary>
        /// 获取资源的路径
        /// </summary>
        string Path { get; }
    }

    /// <summary>
    /// 资源目录映射结构体，用于存储资源目录的键值对映射关系
    /// </summary>
    /// <param name="Key">资源目录的键</param>
    /// <param name="Id">资源标识符</param>
    public readonly record struct AssetCatalogMapping(string Key, IAssetId Id);
    
    /// <summary>
    /// 场景标识符结构体，用于唯一标识一个场景资源
    /// </summary>
    /// <param name="Path">场景资源的路径</param>
    public readonly record struct SceneId(string Path) : IAssetId;
    
    /// <summary>
    /// 资源标识符结构体，用于唯一标识一个游戏资源
    /// </summary>
    /// <param name="Path">游戏资源的路径</param>
    public readonly record struct ResourceId(string Path) : IAssetId;

}
