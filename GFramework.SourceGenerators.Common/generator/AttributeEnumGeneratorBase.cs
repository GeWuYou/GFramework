using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GFramework.SourceGenerators.Common.generator;

/// <summary>
/// 基于特性的枚举生成器基类
/// </summary>
/// <typeparam name="TAttribute">特性类型，必须继承自Attribute</typeparam>
public abstract class AttributeEnumGeneratorBase<TAttribute> : IIncrementalGenerator
    where TAttribute : Attribute
{
    /// <summary>
    /// 获取特性的短名称（不包含后缀）
    /// </summary>
    protected abstract string AttributeShortNameWithoutSuffix { get; }

    /// <summary>
    /// 初始化增量生成器
    /// </summary>
    /// <param name="context">增量生成器初始化上下文</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 查找带有指定特性的枚举声明
        var enums = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) => node is EnumDeclarationSyntax decl &&
                         decl.AttributeLists.SelectMany(a => a.Attributes)
                             .Any(a => a.Name.ToString().Contains(AttributeShortNameWithoutSuffix)),
            (ctx, _) =>
            {
                var decl = (EnumDeclarationSyntax)ctx.Node;
                var symbol = ctx.SemanticModel.GetDeclaredSymbol(decl) as INamedTypeSymbol;
                return (decl, symbol);
            }).Where(x => x.symbol != null);

        // 注册源代码输出
        context.RegisterSourceOutput(enums, (spc, pair) =>
        {
            var attr = pair.symbol!.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == typeof(TAttribute).FullName);
            if (attr == null) return;

            var src = Generate(pair.symbol!, attr);
            spc.AddSource($"{pair.symbol!.Name}.EnumExtensions.g.cs", SourceText.From(src, Encoding.UTF8));
        });
    }

    /// <summary>
    /// 生成源代码
    /// </summary>
    /// <param name="enumSymbol">枚举符号</param>
    /// <param name="attr">特性数据</param>
    /// <returns>生成的源代码字符串</returns>
    protected abstract string Generate(INamedTypeSymbol enumSymbol, AttributeData attr);
}