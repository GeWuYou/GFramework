using GFramework.Core.SourceGenerators.Analyzers;
using GFramework.SourceGenerators.Tests.Core;

namespace GFramework.SourceGenerators.Tests.Analyzers;

/// <summary>
///     验证 Context Get 注册可见性分析器的核心行为。
/// </summary>
public sealed class ContextRegistrationAnalyzerTests
{
    private const string TestPreamble = """
                                        using System;
                                        using System.Collections.Generic;

                                        namespace GFramework.Core.Abstractions.Rule
                                        {
                                            public interface IContextAware { }
                                        }

                                        namespace GFramework.Core.Abstractions.Model
                                        {
                                            public interface IModel : GFramework.Core.Abstractions.Rule.IContextAware { }
                                        }

                                        namespace GFramework.Core.Abstractions.Systems
                                        {
                                            public interface ISystem : GFramework.Core.Abstractions.Rule.IContextAware { }
                                        }

                                        namespace GFramework.Core.Abstractions.Utility
                                        {
                                            public interface IUtility : GFramework.Core.Abstractions.Rule.IContextAware { }
                                        }

                                        namespace GFramework.Core.Abstractions.Architectures
                                        {
                                            public interface IArchitecture
                                            {
                                                T RegisterModel<T>(T model) where T : GFramework.Core.Abstractions.Model.IModel;
                                                void RegisterModel<T>(Action<T> onCreated = null) where T : class, GFramework.Core.Abstractions.Model.IModel;
                                                T RegisterSystem<T>(T system) where T : GFramework.Core.Abstractions.Systems.ISystem;
                                                void RegisterSystem<T>(Action<T> onCreated = null) where T : class, GFramework.Core.Abstractions.Systems.ISystem;
                                                T RegisterUtility<T>(T utility) where T : GFramework.Core.Abstractions.Utility.IUtility;
                                                void RegisterUtility<T>(Action<T> onCreated = null) where T : class, GFramework.Core.Abstractions.Utility.IUtility;
                                                IArchitectureModule InstallModule(IArchitectureModule module);
                                            }
                                            
                                            public interface IArchitectureModule
                                            {
                                                void Install(IArchitecture architecture);
                                            }
                                            
                                            public interface IArchitectureContext
                                            {
                                                TModel GetModel<TModel>() where TModel : class, GFramework.Core.Abstractions.Model.IModel;
                                                IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, GFramework.Core.Abstractions.Model.IModel;
                                                TSystem GetSystem<TSystem>() where TSystem : class, GFramework.Core.Abstractions.Systems.ISystem;
                                                IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, GFramework.Core.Abstractions.Systems.ISystem;
                                                TUtility GetUtility<TUtility>() where TUtility : class, GFramework.Core.Abstractions.Utility.IUtility;
                                                IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, GFramework.Core.Abstractions.Utility.IUtility;
                                            }
                                        }

                                        namespace GFramework.Core.Architectures
                                        {
                                            public abstract class Architecture : GFramework.Core.Abstractions.Architectures.IArchitecture
                                            {
                                                protected abstract void OnInitialize();
                                                
                                                public virtual T RegisterModel<T>(T model) where T : GFramework.Core.Abstractions.Model.IModel => model;
                                                
                                                public virtual void RegisterModel<T>(Action<T> onCreated = null)
                                                    where T : class, GFramework.Core.Abstractions.Model.IModel
                                                {
                                                }
                                                
                                                public virtual T RegisterSystem<T>(T system) where T : GFramework.Core.Abstractions.Systems.ISystem => system;
                                                
                                                public virtual void RegisterSystem<T>(Action<T> onCreated = null)
                                                    where T : class, GFramework.Core.Abstractions.Systems.ISystem
                                                {
                                                }
                                                
                                                public virtual T RegisterUtility<T>(T utility) where T : GFramework.Core.Abstractions.Utility.IUtility => utility;
                                                
                                                public virtual void RegisterUtility<T>(Action<T> onCreated = null)
                                                    where T : class, GFramework.Core.Abstractions.Utility.IUtility
                                                {
                                                }
                                                
                                                public virtual GFramework.Core.Abstractions.Architectures.IArchitectureModule InstallModule(
                                                    GFramework.Core.Abstractions.Architectures.IArchitectureModule module)
                                                {
                                                    module.Install(this);
                                                    return module;
                                                }
                                            }
                                        }

                                        namespace GFramework.Core.Extensions
                                        {
                                            public static class ContextAwareServiceExtensions
                                            {
                                                public static TModel GetModel<TModel>(this GFramework.Core.Abstractions.Rule.IContextAware contextAware)
                                                    where TModel : class, GFramework.Core.Abstractions.Model.IModel => throw new NotImplementedException();
                                                
                                                public static IReadOnlyList<TModel> GetModels<TModel>(this GFramework.Core.Abstractions.Rule.IContextAware contextAware)
                                                    where TModel : class, GFramework.Core.Abstractions.Model.IModel => throw new NotImplementedException();
                                                
                                                public static TSystem GetSystem<TSystem>(this GFramework.Core.Abstractions.Rule.IContextAware contextAware)
                                                    where TSystem : class, GFramework.Core.Abstractions.Systems.ISystem => throw new NotImplementedException();
                                                
                                                public static IReadOnlyList<TSystem> GetSystems<TSystem>(this GFramework.Core.Abstractions.Rule.IContextAware contextAware)
                                                    where TSystem : class, GFramework.Core.Abstractions.Systems.ISystem => throw new NotImplementedException();
                                                
                                                public static TUtility GetUtility<TUtility>(this GFramework.Core.Abstractions.Rule.IContextAware contextAware)
                                                    where TUtility : class, GFramework.Core.Abstractions.Utility.IUtility => throw new NotImplementedException();
                                                
                                                public static IReadOnlyList<TUtility> GetUtilities<TUtility>(this GFramework.Core.Abstractions.Rule.IContextAware contextAware)
                                                    where TUtility : class, GFramework.Core.Abstractions.Utility.IUtility => throw new NotImplementedException();
                                            }
                                        }

                                        namespace GFramework.Core.SourceGenerators.Abstractions.Rule
                                        {
                                            public sealed class GetModelAttribute : Attribute { }
                                            public sealed class GetModelsAttribute : Attribute { }
                                            public sealed class GetSystemAttribute : Attribute { }
                                            public sealed class GetSystemsAttribute : Attribute { }
                                            public sealed class GetUtilityAttribute : Attribute { }
                                            public sealed class GetUtilitiesAttribute : Attribute { }
                                        }
                                        """;

