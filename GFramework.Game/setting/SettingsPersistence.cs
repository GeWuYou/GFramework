using GFramework.Core.Abstractions.storage;
using GFramework.Core.extensions;
using GFramework.Core.utility;
using GFramework.Game.Abstractions.setting;
using GFramework.Game.setting.events;

namespace GFramework.Game.setting;

/// <summary>
///     设置持久化服务类，负责处理设置数据的加载、保存、删除等操作
/// </summary>
public class SettingsPersistence : AbstractContextUtility, ISettingsPersistence
{
    private IStorage _storage = null!;

    /// <summary>
    ///     异步加载指定类型的设置数据
    /// </summary>
    /// <typeparam name="T">设置数据类型，必须实现ISettingsData接口</typeparam>
    /// <returns>如果存在则返回存储的设置数据，否则返回新创建的实例</returns>
    public async Task<T> LoadAsync<T>() where T : class, IResettable, new()
    {
        var key = GetKey<T>();

        if (await _storage.ExistsAsync(key))
        {
            var result = await _storage.ReadAsync<T>(key);
            this.SendEvent(new SettingsLoadedEvent<T>(result));
            return result;
        }

        var newSettings = new T();
        this.SendEvent(new SettingsLoadedEvent<T>(newSettings));
        return newSettings;
    }

    /// <summary>
    ///     异步保存设置数据到存储中
    /// </summary>
    /// <typeparam name="T">设置数据类型，必须实现ISettingsData接口</typeparam>
    /// <param name="section">要保存的设置数据实例</param>
    public async Task SaveAsync<T>(T section) where T : class, IResettable
    {
        var key = GetKey<T>();
        await _storage.WriteAsync(key, section);
        this.SendEvent(new SettingsSavedEvent<T>(section));
    }

    /// <summary>
    ///     检查指定类型的设置数据是否存在
    /// </summary>
    /// <typeparam name="T">设置数据类型，必须实现ISettingsData接口</typeparam>
    /// <returns>如果存在返回true，否则返回false</returns>
    public async Task<bool> ExistsAsync<T>() where T : class, IResettable
    {
        var key = GetKey<T>();
        return await _storage.ExistsAsync(key);
    }

    /// <summary>
    ///     异步删除指定类型的设置数据
    /// </summary>
    /// <typeparam name="T">设置数据类型，必须实现ISettingsData接口</typeparam>
    public async Task DeleteAsync<T>() where T : class, IResettable
    {
        var key = GetKey<T>();
        await _storage.DeleteAsync(key);
        this.SendEvent(new SettingsDeletedEvent(typeof(T)));
        await Task.CompletedTask;
    }

    /// <summary>
    ///     异步保存所有设置数据到存储中
    /// </summary>
    /// <param name="allData">包含所有设置数据的可枚举集合</param>
    public async Task SaveAllAsync(IEnumerable<IResettable> allData)
    {
        var dataList = allData.ToList();
        foreach (var data in dataList)
        {
            var type = data.GetType();
            var key = GetKey(type);
            await _storage.WriteAsync(key, data);
        }

        this.SendEvent(new SettingsBatchSavedEvent(dataList));
    }

    protected override void OnInit()
    {
        _storage = this.GetUtility<IStorage>()!;
    }

    /// <summary>
    ///     获取指定类型的存储键名
    /// </summary>
    /// <typeparam name="T">设置数据类型</typeparam>
    /// <returns>格式为"Settings_类型名称"的键名</returns>
    private static string GetKey<T>() where T : IResettable
    {
        return GetKey(typeof(T));
    }

    /// <summary>
    ///     获取指定类型的存储键名
    /// </summary>
    /// <param name="type">设置数据类型</param>
    /// <returns>格式为"Settings_类型名称"的键名</returns>
    private static string GetKey(Type type)
    {
        return $"Settings_{type.Name}";
    }
}