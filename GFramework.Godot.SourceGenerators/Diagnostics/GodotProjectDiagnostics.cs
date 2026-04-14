using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Godot.SourceGenerators.Diagnostics;

/// <summary>
///     基于 <c>project.godot</c> 的项目元数据生成相关诊断。
/// </summary>
public static class GodotProjectDiagnostics
{
    /// <summary>
    ///     标记了 <c>[AutoLoad]</c> 的类型必须继承自 <c>Godot.Node</c>。
    /// </summary>
    public static readonly DiagnosticDescriptor AutoLoadTypeMustDeriveFromNode = new(
        "GF_Godot_Project_001",
        "AutoLoad types must derive from Godot.Node",
        "Type '{0}' uses [AutoLoad] but does not derive from Godot.Node",
        PathContests.GodotNamespace,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     多个类型映射到同一 AutoLoad 名称时会退化为非强类型访问。
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateAutoLoadMapping = new(
        "GF_Godot_Project_002",
        "Duplicate AutoLoad mappings were found",
        "AutoLoad '{0}' is mapped by multiple types ({1}); the generated accessor falls back to Godot.Node until the mapping is unique",
        PathContests.GodotNamespace,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     多个 AutoLoad 名称映射到同一个标识符时会追加稳定后缀。
    /// </summary>
    public static readonly DiagnosticDescriptor AutoLoadIdentifierCollision = new(
        "GF_Godot_Project_003",
        "Generated AutoLoad identifier collision",
        "AutoLoad '{0}' collides with another generated identifier '{1}'; a stable numeric suffix was appended",
        PathContests.GodotNamespace,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     多个 Input Action 名称映射到同一个标识符时会追加稳定后缀。
    /// </summary>
    public static readonly DiagnosticDescriptor InputActionIdentifierCollision = new(
        "GF_Godot_Project_004",
        "Generated Input Action identifier collision",
        "Input action '{0}' collides with another generated identifier '{1}'; a stable numeric suffix was appended",
        PathContests.GodotNamespace,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     同一个 <c>project.godot</c> 中存在重复 AutoLoad 条目。
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateAutoLoadEntry = new(
        "GF_Godot_Project_005",
        "Duplicate AutoLoad entry in project.godot",
        "AutoLoad '{0}' is declared multiple times in project.godot; only the first declaration is used",
        PathContests.GodotNamespace,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     同一个 <c>project.godot</c> 中存在重复 Input Action 条目。
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateInputActionEntry = new(
        "GF_Godot_Project_006",
        "Duplicate Input Action entry in project.godot",
        "Input action '{0}' is declared multiple times in project.godot; only the first declaration is used",
        PathContests.GodotNamespace,
        DiagnosticSeverity.Warning,
        true);
}
