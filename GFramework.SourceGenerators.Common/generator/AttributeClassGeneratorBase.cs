using System;
using System.Linq;
using GFramework.SourceGenerators.Common.diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GFramework.SourceGenerators.Common.generator;

/// <summary>
/// 属性类生成器基类，用于处理带有特定属性的类并生成相应的源代码
/// </summary>
public abstract class AttributeClassGeneratorBase : IIncrementalGenerator
{
    /// <summary>
    /// 获取属性的元数据名称
    /// </summary>
    protected abstract Type AttributeType { get; }

    /// <summary>
    /// Attribute 的短名称（不含 Attribute 后缀）
    /// 仅用于 Syntax 层宽松匹配
    /// </summary>
    protected abstract string AttributeShortNameWithoutSuffix { get; }

    /// <summary>
    /// 初始化增量生成器
    /// </summary>
    /// <param name="context">增量生成器初始化上下文</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) =>
                    node is ClassDeclarationSyntax cls &&
                    cls.AttributeLists
                        .SelectMany(a => a.Attributes)
                        .Any(a => a.Name.ToString()
                            .Contains(AttributeShortNameWithoutSuffix)),
                transform: static (ctx, t) =>
                {
                    var cls = (ClassDeclarationSyntax)ctx.Node;
                    var symbol = ctx.SemanticModel.GetDeclaredSymbol(cls, cancellationToken: t);
                    return (ClassDecl: cls, Symbol: symbol);
                }
            )
            .Where(x => x.Symbol is not null);

        context.RegisterSourceOutput(targets, (spc, pair) =>
        {
            try
            {
                Execute(spc, pair.ClassDecl, pair.Symbol!);
            }
            catch (Exception ex)
            {
                EmitError(spc, pair.Symbol, ex);
            }
        });
    }

    /// <summary>
    /// 执行源代码生成的主要逻辑
    /// </summary>
    /// <param name="context">源生产上下文</param>
    /// <param name="classDecl">类声明语法节点</param>
    /// <param name="symbol">命名类型符号</param>
    private void Execute(
        SourceProductionContext context,
        ClassDeclarationSyntax classDecl,
        INamedTypeSymbol symbol)
    {
        var attr = GetAttribute(symbol);
        if (attr == null) return;

        // partial 校验
        if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            ReportClassMustBePartial(context, classDecl, symbol);
            return;
        }

        // 子类校验
        if (!ValidateSymbol(context, classDecl, symbol, attr))
            return;

        var source = Generate(symbol, attr);
        context.AddSource(GetHintName(symbol), source);
    }

    #region 可覆写点

    /// <summary>
    /// 验证符号的有效性
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
        AttributeData attr)
        => true;

    /// <summary>
    /// 生成源代码
    /// </summary>
    /// <param name="symbol">命名类型符号</param>
    /// <param name="attr">属性数据</param>
    /// <returns>生成的源代码字符串</returns>
    protected abstract string Generate(
        INamedTypeSymbol symbol,
        AttributeData attr);

    /// <summary>
    /// 获取生成文件的提示名称
    /// </summary>
    /// <param name="symbol">命名类型符号</param>
    /// <returns>生成文件的提示名称</returns>
    protected virtual string GetHintName(INamedTypeSymbol symbol)
        => $"{symbol.Name}.g.cs";

    #endregion

    #region Attribute / Diagnostic

    /// <summary>
    /// 获取指定符号的属性数据
    /// </summary>
    /// <param name="symbol">命名类型符号</param>
    /// <returns>属性数据，如果未找到则返回null</returns>
    protected virtual AttributeData? GetAttribute(INamedTypeSymbol symbol)
        => symbol.GetAttributes().FirstOrDefault(a =>
            string.Equals(a.AttributeClass?.ToDisplayString(), AttributeType.FullName, StringComparison.Ordinal));

    /// <summary>
    /// 报告类必须是partial的诊断信息
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

    /// <summary>
    /// 发出错误信息
    /// </summary>
    /// <param name="context">源生产上下文</param>
    /// <param name="symbol">命名类型符号</param>
    /// <param name="ex">异常对象</param>
    protected virtual void EmitError(
        SourceProductionContext context,
        INamedTypeSymbol? symbol,
        Exception ex)
    {
        var name = symbol?.Name ?? "Unknown";
        var text =
            $"// source generator error: {ex.Message}\n// {ex.StackTrace}";
        context.AddSource($"{name}.Error.g.cs", text);
    }

    #endregion
}