using System.Collections.Generic;
using System.Linq;
using GFramework.SourceGenerators.Common.info;
using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.Common.extensions;

/// <summary>
/// 提供INamedTypeSymbol类型的扩展方法
/// </summary>
public static class INamedTypeSymbolExtensions
{
    /// <summary>
    /// 根据类型种类解析对应的类型关键字
    /// </summary>
    /// <param name="symbol">要解析类型的命名类型符号</param>
    /// <returns>对应类型的字符串表示，如"class"、"struct"或"record"</returns>
    public static string ResolveTypeKind(this INamedTypeSymbol symbol)
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

    /// <summary>
    /// 解析泛型信息，包括泛型参数和约束条件
    /// </summary>
    /// <param name="symbol">要解析泛型信息的命名类型符号</param>
    /// <returns>包含泛型参数和约束条件的GenericInfo对象</returns>
    public static GenericInfo ResolveGenerics(this INamedTypeSymbol symbol)
    {
        if (symbol.TypeParameters.Length == 0)
            return new GenericInfo(string.Empty, []);

        // 构建泛型参数列表
        var parameters =
            "<" + string.Join(", ", symbol.TypeParameters.Select(tp => tp.Name)) + ">";

        // 构建泛型约束条件列表
        var constraints = symbol.TypeParameters
            .Select(BuildConstraint)
            .Where(c => c != null)
            .Cast<string>()
            .ToList();

        return new GenericInfo(parameters, constraints);
    }

    /// <summary>
    /// 构建单个类型参数的约束条件字符串
    /// </summary>
    /// <param name="tp">类型参数符号</param>
    /// <returns>约束条件的字符串表示，如果没有约束则返回null</returns>
    private static string? BuildConstraint(ITypeParameterSymbol tp)
    {
        var parts = new List<string>();

        if (tp.HasReferenceTypeConstraint) parts.Add("class");
        if (tp.HasValueTypeConstraint) parts.Add("struct");
        parts.AddRange(tp.ConstraintTypes.Select(t => t.ToDisplayString()));
        if (tp.HasConstructorConstraint) parts.Add("new()");

        return parts.Count == 0
            ? null
            : $"where {tp.Name} : {string.Join(", ", parts)}";
    }

    /// <param name="symbol">要获取完整类名的命名类型符号</param>
    extension(INamedTypeSymbol symbol)
    {
        /// <summary>
        /// 获取命名类型符号的完整类名（包括嵌套类型名称）
        /// </summary>
        /// <returns>完整的类名，格式为"外层类名.内层类名.当前类名"</returns>
        public string GetFullClassName()
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
        /// <returns>命名空间名称，如果是全局命名空间则返回null</returns>
        public string? GetNamespace()
        {
            return symbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : symbol.ContainingNamespace.ToDisplayString();
        }
    }
}