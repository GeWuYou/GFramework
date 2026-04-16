namespace GFramework.Core.SourceGenerators.Abstractions.Rule;

/// <summary>
///     标记字段需要自动注入单个工具实例。
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class GetUtilityAttribute : Attribute
{
}
