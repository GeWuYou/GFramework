namespace GFramework.Core.SourceGenerators.Abstractions.Rule;

/// <summary>
///     标记类需要自动推断并注入上下文相关字段。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GetAllAttribute : Attribute
{
}