    [Test]
    public async Task Reports_Warning_When_FieldInjectedModel_Is_Not_Registered()
    {
        var markup = MarkupTestSource.Parse(
            Wrap("""
                 namespace TestApp
                 {
                     using GFramework.Core.Abstractions.Model;
                     using GFramework.Core.Abstractions.Systems;
                     using GFramework.Core.Architectures;
                     using GFramework.Core.SourceGenerators.Abstractions.Rule;
                     
                     public interface IInventoryModel : IModel { }
                     
                     public sealed class InventoryPanelSystem : ISystem
                     {
                         [GetModel]
                         private IInventoryModel {|#0:_model|} = null!;
                     }
                     
                     public sealed class GameArchitecture : Architecture
                     {
                         protected override void OnInitialize()
                         {
                             RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                 }
                 """));

        await AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            markup.Source,
            markup.WithSpan(
                new DiagnosticResult("GF_ContextRegistration_001", DiagnosticSeverity.Warning)
                    .WithArguments("IInventoryModel", "InventoryPanelSystem", "GameArchitecture"),
                "0"));
    }

    [Test]
    public async Task Does_Not_Report_When_FieldInjectedModel_Is_Registered()
    {
        await AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            Wrap("""
                 namespace TestApp
                 {
                     using GFramework.Core.Abstractions.Model;
                     using GFramework.Core.Abstractions.Systems;
                     using GFramework.Core.Architectures;
                     using GFramework.Core.SourceGenerators.Abstractions.Rule;
                     
                     public interface IInventoryModel : IModel { }
                     
                     public sealed class InventoryModel : IInventoryModel { }
                     
                     public sealed class InventoryPanelSystem : ISystem
                     {
                         [GetModel]
                         private IInventoryModel _model = null!;
                     }
                     
                     public sealed class GameArchitecture : Architecture
                     {
                         protected override void OnInitialize()
                         {
                             RegisterModel(new InventoryModel());
                             RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                 }
                 """));
    }

    [Test]
    public async Task Reports_Warning_When_HandWrittenGetSystem_Call_Has_No_Registration()
    {
        var markup = MarkupTestSource.Parse(
            Wrap("""
                 namespace TestApp
                 {
                     using GFramework.Core.Abstractions.Systems;
                     using GFramework.Core.Abstractions.Utility;
                     using GFramework.Core.Architectures;
                     using GFramework.Core.Extensions;
                     
                     public interface ICombatSystem : ISystem { }
                     
                     public sealed class UiUtility : IUtility
                     {
                         public void Initialize()
                         {
                             {|#0:this.GetSystem<ICombatSystem>()|};
                         }
                     }
                     
                     public sealed class GameArchitecture : Architecture
                     {
                         protected override void OnInitialize()
                         {
                             RegisterUtility(new UiUtility());
                         }
                     }
                 }
                 """));

        await AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            markup.Source,
            markup.WithSpan(
                new DiagnosticResult("GF_ContextRegistration_002", DiagnosticSeverity.Warning)
                    .WithArguments("ICombatSystem", "UiUtility", "GameArchitecture"),
                "0"));
    }

    [Test]
    public async Task Does_Not_Report_When_Registration_Comes_From_Installed_Module()
    {
        await AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            Wrap("""
                 namespace TestApp
                 {
                     using GFramework.Core.Abstractions.Architectures;
                     using GFramework.Core.Abstractions.Model;
                     using GFramework.Core.Abstractions.Systems;
                     using GFramework.Core.Architectures;
                     using GFramework.Core.SourceGenerators.Abstractions.Rule;
                     
                     public interface IInventoryModel : IModel { }
                     
                     public sealed class InventoryModel : IInventoryModel { }
                     
                     public sealed class InventoryPanelSystem : ISystem
                     {
                         [GetModel]
                         private IInventoryModel _model = null!;
                     }
                     
                     public sealed class InventoryModule : IArchitectureModule
                     {
                         public void Install(IArchitecture architecture)
                         {
                             architecture.RegisterModel(new InventoryModel());
                         }
                     }
                     
                     public sealed class GameArchitecture : Architecture
                     {
                         protected override void OnInitialize()
                         {
                             InstallModule(new InventoryModule());
                             RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                 }
                 """));
    }

    [Test]
    public async Task Does_Not_Report_When_Owning_Architecture_Cannot_Be_Uniquely_Determined()
    {
        await AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            Wrap("""
                 namespace TestApp
                 {
                     using GFramework.Core.Abstractions.Model;
                     using GFramework.Core.Abstractions.Systems;
                     using GFramework.Core.Architectures;
                     using GFramework.Core.SourceGenerators.Abstractions.Rule;
                     
                     public interface IInventoryModel : IModel { }
                     
                     public sealed class InventoryPanelSystem : ISystem
                     {
                         [GetModel]
                         private IInventoryModel _model = null!;
                     }
                     
                     public sealed class FirstArchitecture : Architecture
                     {
                         protected override void OnInitialize()
                         {
                             RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                     
                     public sealed class SecondArchitecture : Architecture
                     {
                         protected override void OnInitialize()
                         {
                             RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                 }
                 """));
    }

    [Test]
    public async Task Reports_Warning_When_GetUtilities_Field_Has_No_Registered_Utility()
    {
        var markup = MarkupTestSource.Parse(
            Wrap("""
                 namespace TestApp
                 {
                     using System.Collections.Generic;
                     using GFramework.Core.Abstractions.Systems;
                     using GFramework.Core.Abstractions.Utility;
                     using GFramework.Core.Architectures;
                     using GFramework.Core.SourceGenerators.Abstractions.Rule;
                     
                     public interface IInventoryUtility : IUtility { }
                     
                     public sealed class InventoryPanelSystem : ISystem
                     {
                         [GetUtilities]
                         private IReadOnlyList<IInventoryUtility> {|#0:_utilities|} = null!;
                     }
                     
                     public sealed class GameArchitecture : Architecture
                     {
                         protected override void OnInitialize()
                         {
                             RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                 }
                 """));

        await AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            markup.Source,
            markup.WithSpan(
                new DiagnosticResult("GF_ContextRegistration_003", DiagnosticSeverity.Warning)
                    .WithArguments("IInventoryUtility", "InventoryPanelSystem", "GameArchitecture"),
                "0"));
    }

    [Test]
    public async Task
        Does_Not_Report_When_Inherited_OnInitialize_Calls_Virtual_Helper_Overridden_In_Derived_Architecture()
    {
        await AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            Wrap("""
                 namespace TestApp
                 {
                     using GFramework.Core.Abstractions.Model;
                     using GFramework.Core.Abstractions.Systems;
                     using GFramework.Core.Architectures;
                     using GFramework.Core.SourceGenerators.Abstractions.Rule;
                     
                     public interface IInventoryModel : IModel { }
                     
                     public sealed class InventoryModel : IInventoryModel { }
                     
                     public abstract class ArchitectureBase : Architecture
                     {
                         protected override void OnInitialize()
                         {
                             RegisterComponents();
                         }
                     
                         protected virtual void RegisterComponents()
                         {
                         }
                     }
                     
                     public sealed class InventoryPanelSystem : ISystem
                     {
                         [GetModel]
                         private IInventoryModel _model = null!;
                     }
                     
                     public sealed class GameArchitecture : ArchitectureBase
                     {
                         protected override void RegisterComponents()
                         {
                             RegisterModel(new InventoryModel());
                             RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                 }
                 """));
    }

    [Test]
    public async Task Does_Not_Report_When_Inherited_Module_Install_Calls_Virtual_Helper_Overridden_In_Derived_Module()
    {
        await AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            Wrap("""
                 namespace TestApp
                 {
                     using GFramework.Core.Abstractions.Architectures;
                     using GFramework.Core.Abstractions.Model;
                     using GFramework.Core.Abstractions.Systems;
                     using GFramework.Core.Architectures;
                     using GFramework.Core.SourceGenerators.Abstractions.Rule;
                     
                     public interface IInventoryModel : IModel { }
                     
                     public sealed class InventoryModel : IInventoryModel { }
                     
                     public abstract class ModuleBase : IArchitectureModule
                     {
                         public void Install(IArchitecture architecture)
                         {
                             RegisterComponents(architecture);
                         }
                     
                         protected virtual void RegisterComponents(IArchitecture architecture)
                         {
                         }
                     }
                     
                     public sealed class DerivedInventoryModule : ModuleBase
                     {
                         protected override void RegisterComponents(IArchitecture architecture)
                         {
                             architecture.RegisterModel(new InventoryModel());
                         }
                     }
                     
                     public sealed class InventoryPanelSystem : ISystem
                     {
                         [GetModel]
                         private IInventoryModel _model = null!;
                     }
                     
                     public sealed class GameArchitecture : Architecture
                     {
                         protected override void OnInitialize()
                         {
                             InstallModule(new DerivedInventoryModule());
                             RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                 }
                 """));
    }

    [Test]
    public async Task Reports_Warning_When_Derived_Architecture_Explicitly_Calls_Base_Helper()
    {
        var markup = MarkupTestSource.Parse(
            Wrap("""
                 namespace TestApp
                 {
                     using GFramework.Core.Abstractions.Model;
                     using GFramework.Core.Abstractions.Systems;
                     using GFramework.Core.Architectures;
                     using GFramework.Core.SourceGenerators.Abstractions.Rule;
                     
                     public interface IInventoryModel : IModel { }
                     
                     public sealed class InventoryModel : IInventoryModel { }
                     
                     public sealed class InventoryPanelSystem : ISystem
                     {
                         [GetModel]
                         private IInventoryModel {|#0:_model|} = null!;
                     }
                     
                     public abstract class ArchitectureBase : Architecture
                     {
                         protected virtual void RegisterComponents()
                         {
                             RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                     
                     public sealed class GameArchitecture : ArchitectureBase
                     {
                         protected override void OnInitialize()
                         {
                             base.RegisterComponents();
                         }
                     
                         protected override void RegisterComponents()
                         {
                             RegisterModel(new InventoryModel());
                             RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                 }
                 """));

        await AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            markup.Source,
            markup.WithSpan(
                new DiagnosticResult("GF_ContextRegistration_001", DiagnosticSeverity.Warning)
                    .WithArguments("IInventoryModel", "InventoryPanelSystem", "GameArchitecture"),
                "0"));
    }

    [Test]
    public async Task Reports_Warning_When_Derived_Module_Explicitly_Calls_Base_Helper()
    {
        var markup = MarkupTestSource.Parse(
            Wrap("""
                 namespace TestApp
                 {
                     using GFramework.Core.Abstractions.Architectures;
                     using GFramework.Core.Abstractions.Model;
                     using GFramework.Core.Abstractions.Systems;
                     using GFramework.Core.Architectures;
                     using GFramework.Core.SourceGenerators.Abstractions.Rule;
                     
                     public interface IInventoryModel : IModel { }
                     
                     public sealed class InventoryModel : IInventoryModel { }
                     
                     public sealed class InventoryPanelSystem : ISystem
                     {
                         [GetModel]
                         private IInventoryModel {|#0:_model|} = null!;
                     }
                     
                     public abstract class ModuleBase : IArchitectureModule
                     {
                         public virtual void Install(IArchitecture architecture)
                         {
                         }
                     
                         protected virtual void RegisterComponents(IArchitecture architecture)
                         {
                             architecture.RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                     
                     public sealed class DerivedInventoryModule : ModuleBase
                     {
                         public override void Install(IArchitecture architecture)
                         {
                             base.RegisterComponents(architecture);
                         }
                     
                         protected override void RegisterComponents(IArchitecture architecture)
                         {
                             architecture.RegisterModel(new InventoryModel());
                             architecture.RegisterSystem(new InventoryPanelSystem());
                         }
                     }
                     
                     public sealed class GameArchitecture : Architecture
                     {
                         protected override void OnInitialize()
                         {
                             InstallModule(new DerivedInventoryModule());
                         }
                     }
                 }
                 """));

        await AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            markup.Source,
            markup.WithSpan(
                new DiagnosticResult("GF_ContextRegistration_001", DiagnosticSeverity.Warning)
                    .WithArguments("IInventoryModel", "InventoryPanelSystem", "GameArchitecture"),
                "0"));
    }

    private static string Wrap(string source)
    {
        return $"{TestPreamble}{Environment.NewLine}{Environment.NewLine}{source}";
    }
}
