using GFramework.Core.Abstractions.storage;
using GFramework.Core.extensions;
using GFramework.Core.utility;
using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting;

/// <summary>
/// 设置持久化服务类，负责处理设置数据的加载、保存、删除等操作
/// </summary>
public class SettingsPersistence : AbstractContextUtility, ISettingsPersistence
{
    private IStorage _storage = null!;

    /// <summary>
    /// 异步加载指定类型的设置节数据
    /// </summary>
    /// <typeparam name="T">设置节类型，必须实现ISettingsSection接口</typeparam>
    /// <returns>如果存在则返回已保存的设置数据，否则返回新创建的默认设置实例</returns>
    public async Task<T> LoadAsync<T>() where T : class, ISettingsSection, new()
    {
        var key = GetKey<T>();

        if (await _storage.ExistsAsync(key))
        {
            return await _storage.ReadAsync<T>(key);
        }

        return new T();
    }

    /// <summary>
    /// 异步保存设置节数据到存储中
    /// </summary>
    /// <typeparam name="T">设置节类型，必须实现ISettingsSection接口</typeparam>
    /// <param name="section">要保存的设置节实例</param>
    public async Task SaveAsync<T>(T section) where T : class, ISettingsSection
    {
        var key = GetKey<T>();
        await _storage.WriteAsync(key, section);
    }

    /// <summary>
    /// 异步检查指定类型的设置节是否存在
    /// </summary>
    /// <typeparam name="T">设置节类型，必须实现ISettingsSection接口</typeparam>
    /// <returns>如果设置节存在返回true，否则返回false</returns>
    public async Task<bool> ExistsAsync<T>() where T : class, ISettingsSection
    {
        var key = GetKey<T>();
        return await _storage.ExistsAsync(key);
    }

    /// <summary>
    /// 异步删除指定类型的设置节数据
    /// </summary>
    /// <typeparam name="T">设置节类型，必须实现ISettingsSection接口</typeparam>
    public async Task DeleteAsync<T>() where T : class, ISettingsSection
    {
        var key = GetKey<T>();
        _storage.Delete(key);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 初始化方法，获取存储服务实例
    /// </summary>
    protected override void OnInit()
    {
        _storage = this.GetUtility<IStorage>()!;
    }

    /// <summary>
    /// 获取设置节对应的存储键名
    /// </summary>
    /// <typeparam name="T">设置节类型</typeparam>
    /// <returns>格式为"Settings_类型名称"的键名字符串</returns>
    private static string GetKey<T>() where T : ISettingsSection
        => $"Settings_{typeof(T).Name}";
}