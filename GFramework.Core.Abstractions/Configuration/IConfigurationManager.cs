// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Abstractions.Configuration;

/// <summary>
///     配置管理器接口，提供类型安全的配置存储和访问
///     线程安全：所有方法都是线程安全的
/// </summary>
public interface IConfigurationManager : IUtility
{
    /// <summary>
    ///     获取配置数量
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     获取指定键的配置值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <returns>配置值，如果不存在则返回类型默认值</returns>
    T? GetConfig<T>(string key);

    /// <summary>
    ///     获取指定键的配置值，如果不存在则返回默认值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值或默认值</returns>
    T GetConfig<T>(string key, T defaultValue);

    /// <summary>
    ///     设置指定键的配置值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值</param>
    void SetConfig<T>(string key, T value);

    /// <summary>
    ///     检查指定键的配置是否存在
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>如果存在返回 true，否则返回 false</returns>
    bool HasConfig(string key);

    /// <summary>
    ///     移除指定键的配置
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>如果成功移除返回 true，否则返回 false</returns>
    bool RemoveConfig(string key);

    /// <summary>
    ///     清空所有配置
    /// </summary>
    void Clear();

    /// <summary>
    ///     监听指定键的配置变化
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="onChange">配置变化时的回调，参数为新值</param>
    /// <returns>取消注册接口</returns>
    IUnRegister WatchConfig<T>(string key, Action<T> onChange);

    /// <summary>
    ///     从 JSON 字符串加载配置
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    void LoadFromJson(string json);

    /// <summary>
    ///     将配置保存为 JSON 字符串
    /// </summary>
    /// <returns>JSON 字符串</returns>
    string SaveToJson();

    /// <summary>
    ///     从文件加载配置
    /// </summary>
    /// <param name="path">文件路径</param>
    void LoadFromFile(string path);

    /// <summary>
    ///     将配置保存到文件
    /// </summary>
    /// <param name="path">文件路径</param>
    void SaveToFile(string path);

    /// <summary>
    ///     获取所有配置键
    /// </summary>
    /// <returns>配置键集合</returns>
    IEnumerable<string> GetAllKeys();
}