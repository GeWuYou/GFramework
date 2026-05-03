// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Utility;

namespace GFramework.Game.Abstractions.Config;

/// <summary>
///     定义配置注册表契约，用于统一保存和解析按名称注册的配置表。
///     注册表是运行时配置系统的入口，负责在加载阶段收集配置表，并在消费阶段提供类型安全查询。
/// </summary>
public interface IConfigRegistry : IUtility
{
    /// <summary>
    ///     获取当前已注册配置表数量。
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     获取所有已注册配置表名称。
    /// </summary>
    /// <returns>配置表名称集合。</returns>
    IReadOnlyCollection<string> GetTableNames();

    /// <summary>
    ///     注册指定名称的配置表。
    ///     若名称已存在，则替换旧表，以便开发期热重载使用同一入口刷新配置。
    /// </summary>
    /// <typeparam name="TKey">配置表主键类型。</typeparam>
    /// <typeparam name="TValue">配置项值类型。</typeparam>
    /// <param name="name">配置表名称。</param>
    /// <param name="table">要注册的配置表实例。</param>
    void RegisterTable<TKey, TValue>(string name, IConfigTable<TKey, TValue> table)
        where TKey : notnull;

    /// <summary>
    ///     获取指定名称的配置表。
    /// </summary>
    /// <typeparam name="TKey">配置表主键类型。</typeparam>
    /// <typeparam name="TValue">配置项值类型。</typeparam>
    /// <param name="name">配置表名称。</param>
    /// <returns>匹配的强类型配置表实例。</returns>
    /// <exception cref="KeyNotFoundException">当配置表名称不存在时抛出。</exception>
    /// <exception cref="InvalidOperationException">当请求类型与已注册配置表类型不匹配时抛出。</exception>
    IConfigTable<TKey, TValue> GetTable<TKey, TValue>(string name)
        where TKey : notnull;

    /// <summary>
    ///     尝试获取指定名称的配置表。
    ///     当名称存在但类型不匹配时返回 <c>false</c>，避免消费端将类型错误误判为加载成功。
    /// </summary>
    /// <typeparam name="TKey">配置表主键类型。</typeparam>
    /// <typeparam name="TValue">配置项值类型。</typeparam>
    /// <param name="name">配置表名称。</param>
    /// <param name="table">匹配的强类型配置表；未找到或类型不匹配时返回空。</param>
    /// <returns>找到且类型匹配时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    bool TryGetTable<TKey, TValue>(string name, out IConfigTable<TKey, TValue>? table)
        where TKey : notnull;

    /// <summary>
    ///     尝试获取指定名称的原始配置表。
    ///     该入口用于跨表校验或诊断场景，以便在不知道泛型参数时仍能访问表元数据。
    /// </summary>
    /// <param name="name">配置表名称。</param>
    /// <param name="table">匹配的原始配置表；未找到时返回空。</param>
    /// <returns>找到配置表时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    bool TryGetTable(string name, out IConfigTable? table);

    /// <summary>
    ///     检查指定名称的配置表是否存在。
    /// </summary>
    /// <param name="name">配置表名称。</param>
    /// <returns>存在时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    bool HasTable(string name);

    /// <summary>
    ///     移除指定名称的配置表。
    /// </summary>
    /// <param name="name">配置表名称。</param>
    /// <returns>移除成功时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    bool RemoveTable(string name);

    /// <summary>
    ///     清空所有已注册配置表。
    /// </summary>
    void Clear();
}