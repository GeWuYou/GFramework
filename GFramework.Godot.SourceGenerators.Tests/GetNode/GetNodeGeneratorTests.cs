using GFramework.Godot.SourceGenerators.Tests.Core;

namespace GFramework.Godot.SourceGenerators.Tests.GetNode;

[TestFixture]
public class GetNodeGeneratorTests
{
    private const string FullGetNodeAttributeDeclaration = """
                                                            [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
                                                            public sealed class GetNodeAttribute : Attribute
                                                            {
                                                                public GetNodeAttribute() {}
                                                                public GetNodeAttribute(string path) { Path = path; }
                                                                public string? Path { get; set; }
                                                                public bool Required { get; set; } = true;
                                                                public NodeLookupMode Lookup { get; set; } = NodeLookupMode.Auto;
                                                            }

                                                            public enum NodeLookupMode
                                                            {
                                                                Auto = 0,
                                                                UniqueName = 1,
                                                                RelativePath = 2,
                                                                AbsolutePath = 3
                                                            }
                                                            """;

    private const string MinimalGetNodeAttributeDeclaration = """
                                                               [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
                                                               public sealed class GetNodeAttribute : Attribute
                                                               {
                                                                   public GetNodeAttribute() {}
                                                               }

                                                               public enum NodeLookupMode
                                                               {
                                                                   Auto = 0
                                                               }
                                                               """;

    private const string PropertyOnlyGetNodeAttributeDeclaration = """
                                                                    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
                                                                    public sealed class GetNodeAttribute : Attribute
                                                                    {
                                                                        public string? Path { get; set; }
                                                                        public bool Required { get; set; } = true;
                                                                        public NodeLookupMode Lookup { get; set; } = NodeLookupMode.Auto;
                                                                    }

                                                                    public enum NodeLookupMode
                                                                    {
                                                                        Auto = 0,
                                                                        UniqueName = 1,
                                                                        RelativePath = 2,
                                                                        AbsolutePath = 3
                                                                    }
                                                                    """;

    private const string NodeWithReadyAndLookupMethods = """
                                                         public class Node
                                                         {
                                                             public virtual void _Ready() {}
                                                             public T GetNode<T>(string path) where T : Node => throw new InvalidOperationException(path);
                                                             public T? GetNodeOrNull<T>(string path) where T : Node => default;
                                                         }
                                                         """;

    private const string HBoxContainerType = """
                                             public class HBoxContainer : Node
                                             {
                                             }
                                             """;

    [Test]
    public async Task Generates_InferredUniqueNameBindings_And_ReadyHook_WhenReadyIsMissing()
    {
        string source = CreateGetNodeSource(
            FullGetNodeAttributeDeclaration,
            """
                public partial class TopBar : HBoxContainer
                {
                    [GetNode]
                    private HBoxContainer _leftContainer = null!;

                    [GetNode]
                    private HBoxContainer m_rightContainer = null!;
                }
            """,
            HBoxContainerType);

        const string expected = """
                                // <auto-generated />
                                #nullable enable

                                namespace TestApp;

                                partial class TopBar
                                {
                                    private void __InjectGetNodes_Generated()
                                    {
                                        _leftContainer = GetNode<global::Godot.HBoxContainer>("%LeftContainer");
                                        m_rightContainer = GetNode<global::Godot.HBoxContainer>("%RightContainer");
                                    }

                                    partial void OnGetNodeReadyGenerated();

                                    public override void _Ready()
                                    {
                                        __InjectGetNodes_Generated();
                                        OnGetNodeReadyGenerated();
                                    }
                                }

                                """;

        await GeneratorTest<GetNodeGenerator>.RunAsync(
            source,
            ("TestApp_TopBar.GetNode.g.cs", expected)).ConfigureAwait(false);
    }

