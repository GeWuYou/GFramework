using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Godot.SourceGenerators.Diagnostics;

internal static class AutoRegisterExportedCollectionsDiagnostics
{
    private const string Category = $"{PathContests.GodotNamespace}.SourceGenerators.Registration";

    public static readonly DiagnosticDescriptor NestedClassNotSupported = new(
        "GF_AutoExport_001",
        "AutoRegisterExportedCollections does not support nested classes",
        "AutoRegisterExportedCollections does not support nested class '{0}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor RegistryMemberNotFound = new(
        "GF_AutoExport_002",
        "Registry member was not found",
        "Member '{0}' referenced by exported collection '{1}' was not found on '{2}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor RegisterMethodNotFound = new(
        "GF_AutoExport_003",
        "Register method was not found",
        "Method '{0}' was not found on registry member '{1}' for exported collection '{2}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor CollectionTypeMustBeEnumerable = new(
        "GF_AutoExport_004",
        "Exported collection must be enumerable",
        "Member '{0}' must be enumerable to use RegisterExportedCollection",
        Category,
        DiagnosticSeverity.Error,
        true);
}
