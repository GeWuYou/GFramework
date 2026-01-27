namespace GFramework.Core.extensions;

/// <summary>
///     提供基于运行时类型判断的对象扩展方法，
///     用于简化类型分支、链式调用和架构分派逻辑。
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    ///     当对象是指定类型 <typeparamref name="T" /> 时，执行给定的操作。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="obj">源对象</param>
    /// <param name="action">当对象类型匹配时执行的操作</param>
    /// <returns>如果类型匹配并执行了操作则返回 true，否则返回 false</returns>
    /// <example>
    ///     <code>
    /// object obj = new MyRule();
    /// 
    /// bool executed = obj.IfType&lt;MyRule&gt;(rule =>
    /// {
    ///     rule.Initialize();
    /// });
    /// </code>
    /// </example>
    public static bool IfType<T>(this object obj, Action<T> action)
    {
        if (obj is not T target) return false;
        action(target);
        return true;
    }

    /// <summary>
    ///     当对象是指定类型 <typeparamref name="T" /> 时，
    ///     使用给定函数计算并返回结果；否则返回默认值。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <typeparam name="TResult">返回结果类型</typeparam>
    /// <param name="obj">源对象</param>
    /// <param name="func">当类型匹配时执行的函数</param>
    /// <returns>
    ///     类型匹配时返回函数计算结果，否则返回 <c>default</c>
    /// </returns>
    /// <example>
    ///     <code>
    /// object obj = new MyRule { Name = "TestRule" };
    /// 
    /// string? name = obj.IfType&lt;MyRule, string&gt;(r => r.Name);
    /// </code>
    /// </example>
    public static TResult? IfType<T, TResult>(
        this object obj,
        Func<T, TResult> func
    )
        where T : class
    {
        return obj is T target ? func(target) : default;
    }

    /// <summary>
    ///     根据对象是否为指定类型 <typeparamref name="T" />，
    ///     分别执行匹配或不匹配的操作。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="obj">源对象</param>
    /// <param name="whenMatch">当对象类型匹配时执行的操作</param>
    /// <param name="whenNotMatch">当对象类型不匹配时执行的操作</param>
    /// <example>
    ///     <code>
    /// obj.IfType&lt;IRule&gt;(
    ///     rule => rule.Execute(),
    ///     other => Logger.Warn($"Unsupported type: {other.GetType()}")
    /// );
    /// </code>
    /// </example>
    public static void IfType<T>(
        this object obj,
        Action<T> whenMatch,
        Action<object>? whenNotMatch
    )
        where T : class
    {
        if (obj is T target)
            whenMatch(target);
        else
            whenNotMatch?.Invoke(obj);
    }

    /// <summary>
    ///     当对象是指定类型 <typeparamref name="T" /> 且满足给定条件时，
    ///     执行指定操作。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="obj">源对象</param>
    /// <param name="predicate">对目标类型对象的条件判断</param>
    /// <param name="action">当条件满足时执行的操作</param>
    /// <returns>如果类型和条件均匹配并执行了操作则返回 true，否则返回 false</returns>
    /// <example>
    ///     <code>
    /// obj.IfType&lt;MyRule&gt;(
    ///     r => r.Enabled,
    ///     r => r.Execute()
    /// );
    /// </code>
    /// </example>
    public static bool IfType<T>(
        this object obj,
        Func<T, bool> predicate,
        Action<T> action
    )
        where T : class
    {
        if (obj is not T target || !predicate(target))
            return false;

        action(target);
        return true;
    }

    /// <summary>
    ///     尝试将对象转换为指定的引用类型 <typeparamref name="T" />。
    /// </summary>
    /// <typeparam name="T">目标引用类型</typeparam>
    /// <param name="obj">源对象</param>
    /// <returns>
    ///     如果类型匹配则返回转换后的对象，否则返回 <c>null</c>
    /// </returns>
    /// <example>
    ///     <code>
    /// obj.As&lt;MyRule&gt;()
    ///    ?.Execute();
    /// </code>
    /// </example>
    public static T? As<T>(this object obj) where T : class
    {
        return obj as T;
    }

    /// <summary>
    ///     对对象执行指定操作后返回对象本身，
    ///     用于构建流式（链式）调用。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">源对象</param>
    /// <param name="action">要执行的操作</param>
    /// <returns>原对象</returns>
    /// <example>
    ///     <code>
    /// obj.As&lt;MyRule&gt;()
    ///    ?.Do(r => r.Initialize())
    ///    ?.Do(r => r.Execute());
    /// </code>
    /// </example>
    public static T Do<T>(this T obj, Action<T> action)
    {
        action(obj);
        return obj;
    }

    /// <summary>
    ///     根据对象的运行时类型，依次匹配并执行对应的处理逻辑，
    ///     只会执行第一个匹配成功的处理器。
    /// </summary>
    /// <param name="obj">源对象</param>
    /// <param name="handlers">
    ///     类型与处理操作的元组数组，用于定义类型分派规则
    /// </param>
    /// <example>
    ///     <code>
    /// obj.SwitchType(
    ///     (typeof(IRule), o => HandleRule((IRule)o)),
    ///     (typeof(ISystem), o => HandleSystem((ISystem)o))
    /// );
    /// </code>
    /// </example>
    public static void SwitchType(
        this object obj,
        params (Type type, Action<object> action)[] handlers
    )
    {
        foreach (var (type, action) in handlers)
        {
            if (!type.IsInstanceOfType(obj)) continue;
            action(obj);
            return;
        }
    }
}