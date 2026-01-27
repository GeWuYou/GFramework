using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GFramework.SourceGenerators.Common.generator;

/// <summary>
///     属性枚举生成器基类，用于基于特定属性的枚举进行源代码生成
/// </summary>
public abstract class AttributeEnumGeneratorBase : IIncrementalGenerator
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
        // 创建语法提供程序，查找带有指定属性的枚举声明
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) =>
                    node is EnumDeclarationSyntax eds &&
                    eds.AttributeLists
                        .SelectMany(a => a.Attributes)
                        .Any(a => a.Name.ToString()
                            .Contains(AttributeShortNameWithoutSuffix)),
                (ctx, _) =>
                {
                    var syntax = (EnumDeclarationSyntax)ctx.Node;
                    var symbol = ctx.SemanticModel.GetDeclaredSymbol(syntax, _) as INamedTypeSymbol;
                    return (syntax, symbol);
                })
            .Where(x => x.symbol is not null);

        var combined = candidates.Combine(context.CompilationProvider);

        // 注册源输出，生成最终的源代码
        context.RegisterSourceOutput(combined, (spc, pair) =>
        {
            var ((syntax, symbol), compilation) = pair;

            var attr = ResolveAttribute(compilation, symbol!);
            if (attr is null)
                return;

            if (!ValidateSymbol(spc, compilation, syntax, symbol!, attr))
                return;

            spc.AddSource(
                GetHintName(symbol!),
                Generate(symbol!, attr));
        });
    }

    /// <summary>
    ///     解析指定符号上的属性数据
    /// </summary>
    /// <param name="compilation">编译对象</param>
    /// <param name="symbol">命名类型符号</param>
    /// <returns>属性数据对象，如果未找到则返回null</returns>
    protected abstract AttributeData? ResolveAttribute(
        Compilation compilation,
        INamedTypeSymbol symbol);

    /// <summary>
    ///     验证符号是否符合生成要求
    /// </summary>
    /// <param name="context">源生产上下文</param>
    /// <param name="compilation">编译对象</param>
    /// <param name="syntax">枚举声明语法节点</param>
    /// <param name="symbol">命名类型符号</param>
    /// <param name="attr">属性数据</param>
    /// <returns>验证是否通过</returns>
    protected abstract bool ValidateSymbol(
        SourceProductionContext context,
        Compilation compilation,
        EnumDeclarationSyntax syntax,
        INamedTypeSymbol symbol,
        AttributeData attr);

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
    {
        return $"{symbol.Name}.g.cs";
    }
}