
namespace GFramework.Core.Godot.system;

/// <summary>
/// 资源目录类，用于定义和管理游戏中的场景和资源标识符
/// </summary>
public static class AssetCatalog
{
    /// <summary>
    /// 场景标识符结构体，用于唯一标识一个场景资源
    /// </summary>
    /// <param name="Path">场景资源的路径</param>
    public readonly record struct SceneId(string Path);
    
    /// <summary>
    /// 资源标识符结构体，用于唯一标识一个游戏资源
    /// </summary>
    /// <param name="Path">游戏资源的路径</param>
    public readonly record struct ResourceId(string Path);
}
