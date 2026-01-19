namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程优先级枚举，定义协程执行的优先级顺序
/// </summary>
/// <remarks>
///     优先级数值越小，执行优先级越高
///     CoroutineScheduler将按照优先级顺序调度协程
/// </remarks>
public enum CoroutinePriority
{
    /// <summary>
    ///     关键优先级 - 最高优先级，用于必须立即执行的协程
    /// </summary>
    Critical = 0,

    /// <summary>
    ///     高优先级 - 用于需要快速响应的协程
    /// </summary>
    High = 100,

    /// <summary>
    ///     普通优先级 - 默认优先级，用于常规协程
    /// </summary>
    Normal = 200,

    /// <summary>
    ///     低优先级 - 用于不紧急的后台协程
    /// </summary>
    Low = 300,

    /// <summary>
    ///     背景优先级 - 最低优先级，用于非关键的后台任务
    /// </summary>
    Background = 400,

    /// <summary>
    ///     继承优先级 - 表示从父作用域继承优先级
    /// </summary>
    Inherit = -1
}