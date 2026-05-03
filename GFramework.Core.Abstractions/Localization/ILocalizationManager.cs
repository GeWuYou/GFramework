// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using GFramework.Core.Abstractions.Systems;

namespace GFramework.Core.Abstractions.Localization;

/// <summary>
/// 本地化管理器接口
/// </summary>
public interface ILocalizationManager : ISystem
{
    /// <summary>
    /// 当前语言代码
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// 当前文化信息
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// 可用语言列表
    /// </summary>
    IReadOnlyList<string> AvailableLanguages { get; }

    /// <summary>
    /// 设置当前语言
    /// </summary>
    /// <param name="languageCode">语言代码</param>
    void SetLanguage(string languageCode);

    /// <summary>
    /// 获取本地化表
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <returns>本地化表</returns>
    ILocalizationTable GetTable(string tableName);

    /// <summary>
    /// 获取本地化文本
    /// </summary>
    /// <param name="table">表名</param>
    /// <param name="key">键名</param>
    /// <returns>本地化文本</returns>
    string GetText(string table, string key);

    /// <summary>
    /// 获取本地化字符串（支持变量和格式化）
    /// </summary>
    /// <param name="table">表名</param>
    /// <param name="key">键名</param>
    /// <returns>本地化字符串</returns>
    ILocalizationString GetString(string table, string key);

    /// <summary>
    /// 尝试获取本地化文本
    /// </summary>
    /// <param name="table">表名</param>
    /// <param name="key">键名</param>
    /// <param name="text">输出文本</param>
    /// <returns>是否成功获取</returns>
    bool TryGetText(string table, string key, out string text);

    /// <summary>
    /// 注册格式化器
    /// </summary>
    /// <param name="name">格式化器名称</param>
    /// <param name="formatter">格式化器实例</param>
    void RegisterFormatter(string name, ILocalizationFormatter formatter);

    /// <summary>
    /// 获取格式化器
    /// </summary>
    /// <param name="name">格式化器名称</param>
    /// <returns>格式化器实例，如果不存在则返回 null</returns>
    ILocalizationFormatter? GetFormatter(string name);

    /// <summary>
    /// 订阅语言变化事件
    /// </summary>
    /// <param name="callback">回调函数</param>
    void SubscribeToLanguageChange(Action<string> callback);

    /// <summary>
    /// 取消订阅语言变化事件
    /// </summary>
    /// <param name="callback">回调函数</param>
    void UnsubscribeFromLanguageChange(Action<string> callback);
}