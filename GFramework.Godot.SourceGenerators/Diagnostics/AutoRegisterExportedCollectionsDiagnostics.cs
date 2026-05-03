// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Godot.SourceGenerators.Diagnostics;

/// <summary>
///     定义导出集合自动注册生成器使用的诊断描述符。
/// </summary>
/// <remarks>
///     这些规则用于在源生成阶段验证集合成员、注册目标以及元素类型推导，
///     避免把配置错误延后到生成代码编译或运行时才暴露。
/// </remarks>
internal static class AutoRegisterExportedCollectionsDiagnostics
{
    private const string Category = $"{PathContests.GodotNamespace}.SourceGenerators.Registration";

    /// <summary>
    ///     报告自动注册生成器不支持嵌套类型。
    /// </summary>
    public static readonly DiagnosticDescriptor NestedClassNotSupported = new(
        "GF_AutoExport_001",
        "AutoRegisterExportedCollections does not support nested classes",
        "AutoRegisterExportedCollections does not support nested class '{0}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     报告特性引用的注册表成员在宿主类型上不存在。
    /// </summary>
    public static readonly DiagnosticDescriptor RegistryMemberNotFound = new(
        "GF_AutoExport_002",
        "Registry member was not found",
        "Member '{0}' referenced by exported collection '{1}' was not found on '{2}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     报告注册表上未找到与集合元素类型兼容的注册方法。
    /// </summary>
    public static readonly DiagnosticDescriptor RegisterMethodNotFound = new(
        "GF_AutoExport_003",
        "Register method was not found",
        "Method '{0}' was not found on registry member '{1}' for exported collection '{2}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     报告被标记成员不是可枚举集合，因此无法执行批量注册。
    /// </summary>
    public static readonly DiagnosticDescriptor CollectionTypeMustBeEnumerable = new(
        "GF_AutoExport_004",
        "Exported collection must be enumerable",
        "Member '{0}' must be enumerable to use RegisterExportedCollection",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     报告集合元素类型无法在编译期推导，因此无法安全匹配注册方法。
    /// </summary>
    public static readonly DiagnosticDescriptor CollectionElementTypeCouldNotBeInferred = new(
        "GF_AutoExport_005",
        "Exported collection element type could not be inferred",
        "Member '{0}' must expose a generic enumerable element type to use RegisterExportedCollection safely",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     报告被标记为导出集合的成员不是实例可读成员，因此无法生成 <c>this.&lt;member&gt;</c> 访问代码。
    /// </summary>
    public static readonly DiagnosticDescriptor CollectionMemberMustBeInstanceReadable = new(
        "GF_AutoExport_006",
        "Exported collection member must be an instance readable member",
        "Member '{0}' must be an instance field or readable non-indexer instance property to use RegisterExportedCollection",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     报告注册表成员不是实例可读成员，因此生成器无法安全读取并调用注册方法。
    /// </summary>
    public static readonly DiagnosticDescriptor RegistryMemberMustBeInstanceReadable = new(
        "GF_AutoExport_007",
        "Registry member must be an instance readable member",
        "Registry member '{0}' referenced by exported collection '{1}' must be an instance field or readable non-indexer instance property",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     报告 <c>RegisterExportedCollectionAttribute</c> 构造参数不满足约定，导致无法解析注册目标成员与方法名。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidAttributeArguments = new(
        "GF_AutoExport_008",
        "RegisterExportedCollection attribute arguments are invalid",
        "Attribute 'RegisterExportedCollectionAttribute' on member '{0}' must provide a string registry member name and a string register method name",
        Category,
        DiagnosticSeverity.Error,
        true);
}
