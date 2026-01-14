using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.Common.extensions;

/// <summary>
/// 提供INamedTypeSymbol类型的扩展方法
/// </summary>
public static class INamedTypeSymbolExtensions
{
    /// <summary>
    /// 获取命名类型符号的完整类名（包括嵌套类型名称）
    /// </summary>
    /// <param name="symbol">要获取完整类名的命名类型符号</param>
    /// <returns>完整的类名，格式为"外层类名.内层类名.当前类名"</returns>
    public static string GetFullClassName(this INamedTypeSymbol symbol)
    {
        var names = new Stack<string>();
        var current = symbol;

        // 遍历包含类型链，将所有类型名称压入栈中
        while (current != null)
        {
            names.Push(current.Name);
            current = current.ContainingType;
        }

        // 将栈中的名称用点号连接，形成完整的类名
        return string.Join(".", names);
    }

    /// <summary>
    /// 获取命名类型符号的命名空间名称
    /// </summary>
    /// <param name="symbol">要获取命名空间的命名类型符号</param>
    /// <returns>命名空间名称，如果是全局命名空间则返回null</returns>
    private static string? GetNamespace(this INamedTypeSymbol symbol)
    {
        return symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();
    }

    /// <summary>
    /// 根据类型种类解析对应的类型关键字
    /// </summary>
    /// <param name="symbol">要解析类型的命名类型符号</param>
    /// <returns>对应类型的字符串表示，如"class"、"struct"或"record"</returns>
    private static string ResolveTypeKind(this INamedTypeSymbol symbol)
    {
        return symbol.TypeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Struct => "struct",
#if NET5_0_OR_GREATER || ROSLYN_3_7_OR_GREATER
            TypeKind.Record => "record",
#endif
            _ => "class",
        };
    }
}