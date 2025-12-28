using System.Linq;
using GFramework.SourceGenerators.Common.diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GFramework.SourceGenerators.Common.generator;

/// <summary>
///     属性类生成器基类，用于处理带有特定属性的类并生成相应的源代码
/// </summary>
public abstract class AttributeClassGeneratorBase : IIncrementalGenerator
{
    /// <summary>
    ///     获取属性的短名称（不包含后缀）
    /// </summary>
    protected abstract string AttributeShortNameWithoutSuffix { get; }

    /// <summary>
    ///     初始化增量生成器
    /// </summary>
    /// <param name="context">增量生成器初始化上下文</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 创建语法提供程序，查找带有指定属性的类声明
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) =>
                    node is ClassDeclarationSyntax cls &&
                    cls.AttributeLists
                        .SelectMany(a => a.Attributes)
                        .Any(a => a.Name.ToString()
                            .Contains(AttributeShortNameWithoutSuffix)),
                (ctx, _) =>
                {
                    var cls = (ClassDeclarationSyntax)ctx.Node;
                    var symbol = ctx.SemanticModel.GetDeclaredSymbol(cls);
                    return (cls, symbol);
                })
            .Where(x => x.symbol is not null);

        var combined = candidates.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, (spc, pair) =>
        {
            var ((cls, symbol), compilation) = pair;
            Execute(spc, compilation, cls, symbol!);
        });
    }

    /// <summary>
    ///     解析指定符号上的属性数据
    /// </summary>
    /// <param name="compilation">编译对象</param>
    /// <param name="symbol">命名类型符号</param>
    /// <returns>属性数据，如果未找到则返回null</returns>
    protected abstract AttributeData? ResolveAttribute(
        Compilation compilation,
        INamedTypeSymbol symbol);

    /// <summary>
    ///     执行源代码生成
    /// </summary>
    /// <param name="context">源生产上下文</param>
    /// <param name="compilation">编译对象</param>
    /// <param name="classDecl">类声明语法节点</param>
    /// <param name="symbol">命名类型符号</param>
    private void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ClassDeclarationSyntax classDecl,
        INamedTypeSymbol symbol)
    {
        var attr = ResolveAttribute(compilation, symbol);
        if (attr is null)
            return;

        if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            ReportClassMustBePartial(context, classDecl, symbol);
            return;
        }

        if (!ValidateSymbol(context, classDecl, symbol, attr))
            return;

        context.AddSource(GetHintName(symbol), Generate(symbol, attr));
    }

    /// <summary>
    ///     验证符号的有效性
    /// </summary>
    /// <param name="context">源生产上下文</param>
    /// <param name="syntax">类声明语法节点</param>
    /// <param name="symbol">命名类型符号</param>
    /// <param name="attr">属性数据</param>
    /// <returns>验证是否通过</returns>
    protected virtual bool ValidateSymbol(
        SourceProductionContext context,
        ClassDeclarationSyntax syntax,
        INamedTypeSymbol symbol,
        AttributeData attr) => true;

    /// <summary>
    ///     生成源代码
    /// </summary>
    /// <param name="symbol">命名类型符号</param>
    /// <param name="attr">属性数据</param>
    /// <returns>生成的源代码字符串</returns>
    protected abstract string Generate(
        INamedTypeSymbol symbol,
        AttributeData attr);

    /// <summary>
    ///     获取生成文件的提示名称
    /// </summary>
    /// <param name="symbol">命名类型符号</param>
    /// <returns>生成文件的提示名称</returns>
    protected virtual string GetHintName(INamedTypeSymbol symbol)
        => $"{symbol.Name}.g.cs";

    /// <summary>
    ///     报告类必须是部分类的错误
    /// </summary>
    /// <param name="context">源生产上下文</param>
    /// <param name="syntax">类声明语法节点</param>
    /// <param name="symbol">命名类型符号</param>
    protected virtual void ReportClassMustBePartial(
        SourceProductionContext context,
        ClassDeclarationSyntax syntax,
        INamedTypeSymbol symbol)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            CommonDiagnostics.ClassMustBePartial,
            syntax.Identifier.GetLocation(),
            symbol.Name));
    }
}