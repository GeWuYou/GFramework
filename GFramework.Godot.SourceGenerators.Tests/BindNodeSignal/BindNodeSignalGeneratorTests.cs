using GFramework.Godot.SourceGenerators.Tests.Core;

namespace GFramework.Godot.SourceGenerators.Tests.BindNodeSignal;

/// <summary>
///     验证 <see cref="BindNodeSignalGenerator" /> 的生成与诊断行为。
/// </summary>
[TestFixture]
public class BindNodeSignalGeneratorTests
{
    private const string BindNodeSignalAttributeDeclaration = """
                                                              [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
                                                              public sealed class BindNodeSignalAttribute : Attribute
                                                              {
                                                                  public BindNodeSignalAttribute(string nodeFieldName, string signalName)
                                                                  {
                                                                      NodeFieldName = nodeFieldName;
                                                                      SignalName = signalName;
                                                                  }

                                                                  public string NodeFieldName { get; }

                                                                  public string SignalName { get; }
                                                              }
                                                              """;

    private const string GetNodeAttributeDeclaration = """
                                                        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
                                                        public sealed class GetNodeAttribute : Attribute
                                                        {
                                                        }
                                                        """;

    private const string EmptyNodeType = """
                                         public class Node
                                         {
                                         }
                                         """;

    private const string LifecycleNodeType = """
                                             public class Node
                                             {
                                                 public virtual void _Ready() {}

                                                 public virtual void _ExitTree() {}
                                             }
                                             """;

    private const string ButtonType = """
                                       public class Button : Node
                                       {
                                           public event Action? Pressed
                                           {
                                               add {}
                                               remove {}
                                           }
                                       }
                                       """;

    private const string SpinBoxType = """
                                        public class SpinBox : Node
                                        {
                                            public delegate void ValueChangedEventHandler(double value);

                                            public event ValueChangedEventHandler? ValueChanged
                                            {
                                                add {}
                                                remove {}
                                            }
                                        }
                                        """;

    /// <summary>
    ///     验证生成器会为已有生命周期调用生成成对的绑定与解绑方法。
    /// </summary>
    [Test]
    public async Task Generates_Bind_And_Unbind_Methods_For_Existing_Lifecycle_Hooks()
    {
        string source = CreateHudSource(
            CreateAbstractionsSource(BindNodeSignalAttributeDeclaration),
            """
                    private Button _startButton = null!;
                    private SpinBox _startOreSpinBox = null!;

                    [BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
                    private void OnStartButtonPressed()
                    {
                    }

                    [BindNodeSignal(nameof(_startOreSpinBox), nameof(SpinBox.ValueChanged))]
                    private void OnStartOreValueChanged(double value)
                    {
                    }

                    public override void _Ready()
                    {
                        __BindNodeSignals_Generated();
                    }

                    public override void _ExitTree()
                    {
                        __UnbindNodeSignals_Generated();
                    }
            """,
            LifecycleNodeType,
            ButtonType,
            SpinBoxType);

        const string expected = """
                                // <auto-generated />
                                #nullable enable

                                namespace TestApp;

                                partial class Hud
                                {
                                    private void __BindNodeSignals_Generated()
                                    {
                                        _startButton.Pressed += OnStartButtonPressed;
                                        _startOreSpinBox.ValueChanged += OnStartOreValueChanged;
                                    }

                                    private void __UnbindNodeSignals_Generated()
                                    {
                                        _startButton.Pressed -= OnStartButtonPressed;
                                        _startOreSpinBox.ValueChanged -= OnStartOreValueChanged;
                                    }
                                }

                                """;

        await GeneratorTest<BindNodeSignalGenerator>.RunAsync(
            source,
            ("TestApp_Hud.BindNodeSignal.g.cs", expected)).ConfigureAwait(false);
    }

    /// <summary>
    ///     验证一个处理方法可以通过多个特性绑定到多个节点事件，且能与 GetNode 声明共存。
    /// </summary>
    [Test]
    public async Task Generates_Multiple_Subscriptions_For_The_Same_Handler_And_Coexists_With_GetNode()
    {
        string source = CreateHudSource(
            CreateAbstractionsSource(BindNodeSignalAttributeDeclaration, GetNodeAttributeDeclaration),
            """
                    [GetNode]
                    private Button _startButton = null!;

                    [GetNode]
                    private Button _cancelButton = null!;

                    [BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
                    [BindNodeSignal(nameof(_cancelButton), nameof(Button.Pressed))]
                    private void OnAnyButtonPressed()
                    {
                    }
            """,
            LifecycleNodeType,
            ButtonType);

        const string expected = """
                                // <auto-generated />
                                #nullable enable

                                namespace TestApp;

                                partial class Hud
                                {
                                    private void __BindNodeSignals_Generated()
                                    {
                                        _startButton.Pressed += OnAnyButtonPressed;
                                        _cancelButton.Pressed += OnAnyButtonPressed;
                                    }

                                    private void __UnbindNodeSignals_Generated()
                                    {
                                        _startButton.Pressed -= OnAnyButtonPressed;
                                        _cancelButton.Pressed -= OnAnyButtonPressed;
                                    }
                                }

                                """;

        await GeneratorTest<BindNodeSignalGenerator>.RunAsync(
            source,
            ("TestApp_Hud.BindNodeSignal.g.cs", expected)).ConfigureAwait(false);
    }

