using GFramework.Core.Abstractions.storage;
using GFramework.Game.Abstractions.storage;

namespace GFramework.Game.storage;

/// <summary>
/// 提供带有作用域前缀的存储包装器，将所有键都加上指定的前缀
/// </summary>
/// <param name="inner">内部的实际存储实现</param>
/// <param name="prefix">用于所有键的前缀字符串</param>
public sealed class ScopedStorage(IStorage inner, string prefix) : IScopedStorage
{
    /// <summary>
    /// 检查指定键是否存在
    /// </summary>
    /// <param name="key">要检查的键</param>
    /// <returns>如果键存在则返回true，否则返回false</returns>
    public bool Exists(string key)
        => inner.Exists(Key(key));

    /// <summary>
    /// 异步检查指定键是否存在
    /// </summary>
    /// <param name="key">要检查的键</param>
    /// <returns>如果键存在则返回true，否则返回false</returns>
    public Task<bool> ExistsAsync(string key)
        => inner.ExistsAsync(Key(key));

    /// <summary>
    /// 读取指定键的值
    /// </summary>
    /// <typeparam name="T">要读取的值的类型</typeparam>
    /// <param name="key">要读取的键</param>
    /// <returns>键对应的值</returns>
    public T Read<T>(string key)
        => inner.Read<T>(Key(key));

    /// <summary>
    /// 读取指定键的值，如果键不存在则返回默认值
    /// </summary>
    /// <typeparam name="T">要读取的值的类型</typeparam>
    /// <param name="key">要读取的键</param>
    /// <param name="defaultValue">当键不存在时返回的默认值</param>
    /// <returns>键对应的值或默认值</returns>
    public T Read<T>(string key, T defaultValue)
        => inner.Read(Key(key), defaultValue);

    /// <summary>
    /// 异步读取指定键的值
    /// </summary>
    /// <typeparam name="T">要读取的值的类型</typeparam>
    /// <param name="key">要读取的键</param>
    /// <returns>键对应的值的任务</returns>
    public Task<T> ReadAsync<T>(string key)
        => inner.ReadAsync<T>(Key(key));

    /// <summary>
    /// 写入指定键值对
    /// </summary>
    /// <typeparam name="T">要写入的值的类型</typeparam>
    /// <param name="key">要写入的键</param>
    /// <param name="value">要写入的值</param>
    public void Write<T>(string key, T value)
        => inner.Write(Key(key), value);

    /// <summary>
    /// 异步写入指定键值对
    /// </summary>
    /// <typeparam name="T">要写入的值的类型</typeparam>
    /// <param name="key">要写入的键</param>
    /// <param name="value">要写入的值</param>
    public Task WriteAsync<T>(string key, T value)
        => inner.WriteAsync(Key(key), value);

    /// <summary>
    /// 删除指定键
    /// </summary>
    /// <param name="key">要删除的键</param>
    public void Delete(string key)
        => inner.Delete(Key(key));

    /// <summary>
    /// 为给定的键添加前缀
    /// </summary>
    /// <param name="key">原始键</param>
    /// <returns>添加前缀后的键</returns>
    private string Key(string key)
        => string.IsNullOrEmpty(prefix)
            ? key
            : $"{prefix}/{key}";

    /// <summary>
    /// 创建一个新的作用域存储实例
    /// </summary>
    /// <param name="scope">新的作用域名称</param>
    /// <returns>新的作用域存储实例</returns>
    public IStorage Scope(string scope)
        => new ScopedStorage(inner, Key(scope));
}