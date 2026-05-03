// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Events;

/// <summary>
///     事件过滤器接口
///     用于在事件触发前进行条件判断，决定是否允许事件传递给监听器
/// </summary>
/// <typeparam name="T">事件类型</typeparam>
public interface IEventFilter<in T>
{
    /// <summary>
    ///     判断事件是否应该被过滤（阻止传递）
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>如果返回 true，则事件被过滤（不传递给监听器）；如果返回 false，则允许传递</returns>
    bool ShouldFilter(T eventData);
}