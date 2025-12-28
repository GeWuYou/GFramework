namespace GFramework.SourceGenerators.Common.constants;

/// <summary>
/// 定义GFramework项目中使用的路径常量
/// </summary>
public static class PathContests
{
    /// <summary>
    /// GFramework基础命名空间
    /// </summary>
    public const string BaseNamespace = "GFramework";

    /// <summary>
    /// GFramework核心模块命名空间
    /// </summary>
    public const string CoreNamespace = $"{BaseNamespace}.Core";

    /// <summary>
    /// GFramework Godot模块命名空间
    /// </summary>
    public const string GodotNamespace = $"{BaseNamespace}.Godot";

    /// <summary>
    /// GFramework游戏模块命名空间
    /// </summary>
    public const string GameNamespace = $"{BaseNamespace}.Game";

    /// <summary>
    /// GFramework源代码生成器抽象层命名空间
    /// </summary>
    public const string SourceGeneratorsAbstractionsPath = $"{BaseNamespace}.SourceGenerators.Abstractions";

    /// <summary>
    /// GFramework Godot源代码生成器抽象层命名空间
    /// </summary>
    public const string GodotSourceGeneratorsAbstractionsPath = $"{GodotNamespace}.SourceGenerators.Abstractions";

    /// <summary>
    /// GFramework核心抽象层命名空间
    /// </summary>
    public const string CoreAbstractionsNamespace = $"{CoreNamespace}.Abstractions";
}