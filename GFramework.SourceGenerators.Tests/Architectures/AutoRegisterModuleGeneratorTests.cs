// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.SourceGenerators.Architectures;
using GFramework.SourceGenerators.Tests.Core;

namespace GFramework.SourceGenerators.Tests.Architectures;

/// <summary>
///     验证 <see cref="AutoRegisterModuleGenerator" /> 在模块自动注册场景下的生成契约与输出顺序。
/// </summary>
[TestFixture]
public class AutoRegisterModuleGeneratorTests
{
    private const string AttributeOrderSource = """
                                                using System;
                                                using GFramework.Core.SourceGenerators.Abstractions.Architectures;

                                                namespace GFramework.Core.SourceGenerators.Abstractions.Architectures
                                                {
                                                    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                                    public sealed class AutoRegisterModuleAttribute : Attribute { }

                                                    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                                                    public sealed class RegisterModelAttribute : Attribute
                                                    {
                                                        public RegisterModelAttribute(Type modelType) { }
                                                    }

                                                    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                                                    public sealed class RegisterSystemAttribute : Attribute
                                                    {
                                                        public RegisterSystemAttribute(Type systemType) { }
                                                    }

                                                    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                                                    public sealed class RegisterUtilityAttribute : Attribute
                                                    {
                                                        public RegisterUtilityAttribute(Type utilityType) { }
                                                    }
                                                }

                                                namespace GFramework.Core.Abstractions.Architectures
                                                {
                                                    public interface IArchitecture
                                                    {
                                                        T RegisterModel<T>(T model) where T : GFramework.Core.Abstractions.Model.IModel;
                                                        T RegisterSystem<T>(T system) where T : GFramework.Core.Abstractions.Systems.ISystem;
                                                        T RegisterUtility<T>(T utility) where T : GFramework.Core.Abstractions.Utility.IUtility;
                                                    }
                                                }

                                                namespace GFramework.Core.Abstractions.Model
                                                {
                                                    public interface IModel { }
                                                }

                                                namespace GFramework.Core.Abstractions.Systems
                                                {
                                                    public interface ISystem { }
                                                }

                                                namespace GFramework.Core.Abstractions.Utility
                                                {
                                                    public interface IUtility { }
                                                }

                                                namespace TestApp
                                                {
                                                    using GFramework.Core.Abstractions.Model;
                                                    using GFramework.Core.Abstractions.Systems;
                                                    using GFramework.Core.Abstractions.Utility;
                                                    using GFramework.Core.SourceGenerators.Abstractions.Architectures;

                                                    public sealed class PlayerModel : IModel { }
                                                    public sealed class CombatSystem : ISystem { }
                                                    public sealed class AudioUtility : IUtility { }

                                                    [AutoRegisterModule]
                                                    [RegisterSystem(typeof(CombatSystem))]
                                                    [RegisterModel(typeof(PlayerModel))]
                                                    [RegisterUtility(typeof(AudioUtility))]
                                                    public partial class GameplayModule
                                                    {
                                                    }
                                                }
                                                """;

    private const string AttributeOrderExpected = """
                                                  // <auto-generated />
                                                  #nullable enable

                                                  namespace TestApp;

                                                  partial class GameplayModule
                                                  {
                                                      public void Install(global::GFramework.Core.Abstractions.Architectures.IArchitecture architecture)
                                                      {
                                                          architecture.RegisterSystem(new global::TestApp.CombatSystem());
                                                          architecture.RegisterModel(new global::TestApp.PlayerModel());
                                                          architecture.RegisterUtility(new global::TestApp.AudioUtility());
                                                      }
                                                  }

                                                  """;

