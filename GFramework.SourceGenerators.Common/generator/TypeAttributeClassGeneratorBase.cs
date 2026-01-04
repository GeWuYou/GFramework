using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.Common.generator;

/// <summary>
///     基于类型特性的类生成器基类
/// </summary>
public abstract class TypeAttributeClassGeneratorBase
    : AttributeClassGeneratorBase
{
    /// <summary>
    ///     获取要处理的特性类型
    /// </summary>
    protected abstract Type AttributeType { get; }

    /// <summary>
    ///     解析指定符号上的特性
    /// </summary>
    /// <param name="compilation">编译对象（未使用）</param>
    /// <param name="symbol">要检查的命名类型符号</param>
    /// <returns>匹配的特性数据，如果未找到则返回null</returns>
    protected override AttributeData? ResolveAttribute(
        Compilation compilation,
        INamedTypeSymbol symbol)
    {
        var fullName = AttributeType.FullName;

        // 查找符号上匹配指定特性的第一个实例
        return symbol.GetAttributes()
            .FirstOrDefault(a =>
                string.Equals(a.AttributeClass?.ToDisplayString(), fullName, StringComparison.Ordinal));
    }
}