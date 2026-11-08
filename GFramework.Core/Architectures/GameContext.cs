// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Threading;
using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Core.Architectures;

/// <summary>
///     游戏上下文管理类，用于管理当前活动的架构上下文实例及其兼容类型别名。
/// </summary>
public static class GameContext
{
    // ConcurrentDictionary 负责向外暴露安全的实时视图；该锁负责维护“别名字典 + 当前活动上下文”之间的组合不变式。
#if NET9_0_OR_GREATER
    private static readonly Lock SyncRoot = new();
#else
    private static readonly object SyncRoot = new();
#endif
    private static readonly ConcurrentDictionary<Type, IArchitectureContext> ArchitectureDictionary
        = new();
    private static IArchitectureContext? _currentArchitectureContext;


    /// <summary>
    ///     获取所有已注册的架构上下文类型别名映射。
    ///     该只读视图会反映当前并发状态，不保证是稳定快照。
    /// </summary>
    public static IReadOnlyDictionary<Type, IArchitectureContext> ArchitectureReadOnlyDictionary =>
        ArchitectureDictionary;

    /// <summary>
    ///     绑定指定类型的架构上下文到管理器中。
    ///     同一时刻只允许存在一个活动上下文实例，但可以为其绑定多个兼容类型别名。
    /// </summary>
    /// <param name="architectureType">架构类型</param>
    /// <param name="context">架构上下文实例</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="architectureType" /> 或 <paramref name="context" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="InvalidOperationException">当指定类型的架构上下文已存在，或尝试绑定第二个不同上下文实例时抛出。</exception>
    public static void Bind(Type architectureType, IArchitectureContext context)
    {
        ArgumentNullException.ThrowIfNull(architectureType);
        ArgumentNullException.ThrowIfNull(context);

        lock (SyncRoot)
        {
            if (_currentArchitectureContext != null && !ReferenceEquals(_currentArchitectureContext, context))
                throw new InvalidOperationException(
                    $"GameContext already tracks active context '{_currentArchitectureContext.GetType().Name}'. " +
                    $"Cannot bind a different context '{context.GetType().Name}'.");

            if (!ArchitectureDictionary.TryAdd(architectureType, context))
                throw new InvalidOperationException(
                    $"Architecture context for '{architectureType.Name}' already exists");

            _currentArchitectureContext ??= context;
        }
    }

    /// <summary>
    ///     获取当前活动的架构上下文。
    ///     该方法保留原有名称以兼容存量调用方，但语义已经收敛为“当前上下文”，而不是任意字典首项。
    /// </summary>
    /// <returns>当前活动的架构上下文实例。</returns>
    /// <exception cref="InvalidOperationException">当当前没有活动上下文时抛出。</exception>
    public static IArchitectureContext GetFirstArchitectureContext()
    {
        lock (SyncRoot)
        {
            if (_currentArchitectureContext is { } context)
                return context;
        }

        throw new InvalidOperationException("No active architecture context is currently bound.");
    }

    /// <summary>
    ///     根据类型获取对应的架构上下文。
    ///     兼容层会优先查找显式绑定的类型别名，然后回退到当前上下文的类型兼容判断。
    /// </summary>
    /// <param name="type">要查找的架构类型</param>
    /// <returns>返回指定类型的架构上下文实例</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">当指定类型的架构上下文不存在时抛出</exception>
    public static IArchitectureContext GetByType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        lock (SyncRoot)
        {
            if (ArchitectureDictionary.TryGetValue(type, out var context))
                return context;

            if (_currentArchitectureContext != null && type.IsInstanceOfType(_currentArchitectureContext))
                return _currentArchitectureContext;
        }

        throw new InvalidOperationException(
            $"Architecture context for '{type.Name}' not found");
    }


    /// <summary>
    ///     获取指定类型的架构上下文实例。
    ///     该方法会优先复用当前活动上下文，再回退到显式注册的类型别名。
    /// </summary>
    /// <typeparam name="T">架构上下文类型，必须实现IArchitectureContext接口</typeparam>
    /// <returns>指定类型的架构上下文实例</returns>
    /// <exception cref="InvalidOperationException">当指定类型的架构上下文不存在时抛出</exception>
    public static T Get<T>() where T : class, IArchitectureContext
    {
        lock (SyncRoot)
        {
            if (_currentArchitectureContext is T currentContext)
                return currentContext;

            if (ArchitectureDictionary.TryGetValue(typeof(T), out var ctx))
                return (T)ctx;
        }

        throw new InvalidOperationException(
            $"Architecture context '{typeof(T).Name}' not found");
    }

    /// <summary>
    ///     尝试获取指定类型的架构上下文实例。
    ///     该方法会优先检查当前活动上下文是否兼容目标类型，再回退到显式注册的类型别名。
    /// </summary>
    /// <typeparam name="T">架构上下文类型，必须实现IArchitectureContext接口</typeparam>
    /// <param name="context">输出参数，如果找到则返回对应的架构上下文实例，否则返回null</param>
    /// <returns>如果找到指定类型的架构上下文则返回true，否则返回false</returns>
    public static bool TryGet<T>(out T? context)
        where T : class, IArchitectureContext
    {
        lock (SyncRoot)
        {
            if (_currentArchitectureContext is T currentContext)
            {
                context = currentContext;
                return true;
            }

            if (ArchitectureDictionary.TryGetValue(typeof(T), out var ctx))
            {
                context = (T)ctx;
                return true;
            }
        }

        context = null;
        return false;
    }

    /// <summary>
    ///     移除指定类型的架构上下文绑定。
    ///     当最后一个指向当前活动上下文的别名被移除时，也会同步清空当前活动上下文指针。
    /// </summary>
    /// <param name="architectureType">要移除的架构类型</param>
    /// <exception cref="ArgumentNullException"><paramref name="architectureType" /> 为 <see langword="null" />。</exception>
    public static void Unbind(Type architectureType)
    {
        ArgumentNullException.ThrowIfNull(architectureType);

        lock (SyncRoot)
        {
            if (!ArchitectureDictionary.TryRemove(architectureType, out var removedContext))
                return;

            if (_currentArchitectureContext == null || !ReferenceEquals(_currentArchitectureContext, removedContext))
                return;

            if (!HasAliasForContext(removedContext))
                _currentArchitectureContext = null;
        }
    }


    /// <summary>
    ///     清空所有架构上下文绑定，并重置当前活动上下文。
    /// </summary>
    public static void Clear()
    {
        lock (SyncRoot)
        {
            ArchitectureDictionary.Clear();
            _currentArchitectureContext = null;
        }
    }

    /// <summary>
    ///     判断当前是否仍存在指向同一上下文实例的其他类型别名。
    /// </summary>
    /// <param name="context">被移除绑定原本指向的上下文实例。</param>
    /// <returns>如果还有其他别名指向同一实例则返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    private static bool HasAliasForContext(IArchitectureContext context)
    {
        foreach (var current in ArchitectureDictionary.Values)
        {
            if (ReferenceEquals(current, context))
                return true;
        }

        return false;
    }
}
