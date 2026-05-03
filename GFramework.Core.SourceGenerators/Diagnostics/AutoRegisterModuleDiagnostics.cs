// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Core.SourceGenerators.Diagnostics;

internal static class AutoRegisterModuleDiagnostics
{
    private const string Category = $"{PathContests.SourceGeneratorsPath}.Architecture";

    public static readonly DiagnosticDescriptor NestedClassNotSupported = new(
        "GF_AutoModule_001",
        "AutoRegisterModule does not support nested classes",
        "AutoRegisterModule does not support nested class '{0}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor RegistrationTypeRequired = new(
        "GF_AutoModule_002",
        "Registration attribute requires a concrete type",
        "Attribute '{0}' on '{1}' requires a concrete type argument",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor RegistrationTypeMustImplementExpectedInterface = new(
        "GF_AutoModule_003",
        "Registration type does not implement the expected interface",
        "Type '{0}' used by '{1}' must implement '{2}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor RegistrationTypeMustHaveParameterlessConstructor = new(
        "GF_AutoModule_004",
        "Registration type must have an accessible parameterless constructor",
        "Type '{0}' used by '{1}' must have an accessible parameterless constructor",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor InstallMethodConflict = new(
        "GF_AutoModule_005",
        "Install method conflicts with generated code",
        "Class '{0}' already defines 'Install(IArchitecture)', which conflicts with AutoRegisterModule generated code",
        Category,
        DiagnosticSeverity.Error,
        true);
}