    private const string DeterministicOrderCommonSource = """
                                                          using System;

                                                          namespace GFramework.Core.SourceGenerators.Abstractions.Architectures
                                                          {
                                                              [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                                              public sealed class AutoRegisterModuleAttribute : Attribute { }

                                                              [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                                                              public sealed class RegisterModelAttribute : Attribute
                                                              {
                                                                  public RegisterModelAttribute(Type modelType) { }
                                                              }

                                                              [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                                                              public sealed class RegisterSystemAttribute : Attribute
                                                              {
                                                                  public RegisterSystemAttribute(Type systemType) { }
                                                              }

                                                              [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                                                              public sealed class RegisterUtilityAttribute : Attribute
                                                              {
                                                                  public RegisterUtilityAttribute(Type utilityType) { }
                                                              }
                                                          }

                                                          namespace GFramework.Core.Abstractions.Architectures
                                                          {
                                                              public interface IArchitecture
                                                              {
                                                                  T RegisterModel<T>(T model) where T : GFramework.Core.Abstractions.Model.IModel;
                                                                  T RegisterSystem<T>(T system) where T : GFramework.Core.Abstractions.Systems.ISystem;
                                                                  T RegisterUtility<T>(T utility) where T : GFramework.Core.Abstractions.Utility.IUtility;
                                                              }
                                                          }

                                                          namespace GFramework.Core.Abstractions.Model
                                                          {
                                                              public interface IModel { }
                                                          }

                                                          namespace GFramework.Core.Abstractions.Systems
                                                          {
                                                              public interface ISystem { }
                                                          }

                                                          namespace GFramework.Core.Abstractions.Utility
                                                          {
                                                              public interface IUtility { }
                                                          }

                                                          namespace TestApp
                                                          {
                                                              using GFramework.Core.Abstractions.Model;
                                                              using GFramework.Core.Abstractions.Systems;
                                                              using GFramework.Core.Abstractions.Utility;

                                                              public sealed class PlayerModel : IModel { }
                                                              public sealed class CombatSystem : ISystem { }
                                                              public sealed class AudioUtility : IUtility { }
                                                          }
                                                          """;

    private const string DeterministicOrderPartASource = """
                                                         namespace TestApp
                                                         {
                                                             using GFramework.Core.SourceGenerators.Abstractions.Architectures;

                                                             // Padding ensures this attribute lives later in the file than the attributes in PartB.
                                                             // The generator should still place it first because PartA sorts before PartB.
                                                             // padding 01
                                                             // padding 02
                                                             // padding 03
                                                             // padding 04
                                                             // padding 05
                                                             // padding 06
                                                             // padding 07
                                                             // padding 08
                                                             // padding 09
                                                             // padding 10
                                                             [AutoRegisterModule]
                                                             [RegisterUtility(typeof(AudioUtility))]
                                                             public partial class GameplayModule
                                                             {
                                                             }
                                                         }
                                                         """;

    private const string DeterministicOrderPartBSource = """
                                                         namespace TestApp
                                                         {
                                                             using GFramework.Core.SourceGenerators.Abstractions.Architectures;

                                                             [RegisterSystem(typeof(CombatSystem))]
                                                             [RegisterModel(typeof(PlayerModel))]
                                                             public partial class GameplayModule
                                                             {
                                                             }
                                                         }
                                                         """;

    private const string DeterministicOrderExpected = """
                                                      // <auto-generated />
                                                      #nullable enable

                                                      namespace TestApp;

                                                      partial class GameplayModule
                                                      {
                                                          public void Install(global::GFramework.Core.Abstractions.Architectures.IArchitecture architecture)
                                                          {
                                                              architecture.RegisterUtility(new global::TestApp.AudioUtility());
                                                              architecture.RegisterSystem(new global::TestApp.CombatSystem());
                                                              architecture.RegisterModel(new global::TestApp.PlayerModel());
                                                          }
                                                      }

                                                      """;

