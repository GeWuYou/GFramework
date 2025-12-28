using GFramework.Core.Abstractions.system;

namespace GFramework.Godot.Abstractions.assets;

/// <summary>
///     资源加载系统接口，提供资源和场景的加载、实例化、预加载等功能
/// </summary>
public interface IResourceLoadSystem : ISystem
{
    /// <summary>
    ///     加载指定路径的资源
    /// </summary>
    /// <typeparam name="T">资源类型，必须继承自Resource</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>加载的资源实例</returns>
    public T? LoadResource<T>(string path) where T : Resource;

    /// <summary>
    ///     获取场景加载器，用于延迟加载场景
    /// </summary>
    /// <param name="path">场景路径</param>
    /// <returns>场景的延迟加载包装器</returns>
    public Lazy<PackedScene> GetSceneLoader(string path);

    /// <summary>
    ///     创建指定路径场景的实例
    /// </summary>
    /// <typeparam name="T">节点类型，必须继承自Node</typeparam>
    /// <param name="path">场景路径</param>
    /// <returns>场景实例化的节点对象</returns>
    public T? CreateInstance<T>(string path) where T : Node;

    /// <summary>
    ///     获取或注册游戏单位工厂函数
    /// </summary>
    /// <typeparam name="T">节点类型，必须继承自Node</typeparam>
    /// <param name="id">场景资源标识符</param>
    /// <returns>创建场景实例的工厂函数</returns>
    Func<T> GetOrRegisterGameUnitFactory<T>(
        AssetCatalog.SceneUnitId id
    ) where T : Node;

    /// <summary>
    ///     获取或注册模板资源工厂函数
    /// </summary>
    /// <typeparam name="T">节点类型，必须继承自Node</typeparam>
    /// <param name="id">模板资源标识符</param>
    /// <returns>创建模板实例的工厂函数</returns>
    Func<T> GetOrRegisterTemplateFactory<T>(
        AssetCatalog.ScenePageId id
    ) where T : Node;

    /// <summary>
    ///     获取或注册通用资产工厂函数
    /// </summary>
    /// <typeparam name="T">资源类型，必须继承自Resource</typeparam>
    /// <param name="id">资产资源标识符</param>
    /// <param name="duplicate">是否对原始资源进行复制操作，默认为false</param>
    /// <returns>创建资产实例的工厂函数</returns>
    Func<T> GetOrRegisterAssetFactory<T>(
        AssetCatalog.AssetId id,
        bool duplicate = false
    ) where T : Resource;

    /// <summary>
    ///     预加载指定路径的多个资源
    /// </summary>
    /// <param name="paths">需要预加载的资源路径集合</param>
    public void Preload(IEnumerable<string> paths);

    /// <summary>
    ///     卸载指定路径的资源
    /// </summary>
    /// <param name="path">需要卸载的资源路径</param>
    public void Unload(string path);

    /// <summary>
    ///     清除所有已加载的资源
    /// </summary>
    public void ClearAll();
}