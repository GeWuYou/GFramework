// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Property;
using GFramework.Core.Abstractions.StateManagement;
using GFramework.Core.StateManagement;

namespace GFramework.Core.Extensions;

/// <summary>
///     为 Store 提供选择器和 BindableProperty 风格桥接扩展。
///     这些扩展用于在集中式状态容器和现有 Property/UI 生态之间建立最小侵入的互操作层。
/// </summary>
public static class StoreExtensions
{
    /// <summary>
    ///     从 Store 中选择一个局部状态视图。
    /// </summary>
    /// <typeparam name="TState">源状态类型。</typeparam>
    /// <typeparam name="TSelected">局部状态类型。</typeparam>
    /// <param name="store">源 Store。</param>
    /// <param name="selector">状态选择委托。</param>
    /// <returns>可用于订阅局部状态变化的只读绑定视图。</returns>
    public static StoreSelection<TState, TSelected> Select<TState, TSelected>(
        this IReadonlyStore<TState> store,
        Func<TState, TSelected> selector)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        return new StoreSelection<TState, TSelected>(store, selector);
    }

    /// <summary>
    ///     从 Store 中选择一个局部状态视图，并指定局部状态比较器。
    /// </summary>
    /// <typeparam name="TState">源状态类型。</typeparam>
    /// <typeparam name="TSelected">局部状态类型。</typeparam>
    /// <param name="store">源 Store。</param>
    /// <param name="selector">状态选择委托。</param>
    /// <param name="comparer">用于比较局部状态是否变化的比较器。</param>
    /// <returns>可用于订阅局部状态变化的只读绑定视图。</returns>
    public static StoreSelection<TState, TSelected> Select<TState, TSelected>(
        this IReadonlyStore<TState> store,
        Func<TState, TSelected> selector,
        IEqualityComparer<TSelected>? comparer)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        return new StoreSelection<TState, TSelected>(store, selector, comparer);
    }

    /// <summary>
    ///     使用显式选择器对象从 Store 中选择一个局部状态视图。
    /// </summary>
    /// <typeparam name="TState">源状态类型。</typeparam>
    /// <typeparam name="TSelected">局部状态类型。</typeparam>
    /// <param name="store">源 Store。</param>
    /// <param name="selector">状态选择器实例。</param>
    /// <param name="comparer">用于比较局部状态是否变化的比较器。</param>
    /// <returns>可用于订阅局部状态变化的只读绑定视图。</returns>
    public static StoreSelection<TState, TSelected> Select<TState, TSelected>(
        this IReadonlyStore<TState> store,
        IStateSelector<TState, TSelected> selector,
        IEqualityComparer<TSelected>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        return new StoreSelection<TState, TSelected>(store, selector.Select, comparer);
    }

    /// <summary>
    ///     将 Store 中选中的局部状态桥接为 IReadonlyBindableProperty 风格接口。
    /// </summary>
    /// <typeparam name="TState">源状态类型。</typeparam>
    /// <typeparam name="TSelected">局部状态类型。</typeparam>
    /// <param name="store">源 Store。</param>
    /// <param name="selector">状态选择委托。</param>
    /// <param name="comparer">用于比较局部状态是否变化的比较器。</param>
    /// <returns>只读绑定属性视图。</returns>
    public static IReadonlyBindableProperty<TSelected> ToBindableProperty<TState, TSelected>(
        this IReadonlyStore<TState> store,
        Func<TState, TSelected> selector,
        IEqualityComparer<TSelected>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        return new StoreSelection<TState, TSelected>(store, selector, comparer);
    }
}