    [Test]
    public async Task Generates_ManualInjectionOnly_WhenReadyAlreadyExists()
    {
        string source = CreateGetNodeSource(
            FullGetNodeAttributeDeclaration,
            """
                public partial class TopBar : HBoxContainer
                {
                    [GetNode("%LeftContainer")]
                    private HBoxContainer _leftContainer = null!;

                    [GetNode(Required = false, Lookup = NodeLookupMode.RelativePath)]
                    private HBoxContainer? _rightContainer;

                    public override void _Ready()
                    {
                        __InjectGetNodes_Generated();
                    }
                }
            """,
            HBoxContainerType);

        const string expected = """
                                // <auto-generated />
                                #nullable enable

                                namespace TestApp;

                                partial class TopBar
                                {
                                    private void __InjectGetNodes_Generated()
                                    {
                                        _leftContainer = GetNode<global::Godot.HBoxContainer>("%LeftContainer");
                                        _rightContainer = GetNodeOrNull<global::Godot.HBoxContainer>("RightContainer");
                                    }
                                }

                                """;

        await GeneratorTest<GetNodeGenerator>.RunAsync(
            source,
            ("TestApp_TopBar.GetNode.g.cs", expected)).ConfigureAwait(false);
    }

    [Test]
    public async Task Reports_Diagnostic_When_FieldType_IsNotGodotNode()
    {
        const string source = """
                              using System;
                              using GFramework.Godot.SourceGenerators.Abstractions;
                              using Godot;

                              namespace GFramework.Godot.SourceGenerators.Abstractions
                              {
                                  [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
                                  public sealed class GetNodeAttribute : Attribute
                                  {
                                      public string? Path { get; set; }
                                      public bool Required { get; set; } = true;
                                      public NodeLookupMode Lookup { get; set; } = NodeLookupMode.Auto;
                                  }

                                  public enum NodeLookupMode
                                  {
                                      Auto = 0,
                                      UniqueName = 1,
                                      RelativePath = 2,
                                      AbsolutePath = 3
                                  }
                              }

                              namespace Godot
                              {
                                  public class Node
                                  {
                                      public virtual void _Ready() {}
                                      public T GetNode<T>(string path) where T : Node => throw new InvalidOperationException(path);
                                      public T? GetNodeOrNull<T>(string path) where T : Node => default;
                                  }
                              }

                              namespace TestApp
                              {
                                  public partial class TopBar : Node
                                  {
                                      [GetNode]
                                      private string _leftContainer = string.Empty;
                                  }
                              }
                              """;

        var test = new CSharpSourceGeneratorTest<GetNodeGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source }
            },
            DisabledDiagnostics = { "GF_Common_Trace_001" }
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult("GF_Godot_GetNode_004", DiagnosticSeverity.Error)
            .WithSpan(39, 24, 39, 38)
            .WithArguments("_leftContainer"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task Reports_Diagnostic_When_Generated_Injection_Method_Name_Already_Exists()
    {
        string source = CreateGetNodeSource(
            MinimalGetNodeAttributeDeclaration,
            """
                public partial class TopBar : HBoxContainer
                {
                    [GetNode]
                    private HBoxContainer _leftContainer = null!;

                    private void {|#0:__InjectGetNodes_Generated|}()
                    {
                    }
                }
            """,
            HBoxContainerType);

        var test = new CSharpSourceGeneratorTest<GetNodeGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source }
            },
            DisabledDiagnostics = { "GF_Common_Trace_001" },
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult("GF_Common_Class_002", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("TopBar", "__InjectGetNodes_Generated"));

        await test.RunAsync().ConfigureAwait(false);
    }

    private static string CreateGetNodeSource(
        string attributeDeclaration,
        string testAppSource,
        params string[] godotTypes)
    {
        string[] allGodotTypes = new string[godotTypes.Length + 1];
        allGodotTypes[0] = NodeWithReadyAndLookupMethods;
        Array.Copy(godotTypes, 0, allGodotTypes, 1, godotTypes.Length);

        string godotSource = string.Join($"{Environment.NewLine}{Environment.NewLine}", allGodotTypes);

        return $$"""
                 using System;
                 using GFramework.Godot.SourceGenerators.Abstractions;
                 using Godot;

                 namespace GFramework.Godot.SourceGenerators.Abstractions
                 {
                 {{attributeDeclaration}}
                 }

                 namespace Godot
                 {
                 {{godotSource}}
                 }

                 namespace TestApp
                 {
                 {{testAppSource}}
                 }
                 """;
    }
}
