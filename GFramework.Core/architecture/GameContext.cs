using System.Collections.Concurrent;
using GFramework.Core.Abstractions.architecture;

namespace GFramework.Core.architecture;

/// <summary>
/// 游戏上下文管理类，用于管理当前的架构上下文实例
/// </summary>
public static class GameContext
{
    private static readonly ConcurrentDictionary<Type, IArchitectureContext> ArchitectureDictionary
        = new();


    /// <summary>
    /// 获取所有已注册的架构上下文的只读字典
    /// </summary>
    public static IReadOnlyDictionary<Type, IArchitectureContext> ArchitectureReadOnlyDictionary =>
        ArchitectureDictionary;

    /// <summary>
    /// 绑定指定类型的架构上下文到管理器中
    /// </summary>
    /// <param name="architectureType">架构类型</param>
    /// <param name="context">架构上下文实例</param>
    /// <exception cref="InvalidOperationException">当指定类型的架构上下文已存在时抛出</exception>
    public static void Bind(Type architectureType, IArchitectureContext context)
    {
        if (!ArchitectureDictionary.TryAdd(architectureType, context))
        {
            throw new InvalidOperationException(
                $"Architecture context for '{architectureType.Name}' already exists");
        }
    }

    /// <summary>
    /// 获取字典中的第一个架构上下文
    /// </summary>
    /// <returns>返回字典中的第一个架构上下文实例</returns>
    /// <exception cref="InvalidOperationException">当字典为空时抛出</exception>
    public static IArchitectureContext GetFirstArchitectureContext()
    {
        return ArchitectureDictionary.Values.First();
    }

    /// <summary>
    /// 根据类型获取对应的架构上下文
    /// </summary>
    /// <param name="type">要查找的架构类型</param>
    /// <returns>返回指定类型的架构上下文实例</returns>
    /// <exception cref="InvalidOperationException">当指定类型的架构上下文不存在时抛出</exception>
    public static IArchitectureContext GetByType(Type type)
    {
        if (ArchitectureDictionary.TryGetValue(type, out var context))
            return context;

        throw new InvalidOperationException(
            $"Architecture context for '{type.Name}' not found");
    }


    /// <summary>
    /// 获取指定类型的架构上下文实例
    /// </summary>
    /// <typeparam name="T">架构上下文类型，必须实现IArchitectureContext接口</typeparam>
    /// <returns>指定类型的架构上下文实例</returns>
    /// <exception cref="InvalidOperationException">当指定类型的架构上下文不存在时抛出</exception>
    public static T Get<T>() where T : class, IArchitectureContext
    {
        if (ArchitectureDictionary.TryGetValue(typeof(T), out var ctx))
            return (T)ctx;

        throw new InvalidOperationException(
            $"Architecture context '{typeof(T).Name}' not found");
    }

    /// <summary>
    /// 尝试获取指定类型的架构上下文实例
    /// </summary>
    /// <typeparam name="T">架构上下文类型，必须实现IArchitectureContext接口</typeparam>
    /// <param name="context">输出参数，如果找到则返回对应的架构上下文实例，否则返回null</param>
    /// <returns>如果找到指定类型的架构上下文则返回true，否则返回false</returns>
    public static bool TryGet<T>(out T? context)
        where T : class, IArchitectureContext
    {
        if (ArchitectureDictionary.TryGetValue(typeof(T), out var ctx))
        {
            context = (T)ctx;
            return true;
        }

        context = null;
        return false;
    }

    /// <summary>
    /// 移除指定类型的架构上下文绑定
    /// </summary>
    /// <param name="architectureType">要移除的架构类型</param>
    public static void Unbind(Type architectureType)
    {
        ArchitectureDictionary.TryRemove(architectureType, out _);
    }


    /// <summary>
    /// 清空所有架构上下文绑定
    /// </summary>
    public static void Clear()
    {
        ArchitectureDictionary.Clear();
    }
}