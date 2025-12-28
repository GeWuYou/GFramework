using System.Linq;
using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.Common.generator;

/// <summary>
/// 元数据属性类生成器基类，用于基于元数据名称解析特性的抽象基类
/// </summary>
public abstract class MetadataAttributeClassGeneratorBase
    : AttributeClassGeneratorBase
{
    /// <summary>
    /// 获取特性元数据名称的抽象属性
    /// </summary>
    protected abstract string AttributeMetadataName { get; }

    /// <summary>
    /// 根据元数据名称解析指定符号上的特性
    /// </summary>
    /// <param name="compilation">编译对象，用于获取类型信息</param>
    /// <param name="symbol">命名类型符号，用于查找其上的特性</param>
    /// <returns>如果找到匹配的特性则返回AttributeData对象，否则返回null</returns>
    protected override AttributeData? ResolveAttribute(
        Compilation compilation,
        INamedTypeSymbol symbol)
    {
        // 通过元数据名称获取特性符号
        var attrSymbol =
            compilation.GetTypeByMetadataName(AttributeMetadataName);

        if (attrSymbol is null)
            return null;

        // 在符号的所有特性中查找与目标特性符号匹配的第一个特性
        return symbol.GetAttributes()
            .FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(
                    a.AttributeClass,
                    attrSymbol));
    }
}