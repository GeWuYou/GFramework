using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GFramework.Generator.generator.logging
{
    [Generator]
    public sealed class LoggerGenerator : IIncrementalGenerator
    {
        // 请确保这里的命名空间和类名与 Attributes 项目中定义的完全一致（注意大小写！）
        private const string AttributeMetadataName = "GFramework.Generator.Attributes.generator.logging.LogAttribute";
        private const string AttributeShortName = "LogAttribute";
        private const string AttributeShortNameWithoutSuffix = "Log";

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
                // === 关键修复：添加 try-catch 块 ===
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
                        // 如果 Diagnostics 类有问题，这里可能会崩，先注释掉或确保该类存在
                        spc.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.MustBePartial,
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

        private static AttributeData GetAttribute(INamedTypeSymbol classSymbol)
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

        private static string Generate(INamedTypeSymbol classSymbol, AttributeData attr)
        {
            var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : classSymbol.ContainingNamespace.ToDisplayString();

            var className = classSymbol.Name;

            // === 解析 Category ===
            string category = className; // 默认使用类名

            // 检查是否有构造函数参数
            if (attr.ConstructorArguments.Length > 0)
            {
                var argValue = attr.ConstructorArguments[0].Value;

                // 情况 1: 参数存在，但值为 null (例如 [Log] 且构造函数有默认值 null)
                if (argValue is null)
                {
                    category = className;
                }
                // 情况 2: 参数存在，且是有效的字符串 (例如 [Log("MyCategory")])
                else if (argValue is string s && !string.IsNullOrWhiteSpace(s))
                {
                    category = s;
                }
                // 情况 3: 参数存在，但类型不对 (防御性编程)
                else
                {
                    // 这里可以选择报错，或者回退到默认值。
                    // 为了让用户知道写错了，保留报错逻辑是合理的。
                    // 注意：这里需要你有 ReportDiagnostic 的逻辑，如果没有，为了不中断生成，建议回退到默认值。
                    
                    // 如果你在 Generate 方法里无法轻松调用 ReportDiagnostic，
                    // 建议直接忽略错误使用默认值，或者生成一段 #error 代码。
                    category = $"{className}_InvalidArg"; 
                }
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
            sb.AppendLine("using GFramework.Core.logging;"); // 确保这里引用了 ILog 和 Log 类

            if (ns is not null)
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"    public partial class {className}");
            sb.AppendLine("    {");
            sb.AppendLine($"        /// <summary>Auto-generated logger</summary>");
            sb.AppendLine(
                $"        {access} {staticKeyword}readonly ILog {fieldName} = " +
                $"Log.CreateLogger(\"{category}\");");
            sb.AppendLine("    }");

            if (ns is not null)
                sb.AppendLine("}");

            return sb.ToString();
        }

        private static object? GetNamedArg(AttributeData attr, string name)
        {
            foreach (var kv in attr.NamedArguments)
            {
                if (kv.Key == name)
                    return kv.Value.Value;
            }

            return null;
        }
    }
}