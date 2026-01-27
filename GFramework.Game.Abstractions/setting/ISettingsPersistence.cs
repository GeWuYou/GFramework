using GFramework.Core.Abstractions.utility;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
///     设置持久化接口
///     定义了设置数据的异步加载、保存、检查存在性和删除操作
/// </summary>
public interface ISettingsPersistence : IContextUtility
{
    /// <summary>
    ///     异步加载指定类型的设置数据
    /// </summary>
    Task<T> LoadAsync<T>() where T : class, ISettingsData, new();

    /// <summary>
    ///     异步保存指定的设置数据
    /// </summary>
    Task SaveAsync<T>(T section) where T : class, ISettingsData;

    /// <summary>
    ///     异步检查指定类型的设置数据是否存在
    /// </summary>
    Task<bool> ExistsAsync<T>() where T : class, ISettingsData;

    /// <summary>
    ///     异步删除指定类型的设置数据
    /// </summary>
    Task DeleteAsync<T>() where T : class, ISettingsData;

    /// <summary>
    ///     保存所有设置数据
    /// </summary>
    Task SaveAllAsync(IEnumerable<ISettingsData> allData);
}