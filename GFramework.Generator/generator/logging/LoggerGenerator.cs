using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GFramework.Generator.generator.logging
{
    /// <summary>
    /// 日志生成器，用于为标记了LogAttribute的类自动生成日志字段
    /// </summary>
    [Generator]
    public sealed class LoggerGenerator : IIncrementalGenerator
    {
        private const string AttributeMetadataName =
            "GFramework.Generator.Attributes.generator.logging.LogAttribute";

        /// <summary>
        /// 初始化增量生成器
        /// </summary>
        /// <param name="context">增量生成器初始化上下文</param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 筛选出带有指定属性标记的类
            var targets =
                context.SyntaxProvider.CreateSyntaxProvider(
                        static (node, _) =>
                        {
                            if (node is not ClassDeclarationSyntax cls)
                                return false;

                            // 只要写了 [Log] / [Log(...)] 就命中
                            return cls.AttributeLists
                                .SelectMany(a => a.Attributes)
                                .Any(a =>
                                {
                                    var name = a.Name.ToString();
                                    return name == "Log"
                                           || name == "LogAttribute"
                                           || name.EndsWith(".Log")
                                           || name.EndsWith(".LogAttribute");
                                });
                        },
                        static (ctx, _) =>
                        {
                            var classDecl = (ClassDeclarationSyntax)ctx.Node;
                            var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl);
                            return (ClassDecl: classDecl, Symbol: symbol);
                        })
                    .Where(x => x.Symbol is not null);


            // 注册源代码输出，为符合条件的类生成日志相关的源代码
            context.RegisterSourceOutput(targets, (spc, pair) =>
            {
                var classDecl = pair.ClassDecl;
                var classSymbol = pair.Symbol!;

                if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.MustBePartial,
                            classDecl.Identifier.GetLocation(),
                            classSymbol.Name));
                    return;
                }

                var source = Generate(classSymbol, spc);
                spc.AddSource($"{classSymbol.Name}.Logger.g.cs",
                    SourceText.From(source, Encoding.UTF8));
            });

        }


        /// <summary>
        /// 生成日志字段代码
        /// </summary>
        /// <param name="classSymbol">类符号</param>
        /// <param name="spc">源代码生成上下文</param>
        /// <returns>生成的代码字符串</returns>
        private static string Generate(INamedTypeSymbol classSymbol, SourceProductionContext spc)
        {
            var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : classSymbol.ContainingNamespace.ToDisplayString();

            var className = classSymbol.Name;

            var attr = classSymbol.GetAttributes()
                .FirstOrDefault(a =>
                {
                    var c = a.AttributeClass;
                    if (c is null) return false;

                    return c.ToDisplayString() == AttributeMetadataName
                           || c.Name == "LogAttribute";
                });

            if (attr is null)
            {
                // 理论上不会发生，但防御式处理
                spc.ReportDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.LogAttributeInvalid,
                        classSymbol.Locations.FirstOrDefault(),
                        className,
                        "未找到 LogAttribute"
                    ));
                return string.Empty;
            }

            // === 解析 category ===
            string category;

            if (attr.ConstructorArguments.Length == 0)
            {
                // 默认：类名
                category = className;
            }
            else if (attr.ConstructorArguments[0].Value is string s &&
                     !string.IsNullOrWhiteSpace(s))
            {
                category = s;
            }
            else
            {
                spc.ReportDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.LogAttributeInvalid,
                        classSymbol.Locations.FirstOrDefault(),
                        className,
                        "LogAttribute 的构造参数不是有效的 string"
                    ));
                return string.Empty;
            }

            var fieldName = GetNamedArg(attr, "FieldName", "_log");
            var access = GetNamedArg(attr, "AccessModifier", "private");
            var isStatic = GetNamedArg(attr, "IsStatic", true);

            var staticKeyword = isStatic ? "static " : "";

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("using GFramework.Core.logging;");

            if (ns is not null)
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"    public partial class {className}");
            sb.AppendLine("    {");
            sb.AppendLine(
                $"        {access} {staticKeyword}readonly ILog {fieldName} = " +
                $"Log.CreateLogger(\"{category}\");");
            sb.AppendLine("    }");

            if (ns is not null)
                sb.AppendLine("}");

            return sb.ToString();
        }


        /// <summary>
        /// 获取属性参数的值
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="attr">属性数据</param>
        /// <param name="name">参数名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值或默认值</returns>
        private static T GetNamedArg<T>(AttributeData attr, string name, T defaultValue)
        {
            foreach (var kv in attr.NamedArguments)
            {
                if (kv.Key == name && kv.Value.Value is T v)
                    return v;
            }

            return defaultValue;
        }
    }
}