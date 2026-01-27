using GFramework.Core.Abstractions.utility;

namespace GFramework.Core.Abstractions.storage;

/// <summary>
///     存储接口，提供同步和异步的数据存储操作功能
/// </summary>
public interface IStorage : IUtility
{
    /// <summary>
    ///     检查指定键的存储项是否存在
    /// </summary>
    /// <param name="key">要检查的键</param>
    /// <returns>如果键存在则返回true，否则返回false</returns>
    bool Exists(string key);

    /// <summary>
    ///     异步检查指定键的存储项是否存在
    /// </summary>
    /// <param name="key">要检查的键</param>
    /// <returns>如果键存在则返回true，否则返回false</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    ///     读取指定键的值
    /// </summary>
    /// <typeparam name="T">要读取的值的类型</typeparam>
    /// <param name="key">要读取的键</param>
    /// <returns>指定键对应的值</returns>
    T Read<T>(string key);

    /// <summary>
    ///     读取指定键的值，如果键不存在则返回默认值
    /// </summary>
    /// <typeparam name="T">要读取的值的类型</typeparam>
    /// <param name="key">要读取的键</param>
    /// <param name="defaultValue">当键不存在时返回的默认值</param>
    /// <returns>指定键对应的值，如果键不存在则返回默认值</returns>
    T Read<T>(string key, T defaultValue);

    /// <summary>
    ///     异步读取指定键的值
    /// </summary>
    /// <typeparam name="T">要读取的值的类型</typeparam>
    /// <param name="key">要读取的键</param>
    /// <returns>指定键对应的值</returns>
    Task<T> ReadAsync<T>(string key);

    /// <summary>
    ///     将值写入指定键
    /// </summary>
    /// <typeparam name="T">要写入的值的类型</typeparam>
    /// <param name="key">要写入的键</param>
    /// <param name="value">要写入的值</param>
    void Write<T>(string key, T value);

    /// <summary>
    ///     异步将值写入指定键
    /// </summary>
    /// <typeparam name="T">要写入的值的类型</typeparam>
    /// <param name="key">要写入的键</param>
    /// <param name="value">要写入的值</param>
    /// <returns>异步操作任务</returns>
    Task WriteAsync<T>(string key, T value);

    /// <summary>
    ///     删除指定键的存储项
    /// </summary>
    /// <param name="key">要删除的键</param>
    void Delete(string key);

    /// <summary>
    ///     异步删除指定键的存储项
    /// </summary>
    /// <param name="key">要删除的键</param>
    /// <returns>表示异步操作的Task</returns>
    Task DeleteAsync(string key);
}