    private const string TypeConstraintSource = """
                                                #nullable enable
                                                using System;
                                                using GFramework.Core.SourceGenerators.Abstractions.Architectures;

                                                namespace GFramework.Core.SourceGenerators.Abstractions.Architectures
                                                {
                                                    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                                    public sealed class AutoRegisterModuleAttribute : Attribute { }

                                                    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                                                    public sealed class RegisterModelAttribute : Attribute
                                                    {
                                                        public RegisterModelAttribute(Type modelType) { }
                                                    }

                                                    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                                                    public sealed class RegisterSystemAttribute : Attribute
                                                    {
                                                        public RegisterSystemAttribute(Type systemType) { }
                                                    }

                                                    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                                                    public sealed class RegisterUtilityAttribute : Attribute
                                                    {
                                                        public RegisterUtilityAttribute(Type utilityType) { }
                                                    }
                                                }

                                                namespace GFramework.Core.Abstractions.Architectures
                                                {
                                                    public interface IArchitecture
                                                    {
                                                        T RegisterModel<T>(T model) where T : GFramework.Core.Abstractions.Model.IModel;
                                                        T RegisterSystem<T>(T system) where T : GFramework.Core.Abstractions.Systems.ISystem;
                                                        T RegisterUtility<T>(T utility) where T : GFramework.Core.Abstractions.Utility.IUtility;
                                                    }
                                                }

                                                namespace GFramework.Core.Abstractions.Model
                                                {
                                                    public interface IModel { }
                                                }

                                                namespace GFramework.Core.Abstractions.Systems
                                                {
                                                    public interface ISystem { }
                                                }

                                                namespace GFramework.Core.Abstractions.Utility
                                                {
                                                    public interface IUtility { }
                                                }

                                                namespace TestApp
                                                {
                                                    using GFramework.Core.Abstractions.Model;
                                                    using GFramework.Core.SourceGenerators.Abstractions.Architectures;

                                                    public sealed class PlayerModel : IModel { }

                                                    [AutoRegisterModule]
                                                    [RegisterModel(typeof(PlayerModel))]
                                                    public partial class GameplayModule<TNullableRef, TNotNull, TUnmanaged>
                                                        where TNullableRef : class?
                                                        where TNotNull : notnull
                                                        where TUnmanaged : unmanaged
                                                    {
                                                    }
                                                }
                                                """;

    private const string TypeConstraintExpected = """
                                                  // <auto-generated />
                                                  #nullable enable

                                                  namespace TestApp;

                                                  partial class GameplayModule<TNullableRef, TNotNull, TUnmanaged>
                                                      where TNullableRef : class?
                                                      where TNotNull : notnull
                                                      where TUnmanaged : unmanaged
                                                  {
                                                      public void Install(global::GFramework.Core.Abstractions.Architectures.IArchitecture architecture)
                                                      {
                                                          architecture.RegisterModel(new global::TestApp.PlayerModel());
                                                      }
                                                  }

                                                  """;

    /// <summary>
    ///     验证同一声明上的注册特性会按照源码中的书写顺序生成安装代码。
    /// </summary>
    [Test]
    public Task Generates_Module_Install_Method_In_Attribute_Order()
    {
        return GeneratorTest<AutoRegisterModuleGenerator>.RunAsync(
            AttributeOrderSource,
            ("TestApp_GameplayModule.AutoRegisterModule.g.cs", AttributeOrderExpected));
    }

    /// <summary>
    ///     验证 partial 声明分布在多个文件时，生成器仍然会使用稳定的跨文件顺序生成注册代码。
    /// </summary>
    [Test]
    public async Task Generates_Module_Install_Method_In_Deterministic_Order_Across_Partial_Declarations()
    {
        var test = new CSharpSourceGeneratorTest<AutoRegisterModuleGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    ("Common.cs", DeterministicOrderCommonSource),
                    ("GameplayModule.PartA.cs", DeterministicOrderPartASource),
                    ("GameplayModule.PartB.cs", DeterministicOrderPartBSource)
                },
                GeneratedSources =
                {
                    (typeof(AutoRegisterModuleGenerator), "TestApp_GameplayModule.AutoRegisterModule.g.cs",
                        NormalizeLineEndings(DeterministicOrderExpected))
                }
            },
            DisabledDiagnostics = { "GF_Common_Trace_001" }
        };

        await test.RunAsync();
    }

    /// <summary>
    ///     验证生成器会保留可空引用、notnull 与 unmanaged 约束。
    /// </summary>
    [Test]
    public Task Generates_Type_Constraints_For_NullableReference_NotNull_And_Unmanaged()
    {
        return GeneratorTest<AutoRegisterModuleGenerator>.RunAsync(
            TypeConstraintSource,
            ("TestApp_GameplayModule.AutoRegisterModule.g.cs", TypeConstraintExpected));
    }

    /// <summary>
    ///     将测试快照统一为当前平台换行符，避免不同系统上的源生成输出比较出现伪差异。
    /// </summary>
    /// <param name="content">原始快照内容。</param>
    /// <returns>使用当前平台换行符的快照内容。</returns>
    private static string NormalizeLineEndings(string content)
    {
        return content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\n", Environment.NewLine, StringComparison.Ordinal);
    }
}
