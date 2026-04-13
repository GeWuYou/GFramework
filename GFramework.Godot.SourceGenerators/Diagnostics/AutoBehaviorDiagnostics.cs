using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Godot.SourceGenerators.Diagnostics;

internal static class AutoBehaviorDiagnostics
{
    private const string Category = $"{PathContests.GodotNamespace}.SourceGenerators.Behavior";

    public static readonly DiagnosticDescriptor NestedClassNotSupported = new(
        "GF_AutoBehavior_001",
        "Auto behavior generators do not support nested classes",
        "Generator '{0}' does not support nested class '{1}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MissingBaseType = new(
        "GF_AutoBehavior_002",
        "Auto behavior generators require a compatible base type",
        "Type '{0}' must inherit from '{1}' to use '{2}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor InvalidUiLayerName = new(
        "GF_AutoBehavior_003",
        "Unknown UiLayer name",
        "Ui layer '{0}' on '{1}' does not exist on GFramework.Game.Abstractions.Enums.UiLayer",
        Category,
        DiagnosticSeverity.Error,
        true);
}
