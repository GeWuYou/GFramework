#nullable enable
namespace GFramework.SourceGenerators.Attributes.logging;

/// <summary>
/// Godot日志特性，用于在类上标记以自动生成日志字段
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GodotLogAttribute : Attribute
{
    /// <summary>
    /// 初始化 GodotLogAttribute 类的新实例
    /// </summary>
    public GodotLogAttribute()
    {
    }

    /// <summary>
    /// 初始化 GodotLogAttribute 类的新实例
    /// </summary>
    /// <param name="name">日志分类名</param>
    public GodotLogAttribute(string? name)
    {
        Name = name;
    }

    /// <summary>日志分类名（默认使用类名）</summary>
    public string? Name { get; set; }

    /// <summary>生成字段名</summary>
    public string FieldName { get; set; } = "_log";

    /// <summary>是否生成 static 字段</summary>
    public bool IsStatic { get; set; } = true;

    /// <summary>访问修饰符</summary>
    public string AccessModifier { get; set; } = "private";
}