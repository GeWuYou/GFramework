#nullable enable
using System;

namespace GFramework.SourceGenerators.Attributes.logging;

/// <summary>
/// 标注在类上，Source Generator 会为该类自动生成一个日志记录器字段。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LogAttribute : Attribute
{
    public LogAttribute()
    {
    }

    /// <summary>
    /// 初始化 GodotLogAttribute 类的新实例
    /// </summary>
    /// <param name="name">日志分类名，默认使用类名</param>
    public LogAttribute(string? name)
    {
        Name = name;
    }

    /// <summary>日志分类名（默认使用类名）</summary>
    public string? Name { get; set; }

    /// <summary>生成字段名</summary>
    public string FieldName { get; set; } = "Logger";

    /// <summary>是否生成 static 字段</summary>
    public bool IsStatic { get; set; } = true;

    /// <summary>访问修饰符</summary>
    public string AccessModifier { get; set; } = "private";
}