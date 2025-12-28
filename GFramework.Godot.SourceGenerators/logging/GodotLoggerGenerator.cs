using System;
using System.Linq;
using System.Text;
using GFramework.Godot.SourceGenerators.constants;
using GFramework.SourceGenerators.Common.diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GFramework.Godot.SourceGenerators.logging;

/// <summary>
///     日志生成器，用于为标记了LogAttribute的类自动生成日志字段
/// </summary>
[Generator]
public sealed class GodotLoggerGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName =
        $"{PathContests.RootAbstractionsPath}.logging.GodotLogAttribute";

    private const string AttributeShortName = "GodotLogAttribute";
    private const string AttributeShortNameWithoutSuffix = "GodotLog";

    /// <summary>
    ///     初始化生成器，设置语法过滤和代码生成逻辑
    /// </summary>
    /// <param name="context">增量生成器初始化上下文</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. 语法过滤：快速筛选候选类
        var targets = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) =>
                {
                    if (node is not ClassDeclarationSyntax cls) return false;
                    // 只要包含 Log 字眼的 Attribute 就先放行
                    return cls.AttributeLists.SelectMany(a => a.Attributes).Any(a =>
                    {
                        var name = a.Name.ToString();
                        // 简单的字符串匹配，防止错过别名情况
                        return name.Contains(AttributeShortNameWithoutSuffix);
                    });
                },
                static (ctx, _) =>
                {
                    var classDecl = (ClassDeclarationSyntax)ctx.Node;
                    var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl);
                    return (ClassDecl: classDecl, Symbol: symbol);
                })
            .Where(x => x.Symbol is not null);

        // 2. 生成代码
        context.RegisterSourceOutput(targets, (spc, pair) =>
        {
            try
            {
                var classDecl = pair.ClassDecl;
                var classSymbol = pair.Symbol!;

                // 再次确认是否真的含有目标 Attribute (语义检查)
                var attr = GetAttribute(classSymbol);
                if (attr == null) return; // 可能是名字相似但不是我们要的 Attribute

                // 检查 partial
                if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        CommonDiagnostics.ClassMustBePartial,
                        classDecl.Identifier.GetLocation(),
                        classSymbol.Name));

                    return;
                }

                var source = Generate(classSymbol, attr);
                var hintName = $"{classSymbol.Name}.Logger.g.cs";
                spc.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                // === 关键修复：生成错误报告文件 ===
                var errorSource = $"// source generator error: {ex.Message}\n// StackTrace:\n// {ex.StackTrace}";
                // 替换非法字符以防文件名报错
                var safeName = pair.Symbol?.Name ?? "Unknown";
                spc.AddSource($"{safeName}.Logger.Error.g.cs", SourceText.From(errorSource, Encoding.UTF8));
            }
        });
    }

    /// <summary>
    ///     获取类符号上的LogAttribute特性
    /// </summary>
    /// <param name="classSymbol">类符号</param>
    /// <returns>LogAttribute特性数据，如果不存在则返回null</returns>
    private static AttributeData? GetAttribute(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetAttributes().FirstOrDefault(a =>
        {
            var cls = a.AttributeClass;
            if (cls == null) return false;

            // 宽松匹配：全名匹配 OR 名字匹配
            return cls.ToDisplayString() == AttributeMetadataName ||
                   cls.Name == AttributeShortName;
        });
    }

    /// <summary>
    ///     生成日志字段代码
    /// </summary>
    /// <param name="classSymbol">类符号</param>
    /// <param name="attr">LogAttribute特性数据</param>
    /// <returns>生成的C#代码字符串</returns>
    private static string Generate(INamedTypeSymbol classSymbol, AttributeData attr)
    {
        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : classSymbol.ContainingNamespace.ToDisplayString();

        var className = classSymbol.Name;

        // === 解析 Name ===
        var name = className; // 默认使用类名

        // 检查是否有构造函数参数
        if (attr.ConstructorArguments.Length > 0)
        {
            var argValue = attr.ConstructorArguments[0].Value;

            name = argValue switch
            {
                // 情况 1: 参数存在，但值为 null (例如 [GodotLog] 且构造函数有默认值 null)
                null => className,
                // 情况 2: 参数存在，且是有效的字符串 (例如 [GodotLog("MyCategory")])
                string s when !string.IsNullOrWhiteSpace(s) => s,
                _ => $"{className}_InvalidArg"
            };
        }

        // === 解析 Named Arguments (更加安全的获取方式) ===
        var fieldName = GetNamedArg(attr, "FieldName")?.ToString() ?? "_log";
        var access =
            GetNamedArg(attr, "AccessModifier")?.ToString() ??
            "private"; // 注意：如果你的 AccessModifier 是枚举，这里得到的可能是 int 或枚举名

        // 处理 bool 类型
        var isStaticObj = GetNamedArg(attr, "IsStatic");
        var isStatic = isStaticObj is not bool b || b; // 默认为 true

        var staticKeyword = isStatic ? "static " : "";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using GFramework.Core.logging;"); // 确保这里引用了 ILogger
        sb.AppendLine("using GFramework.Godot.logging;"); // 确保这里引用了 GodotLoggerFactory

        if (ns is not null)
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }

        sb.AppendLine($"    public partial class {className}");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <summary>Auto-generated logger</summary>");
        sb.AppendLine(
            $"        {access} {staticKeyword}readonly ILogger {fieldName} = " +
            $"new GodotLoggerFactory().GetLogger(\"{name}\");");
        sb.AppendLine("    }");

        if (ns is not null)
            sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    ///     从特性数据中获取命名参数的值
    /// </summary>
    /// <param name="attr">特性数据</param>
    /// <param name="name">参数名称</param>
    /// <returns>参数值，如果不存在则返回null</returns>
    private static object? GetNamedArg(AttributeData attr, string name)
    {
        return (from kv in attr.NamedArguments where kv.Key == name select kv.Value.Value).FirstOrDefault();
    }
}