    /// <summary>
    ///     验证引用不存在的事件时会报告错误。
    /// </summary>
    [Test]
    public async Task Reports_Diagnostic_When_Signal_Does_Not_Exist()
    {
        string source = CreateHudSource(
            CreateAbstractionsSource(BindNodeSignalAttributeDeclaration),
            """
                    private Button _startButton = null!;

                    [{|#0:BindNodeSignal(nameof(_startButton), "Released")|}]
                    private void OnStartButtonPressed()
                    {
                    }
            """,
            EmptyNodeType,
            ButtonType);

        await VerifyDiagnosticsAsync(
            source,
            new DiagnosticResult("GF_Godot_BindNodeSignal_006", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("_startButton", "Released")).ConfigureAwait(false);
    }

    /// <summary>
    ///     验证方法签名与事件委托不匹配时会报告错误。
    /// </summary>
    [Test]
    public async Task Reports_Diagnostic_When_Method_Signature_Does_Not_Match_Event()
    {
        string source = CreateHudSource(
            CreateAbstractionsSource(BindNodeSignalAttributeDeclaration),
            """
                    private SpinBox _startOreSpinBox = null!;

                    [{|#0:BindNodeSignal(nameof(_startOreSpinBox), nameof(SpinBox.ValueChanged))|}]
                    private void OnStartOreValueChanged()
                    {
                    }
            """,
            EmptyNodeType,
            SpinBoxType);

        await VerifyDiagnosticsAsync(
            source,
            new DiagnosticResult("GF_Godot_BindNodeSignal_007", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("OnStartOreValueChanged", "ValueChanged", "_startOreSpinBox")).ConfigureAwait(false);
    }

    /// <summary>
    ///     验证特性构造参数为空时会报告明确的参数无效诊断。
    /// </summary>
    [Test]
    public async Task Reports_Diagnostic_When_Constructor_Argument_Is_Empty()
    {
        string source = CreateHudSource(
            CreateAbstractionsSource(BindNodeSignalAttributeDeclaration),
            """
                    private Button _startButton = null!;

                    [{|#0:BindNodeSignal(nameof(_startButton), "")|}]
                    private void OnStartButtonPressed()
                    {
                    }
            """,
            EmptyNodeType,
            ButtonType);

        await VerifyDiagnosticsAsync(
            source,
            new DiagnosticResult("GF_Godot_BindNodeSignal_010", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("OnStartButtonPressed", "signalName")).ConfigureAwait(false);
    }

    /// <summary>
    ///     验证当用户自定义了与生成方法同名的成员时，会报告冲突而不是生成重复成员。
    /// </summary>
    [Test]
    public async Task Reports_Diagnostic_When_Generated_Method_Names_Already_Exist()
    {
        string source = CreateHudSource(
            CreateAbstractionsSource(BindNodeSignalAttributeDeclaration),
            """
                    private Button _startButton = null!;

                    [BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
                    private void OnStartButtonPressed()
                    {
                    }

                    private void {|#0:__BindNodeSignals_Generated|}()
                    {
                    }

                    private void {|#1:__UnbindNodeSignals_Generated|}()
                    {
                    }
            """,
            EmptyNodeType,
            ButtonType);

        await VerifyDiagnosticsAsync(
            source,
            new DiagnosticResult("GF_Common_Class_002", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("Hud", "__BindNodeSignals_Generated"),
            new DiagnosticResult("GF_Common_Class_002", DiagnosticSeverity.Error)
                .WithLocation(1)
                .WithArguments("Hud", "__UnbindNodeSignals_Generated")).ConfigureAwait(false);
    }

    /// <summary>
    ///     验证已有生命周期方法但未调用生成方法时会报告对称的警告。
    /// </summary>
    [Test]
    public async Task Reports_Warnings_When_Lifecycle_Methods_Do_Not_Call_Generated_Methods()
    {
        string source = CreateHudSource(
            CreateAbstractionsSource(BindNodeSignalAttributeDeclaration),
            """
                    private Button _startButton = null!;

                    [BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
                    private void OnStartButtonPressed()
                    {
                    }

                    public override void {|#0:_Ready|}()
                    {
                    }

                    public override void {|#1:_ExitTree|}()
                    {
                    }
            """,
            LifecycleNodeType,
            ButtonType);

        await VerifyDiagnosticsAsync(
            source,
            new DiagnosticResult("GF_Godot_BindNodeSignal_008", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Hud"),
            new DiagnosticResult("GF_Godot_BindNodeSignal_009", DiagnosticSeverity.Warning)
                .WithLocation(1)
                .WithArguments("Hud")).ConfigureAwait(false);
    }

    private static string CreateAbstractionsSource(params string[] attributeDeclarations)
    {
        string declarations = string.Join($"{Environment.NewLine}{Environment.NewLine}", attributeDeclarations);

        return $$"""
                 namespace GFramework.Godot.SourceGenerators.Abstractions
                 {
                 {{declarations}}
                 }
                 """;
    }

    private static string CreateHudSource(
        string abstractionsSource,
        string hudMembers,
        params string[] godotTypes)
    {
        string godotSource = string.Join($"{Environment.NewLine}{Environment.NewLine}", godotTypes);

        return $$"""
                 using System;
                 using GFramework.Godot.SourceGenerators.Abstractions;
                 using Godot;

                 {{abstractionsSource}}

                 namespace Godot
                 {
                 {{godotSource}}
                 }

                 namespace TestApp
                 {
                     public partial class Hud : Node
                     {
                 {{hudMembers}}
                     }
                 }
                 """;
    }

    private static Task VerifyDiagnosticsAsync(string source, params DiagnosticResult[] expectedDiagnostics)
    {
        var test = new CSharpSourceGeneratorTest<BindNodeSignalGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source }
            },
            DisabledDiagnostics = { "GF_Common_Trace_001" },
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        foreach (DiagnosticResult expectedDiagnostic in expectedDiagnostics)
        {
            test.ExpectedDiagnostics.Add(expectedDiagnostic);
        }

        return test.RunAsync();
    }
}
