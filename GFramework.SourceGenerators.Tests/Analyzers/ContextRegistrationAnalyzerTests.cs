// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

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

    // Keep scenario fixtures at class scope so MA0051 reduction does not change analyzer inputs or markup spans.
    private const string MissingFieldInjectedModelRegistrationSource = """
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
                                                                      """;

    private const string RegisteredFieldInjectedModelSource = """
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
                                                              """;

    private const string MissingHandWrittenGetSystemRegistrationSource = """
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
                                                                        """;

    private const string ModuleProvidedModelRegistrationSource = """
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
                                                                 """;

    private const string AmbiguousOwningArchitectureSource = """
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
                                                            """;

    private const string MissingGetUtilitiesRegistrationSource = """
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
                                                                 """;

    private const string DerivedArchitectureVirtualHelperRegistrationSource = """
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
                                                                            """;

    private const string DerivedModuleVirtualHelperRegistrationSource = """
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
                                                                       """;

    private const string DerivedArchitectureBaseHelperCallSource = """
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
                                                                  """;

    private const string DerivedModuleBaseHelperCallSource = """
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
                                                            """;

    /// <summary>
    ///     验证字段注入模型未注册时会报告缺失注册告警。
    /// </summary>
    [Test]
    public Task Reports_Warning_When_FieldInjectedModel_Is_Not_Registered()
    {
        return RunWarningScenarioAsync(
            MissingFieldInjectedModelRegistrationSource,
            CreateContextRegistrationWarning(
                "GF_ContextRegistration_001",
                "IInventoryModel",
                "InventoryPanelSystem",
                "GameArchitecture"));
    }

    /// <summary>
    ///     验证字段注入模型已注册时不会产生误报。
    /// </summary>
    [Test]
    public Task Does_Not_Report_When_FieldInjectedModel_Is_Registered()
    {
        return RunNoDiagnosticScenarioAsync(RegisteredFieldInjectedModelSource);
    }

    /// <summary>
    ///     验证手写扩展方法访问未注册 System 时会报告缺失注册告警。
    /// </summary>
    [Test]
    public Task Reports_Warning_When_HandWrittenGetSystem_Call_Has_No_Registration()
    {
        return RunWarningScenarioAsync(
            MissingHandWrittenGetSystemRegistrationSource,
            CreateContextRegistrationWarning(
                "GF_ContextRegistration_002",
                "ICombatSystem",
                "UiUtility",
                "GameArchitecture"));
    }

    /// <summary>
    ///     验证模块安装链路提供注册时分析器会把该注册视为有效来源。
    /// </summary>
    [Test]
    public Task Does_Not_Report_When_Registration_Comes_From_Installed_Module()
    {
        return RunNoDiagnosticScenarioAsync(ModuleProvidedModelRegistrationSource);
    }

    /// <summary>
    ///     验证无法唯一推导所属 Architecture 时分析器保持静默以避免误报。
    /// </summary>
    [Test]
    public Task Does_Not_Report_When_Owning_Architecture_Cannot_Be_Uniquely_Determined()
    {
        return RunNoDiagnosticScenarioAsync(AmbiguousOwningArchitectureSource);
    }

    /// <summary>
    ///     验证集合注入 Utility 缺失注册时仍会报告对应告警。
    /// </summary>
    [Test]
    public Task Reports_Warning_When_GetUtilities_Field_Has_No_Registered_Utility()
    {
        return RunWarningScenarioAsync(
            MissingGetUtilitiesRegistrationSource,
            CreateContextRegistrationWarning(
                "GF_ContextRegistration_003",
                "IInventoryUtility",
                "InventoryPanelSystem",
                "GameArchitecture"));
    }

    /// <summary>
    ///     验证基类初始化经由虚方法分派到派生实现时，派生注册仍会被识别。
    /// </summary>
    [Test]
    public Task
        Does_Not_Report_When_Inherited_OnInitialize_Calls_Virtual_Helper_Overridden_In_Derived_Architecture()
    {
        return RunNoDiagnosticScenarioAsync(DerivedArchitectureVirtualHelperRegistrationSource);
    }

    /// <summary>
    ///     验证模块基类通过虚方法转发注册时，派生模块的注册依然会被识别。
    /// </summary>
    [Test]
    public Task Does_Not_Report_When_Inherited_Module_Install_Calls_Virtual_Helper_Overridden_In_Derived_Module()
    {
        return RunNoDiagnosticScenarioAsync(DerivedModuleVirtualHelperRegistrationSource);
    }

    /// <summary>
    ///     验证显式调用基类 helper 时，分析器按基类实际执行的注册路径发出告警。
    /// </summary>
    [Test]
    public Task Reports_Warning_When_Derived_Architecture_Explicitly_Calls_Base_Helper()
    {
        return RunWarningScenarioAsync(
            DerivedArchitectureBaseHelperCallSource,
            CreateContextRegistrationWarning(
                "GF_ContextRegistration_001",
                "IInventoryModel",
                "InventoryPanelSystem",
                "GameArchitecture"));
    }

    /// <summary>
    ///     验证模块显式调用基类 helper 时，分析器按实际执行的安装路径发出告警。
    /// </summary>
    [Test]
    public Task Reports_Warning_When_Derived_Module_Explicitly_Calls_Base_Helper()
    {
        return RunWarningScenarioAsync(
            DerivedModuleBaseHelperCallSource,
            CreateContextRegistrationWarning(
                "GF_ContextRegistration_001",
                "IInventoryModel",
                "InventoryPanelSystem",
                "GameArchitecture"));
    }

    /// <summary>
    ///     运行包含诊断标记的 analyzer 场景，并把预期诊断绑定到统一的 `#0` span。
    /// </summary>
    /// <param name="source">不含公共前导代码的测试源码。</param>
    /// <param name="expectedDiagnostic">需要命中的预期诊断。</param>
    /// <returns>代表 analyzer 验证流程的异步任务。</returns>
    private static Task RunWarningScenarioAsync(string source, DiagnosticResult expectedDiagnostic)
    {
        MarkupTestSource markup = MarkupTestSource.Parse(Wrap(source));
        return AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(
            markup.Source,
            markup.WithSpan(expectedDiagnostic, "0"));
    }

    /// <summary>
    ///     运行不应产生诊断的 analyzer 场景。
    /// </summary>
    /// <param name="source">不含公共前导代码的测试源码。</param>
    /// <returns>代表 analyzer 验证流程的异步任务。</returns>
    private static Task RunNoDiagnosticScenarioAsync(string source)
    {
        return AnalyzerTestDriver<ContextRegistrationAnalyzer>.RunAsync(Wrap(source));
    }

    /// <summary>
    ///     构造 Context 注册分析器的统一预期诊断，以保持断言参数顺序稳定。
    /// </summary>
    /// <param name="diagnosticId">预期诊断 ID。</param>
    /// <param name="serviceType">缺失注册的服务或依赖类型。</param>
    /// <param name="ownerType">触发访问的拥有者类型。</param>
    /// <param name="architectureType">推导出的所属 Architecture 类型。</param>
    /// <returns>配置好参数的预期诊断结果。</returns>
    private static DiagnosticResult CreateContextRegistrationWarning(
        string diagnosticId,
        string serviceType,
        string ownerType,
        string architectureType)
    {
        return new DiagnosticResult(diagnosticId, DiagnosticSeverity.Warning)
            .WithArguments(serviceType, ownerType, architectureType);
    }

    /// <summary>
    ///     将公共测试前导代码与具体场景源码拼接为完整编译单元。
    /// </summary>
    /// <param name="source">具体测试场景源码。</param>
    /// <returns>包含公共前导代码的完整源码文本。</returns>
    private static string Wrap(string source)
    {
        return $"{TestPreamble}{Environment.NewLine}{Environment.NewLine}{source}";
    }
}
