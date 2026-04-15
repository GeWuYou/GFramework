namespace GFramework.Godot.SourceGenerators.Abstractions.UI;

/// <summary>
///     标记类型允许为带映射特性的导出集合生成批量注册代码。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoRegisterExportedCollectionsAttribute : Attribute
{
}
