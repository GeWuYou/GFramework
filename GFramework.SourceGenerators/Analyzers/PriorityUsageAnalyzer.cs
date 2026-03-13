using System.Collections.Immutable;
using GFramework.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace GFramework.SourceGenerators.Analyzers;

/// <summary>
/// 优先级使用分析器，检测应该使用 GetAllByPriority 而非 GetAll 的场景
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PriorityUsageAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// 支持的诊断规则
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(PriorityDiagnostic.SuggestGetAllByPriority);

    /// <summary>
    /// 初始化分析器
    /// </summary>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // 缓存符号查找
            var iPrioritized = compilationContext.Compilation.GetTypeByMetadataName(
                "GFramework.Core.Abstractions.Bases.IPrioritized");

            if (iPrioritized == null)
                return;

            var iocContainer = compilationContext.Compilation.GetTypeByMetadataName(
                "GFramework.Core.Abstractions.Ioc.IIocContainer");

            var architectureContext = compilationContext.Compilation.GetTypeByMetadataName(
                "GFramework.Core.Abstractions.Architecture.IArchitectureContext");

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(
                    operationContext,
                    iPrioritized,
                    iocContainer,
                    architectureContext),
                OperationKind.Invocation);
        });
    }

    /// <summary>
    /// 分析方法调用
    /// </summary>
    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol iPrioritized,
        INamedTypeSymbol? iocContainer,
        INamedTypeSymbol? architectureContext)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        // 检查方法名是否为 GetAll
        if (method.Name != "GetAll")
            return;

        // 检查是否为泛型方法
        if (!method.IsGenericMethod || method.TypeArguments.Length != 1)
            return;

        // 检查方法来源
        var containingType = method.ContainingType;
        if (iocContainer != null && SymbolEqualityComparer.Default.Equals(containingType, iocContainer))
        {
            // 来自 IIocContainer
        }
        else if (architectureContext != null &&
                 SymbolEqualityComparer.Default.Equals(containingType, architectureContext))
        {
            // 来自 IArchitectureContext
        }
        else
        {
            return;
        }

        // 检查泛型参数是否实现了 IPrioritized
        var typeArgument = method.TypeArguments[0];
        if (typeArgument is not INamedTypeSymbol namedType)
            return;

        if (!ImplementsInterface(namedType, iPrioritized))
            return;

        // 报告诊断
        var diagnostic = Diagnostic.Create(
            PriorityDiagnostic.SuggestGetAllByPriority,
            invocation.Syntax.GetLocation(),
            typeArgument.ToDisplayString());

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// 检查类型是否实现了指定接口
    /// </summary>
    private static bool ImplementsInterface(INamedTypeSymbol type, INamedTypeSymbol interfaceType)
    {
        return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
    }
}