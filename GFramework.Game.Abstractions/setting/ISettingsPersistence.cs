using System.Threading.Tasks;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
/// 设置持久化接口
/// 定义了设置数据的异步加载、保存、检查存在性和删除操作
/// </summary>
public interface ISettingsPersistence
{
    /// <summary>
    /// 异步加载指定类型的设置节
    /// </summary>
    /// <typeparam name="T">设置节类型，必须实现ISettingsSection接口并具有无参构造函数</typeparam>
    /// <returns>返回加载的设置节实例</returns>
    Task<T> LoadAsync<T>() where T : class, ISettingsSection, new();

    /// <summary>
    /// 异步保存指定的设置节
    /// </summary>
    /// <typeparam name="T">设置节类型，必须实现ISettingsSection接口</typeparam>
    /// <param name="section">要保存的设置节实例</param>
    /// <returns>异步操作任务</returns>
    Task SaveAsync<T>(T section) where T : class, ISettingsSection;

    /// <summary>
    /// 异步检查指定类型的设置节是否存在
    /// </summary>
    /// <typeparam name="T">设置节类型，必须实现ISettingsSection接口</typeparam>
    /// <returns>如果设置节存在则返回true，否则返回false</returns>
    Task<bool> ExistsAsync<T>() where T : class, ISettingsSection;

    /// <summary>
    /// 异步删除指定类型的设置节
    /// </summary>
    /// <typeparam name="T">设置节类型，必须实现ISettingsSection接口</typeparam>
    /// <returns>异步操作任务</returns>
    Task DeleteAsync<T>() where T : class, ISettingsSection;
}