// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events.Filters;

/// <summary>
///     基于谓词的事件过滤器
///     允许使用自定义条件函数来过滤事件
/// </summary>
/// <typeparam name="T">事件类型</typeparam>
public sealed class PredicateEventFilter<T> : IEventFilter<T>
{
    private readonly Func<T, bool> _predicate;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="predicate">过滤条件函数，返回 true 表示过滤（阻止），返回 false 表示允许</param>
    public PredicateEventFilter(Func<T, bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    /// <inheritdoc />
    public bool ShouldFilter(T eventData)
    {
        return _predicate(eventData);
    }
}