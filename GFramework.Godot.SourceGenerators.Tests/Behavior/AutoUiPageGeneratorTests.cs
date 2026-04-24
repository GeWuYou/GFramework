using GFramework.Godot.SourceGenerators.Behavior;
using GFramework.Godot.SourceGenerators.Tests.Core;

namespace GFramework.Godot.SourceGenerators.Tests.Behavior;

[TestFixture]
public class AutoUiPageGeneratorTests
{
    private const string AutoUiPageAttributeWithLayerDeclaration = """
                                                                     [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                                                     public sealed class AutoUiPageAttribute : Attribute
                                                                     {
                                                                         public AutoUiPageAttribute(string key, string layerName) { }
                                                                     }
                                                                     """;

    private const string AutoUiPageAttributeWithoutLayerDeclaration = """
                                                                        [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                                                        public sealed class AutoUiPageAttribute : Attribute
                                                                        {
                                                                            public AutoUiPageAttribute(string key) { }
                                                                        }
                                                                        """;

    private const string CanvasNodeTypes = """
                                           public class Node { }
                                           public class CanvasItem : Node { }
                                           public class Control : CanvasItem { }
                                           """;

    private const string UiLayerFullEnum = """
                                          namespace GFramework.Game.Abstractions.Enums
                                          {
                                              public enum UiLayer
                                              {
                                                  Page,
                                                  Overlay,
                                                  Modal
                                              }
                                          }
                                          """;

    private const string UiLayerPageOnlyEnum = """
                                              namespace GFramework.Game.Abstractions.Enums
                                              {
                                                  public enum UiLayer
                                                  {
                                                      Page
                                                  }
                                              }
                                              """;

    private const string UiBehaviorInfrastructure = """
                                                   namespace GFramework.Game.Abstractions.UI
                                                   {
                                                       public interface IUiPageBehavior { }
                                                   }

                                                   namespace GFramework.Godot.UI
                                                   {
                                                       using GFramework.Game.Abstractions.Enums;
                                                       using GFramework.Game.Abstractions.UI;
                                                       using Godot;

                                                       public static class UiPageBehaviorFactory
                                                       {
                                                           public static IUiPageBehavior Create<T>(T owner, string key, UiLayer layer)
                                                               where T : CanvasItem
                                                           {
                                                               return null!;
                                                           }
                                                       }
                                                   }
                                                   """;

    [Test]
    public async Task Generates_Ui_Page_Behavior_Boilerplate()
    {
        string source = CreateAutoUiPageSource(
            AutoUiPageAttributeWithLayerDeclaration,
            UiLayerFullEnum,
            """
                [AutoUiPage("MainMenu", "Page")]
                public partial class MainMenu : Control
                {
                }
            """);

        const string expected = """
                                // <auto-generated />
                                #nullable enable

                                namespace TestApp;

                                partial class MainMenu
                                {
                                    private global::GFramework.Game.Abstractions.UI.IUiPageBehavior? __autoUiPageBehavior_Generated;

                                    public static string UiKeyStr => "MainMenu";

                                    public global::GFramework.Game.Abstractions.UI.IUiPageBehavior GetPage()
                                    {
                                        return __autoUiPageBehavior_Generated ??= global::GFramework.Godot.UI.UiPageBehaviorFactory.Create(this, UiKeyStr, global::GFramework.Game.Abstractions.Enums.UiLayer.Page);
                                    }
                                }

                                """;

        await GeneratorTest<AutoUiPageGenerator>.RunAsync(
            source,
            ("TestApp_MainMenu.AutoUiPage.g.cs", expected)).ConfigureAwait(false);
    }

    [Test]
    public async Task Reports_Diagnostic_When_AutoUiPage_Attribute_Arguments_Are_Invalid()
    {
        string source = CreateAutoUiPageSource(
            AutoUiPageAttributeWithoutLayerDeclaration,
            UiLayerPageOnlyEnum,
            """
                [{|#0:AutoUiPage("MainMenu")|}]
                public partial class MainMenu : Control
                {
                }
            """);

        var test = new CSharpSourceGeneratorTest<AutoUiPageGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source }
            },
            DisabledDiagnostics = { "GF_Common_Trace_001" },
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult("GF_AutoBehavior_004", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments(
                "AutoUiPageAttribute",
                "MainMenu",
                "a string key argument and a string UiLayer name argument"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task Generates_Type_Constraints_For_ClassNullable_NotNull_And_Unmanaged()
    {
        string source = CreateAutoUiPageSource(
            AutoUiPageAttributeWithLayerDeclaration,
            UiLayerPageOnlyEnum,
            """
                [AutoUiPage("MainMenu", "Page")]
                public partial class MainMenu<TReference, TNotNull, TUnmanaged> : Control
                    where TReference : class?
                    where TNotNull : notnull
                    where TUnmanaged : unmanaged
                {
                }
            """,
            nullableEnabled: true);

        const string expected = """
                                // <auto-generated />
                                #nullable enable

                                namespace TestApp;

                                partial class MainMenu<TReference, TNotNull, TUnmanaged>
                                    where TReference : class?
                                    where TNotNull : notnull
                                    where TUnmanaged : unmanaged
                                {
                                    private global::GFramework.Game.Abstractions.UI.IUiPageBehavior? __autoUiPageBehavior_Generated;

                                    public static string UiKeyStr => "MainMenu";

                                    public global::GFramework.Game.Abstractions.UI.IUiPageBehavior GetPage()
                                    {
                                        return __autoUiPageBehavior_Generated ??= global::GFramework.Godot.UI.UiPageBehaviorFactory.Create(this, UiKeyStr, global::GFramework.Game.Abstractions.Enums.UiLayer.Page);
                                    }
                                }

                                """;

        await GeneratorTest<AutoUiPageGenerator>.RunAsync(
            source,
            ("TestApp_MainMenu.AutoUiPage.g.cs", expected)).ConfigureAwait(false);
    }

    private static string CreateAutoUiPageSource(
        string attributeDeclaration,
        string uiLayerDeclaration,
        string testAppSource,
        bool nullableEnabled = false)
    {
        string nullableDirective = nullableEnabled ? "#nullable enable\n" : string.Empty;

        return $$"""
                 {{nullableDirective}}using System;
                 using GFramework.Godot.SourceGenerators.Abstractions.UI;
                 using Godot;

                 namespace GFramework.Godot.SourceGenerators.Abstractions.UI
                 {
                 {{attributeDeclaration}}
                 }

                 namespace Godot
                 {
                 {{CanvasNodeTypes}}
                 }

                 {{uiLayerDeclaration}}

                 {{UiBehaviorInfrastructure}}

                 namespace TestApp
                 {
                 {{testAppSource}}
                 }
                 """;
    }
}
