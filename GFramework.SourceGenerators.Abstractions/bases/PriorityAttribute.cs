namespace GFramework.SourceGenerators.Abstractions.bases;

/// <summary>
/// 标记类的优先级，自动生成 <see cref="GFramework.Core.Abstractions.bases.IPrioritized"/> 接口实现
/// </summary>
/// <remarks>
/// 使用此特性可以避免手动实现 IPrioritized 接口。
/// 优先级值越小，优先级越高（负数表示高优先级）。
/// </remarks>
/// <example>
/// <code>
/// [Priority(10)]
/// public partial class MySystem : AbstractSystem
/// {
///     // 自动生成: public int Priority => 10;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PriorityAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="PriorityAttribute"/> 类的新实例
    /// </summary>
    /// <param name="value">优先级值，越小优先级越高</param>
    public PriorityAttribute(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 获取优先级值
    /// </summary>
    public int Value { get; }
}