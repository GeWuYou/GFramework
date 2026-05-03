// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Cqrs.SourceGenerators.Cqrs;
using GFramework.SourceGenerators.Tests.Core;

namespace GFramework.SourceGenerators.Tests.Cqrs;

/// <summary>
///     验证 CQRS 处理器注册生成器的输出与回退边界。
/// </summary>
[TestFixture]
public class CqrsHandlerRegistryGeneratorTests
{
    private const string HiddenNestedHandlerSelfRegistrationSource = """
                                                                    using System;

                                                                    namespace Microsoft.Extensions.DependencyInjection
                                                                    {
                                                                        public interface IServiceCollection { }

                                                                        public static class ServiceCollectionServiceExtensions
                                                                        {
                                                                            public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                        }
                                                                    }

                                                                    namespace GFramework.Core.Abstractions.Logging
                                                                    {
                                                                        public interface ILogger
                                                                        {
                                                                            void Debug(string msg);
                                                                        }
                                                                    }

                                                                    namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                    {
                                                                        public interface IRequest<TResponse> { }
                                                                        public interface INotification { }
                                                                        public interface IStreamRequest<TResponse> { }

                                                                        public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                        public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                        public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                    }

                                                                    namespace GFramework.Cqrs
                                                                    {
                                                                        public interface ICqrsHandlerRegistry
                                                                        {
                                                                            void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                        }

                                                                        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                        public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                        {
                                                                            public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                        }

                                                                        [AttributeUsage(AttributeTargets.Assembly)]
                                                                        public sealed class CqrsReflectionFallbackAttribute : Attribute
                                                                        {
                                                                            public CqrsReflectionFallbackAttribute(params string[] fallbackHandlerTypeNames) { }
                                                                        }
                                                                    }

                                                                    namespace TestApp
                                                                    {
                                                                        using GFramework.Cqrs.Abstractions.Cqrs;

                                                                        public sealed record VisibleRequest() : IRequest<string>;

                                                                        public sealed class Container
                                                                        {
                                                                            private sealed record HiddenRequest() : IRequest<string>;

                                                                            private sealed class HiddenHandler : IRequestHandler<HiddenRequest, string> { }
                                                                        }

                                                                        public sealed class VisibleHandler : IRequestHandler<VisibleRequest, string> { }
                                                                    }
                                                                    """;

    private const string HiddenNestedHandlerSelfRegistrationExpected = """
                                                                       // <auto-generated />
                                                                       #nullable enable

                                                                       [assembly: global::GFramework.Cqrs.CqrsHandlerRegistryAttribute(typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry))]

                                                                       namespace GFramework.Generated.Cqrs;

                                                                       internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry
                                                                       {
                                                                           public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::GFramework.Core.Abstractions.Logging.ILogger logger)
                                                                           {
                                                                               if (services is null)
                                                                                   throw new global::System.ArgumentNullException(nameof(services));
                                                                               if (logger is null)
                                                                                   throw new global::System.ArgumentNullException(nameof(logger));

                                                                               var registryAssembly = typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry).Assembly;

                                                                               var implementationType0 = registryAssembly.GetType("TestApp.Container+HiddenHandler", throwOnError: false, ignoreCase: false);
                                                                               if (implementationType0 is not null)
                                                                               {
                                                                                   var serviceType0_0Argument0 = registryAssembly.GetType("TestApp.Container+HiddenRequest", throwOnError: false, ignoreCase: false);
                                                                                   if (serviceType0_0Argument0 is not null)
                                                                                   {
                                                                                       var serviceType0_0 = typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType(serviceType0_0Argument0, typeof(string));
                                                                                       global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                           services,
                                                                                           serviceType0_0,
                                                                                           implementationType0);
                                                                                       logger.Debug("Registered CQRS handler TestApp.Container.HiddenHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<TestApp.Container.HiddenRequest, string>.");
                                                                                   }
                                                                               }
                                                                               global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                   services,
                                                                                   typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<global::TestApp.VisibleRequest, string>),
                                                                                   typeof(global::TestApp.VisibleHandler));
                                                                               logger.Debug("Registered CQRS handler TestApp.VisibleHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<TestApp.VisibleRequest, string>.");
                                                                           }
                                                                       }

                                                                       """;

    private const string HiddenImplementationDirectInterfaceRegistrationSource = """
                                                                              using System;

                                                                              namespace Microsoft.Extensions.DependencyInjection
                                                                              {
                                                                                  public interface IServiceCollection { }

                                                                                  public static class ServiceCollectionServiceExtensions
                                                                                  {
                                                                                      public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                                  }
                                                                              }

                                                                              namespace GFramework.Core.Abstractions.Logging
                                                                              {
                                                                                  public interface ILogger
                                                                                  {
                                                                                      void Debug(string msg);
                                                                                  }
                                                                              }

                                                                              namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                              {
                                                                                  public interface IRequest<TResponse> { }
                                                                                  public interface INotification { }
                                                                                  public interface IStreamRequest<TResponse> { }

                                                                                  public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                                  public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                                  public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                              }

                                                                              namespace GFramework.Cqrs
                                                                              {
                                                                                  public interface ICqrsHandlerRegistry
                                                                                  {
                                                                                      void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                                  }

                                                                                  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                                  public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                                  {
                                                                                      public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                                  }
                                                                              }

                                                                              namespace TestApp
                                                                              {
                                                                                  using GFramework.Cqrs.Abstractions.Cqrs;

                                                                                  public sealed record VisibleRequest() : IRequest<string>;

                                                                                  public sealed class Container
                                                                                  {
                                                                                      private sealed class HiddenHandler : IRequestHandler<VisibleRequest, string> { }
                                                                                  }
                                                                              }
                                                                              """;

    private const string HiddenImplementationDirectInterfaceRegistrationExpected = """
        // <auto-generated />
        #nullable enable

        [assembly: global::GFramework.Cqrs.CqrsHandlerRegistryAttribute(typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry))]

        namespace GFramework.Generated.Cqrs;

        internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry
        {
            public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::GFramework.Core.Abstractions.Logging.ILogger logger)
            {
                if (services is null)
                    throw new global::System.ArgumentNullException(nameof(services));
                if (logger is null)
                    throw new global::System.ArgumentNullException(nameof(logger));

                var registryAssembly = typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry).Assembly;

                var implementationType0 = registryAssembly.GetType("TestApp.Container+HiddenHandler", throwOnError: false, ignoreCase: false);
                if (implementationType0 is not null)
                {
                    global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                        services,
                        typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<global::TestApp.VisibleRequest, string>),
                        implementationType0);
                    logger.Debug("Registered CQRS handler TestApp.Container.HiddenHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<TestApp.VisibleRequest, string>.");
                }
            }
        }

        """;

    private const string HiddenArrayResponseFallbackSource = """
                                                            using System;

                                                            namespace Microsoft.Extensions.DependencyInjection
                                                            {
                                                                public interface IServiceCollection { }

                                                                public static class ServiceCollectionServiceExtensions
                                                                {
                                                                    public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                }
                                                            }

                                                            namespace GFramework.Core.Abstractions.Logging
                                                            {
                                                                public interface ILogger
                                                                {
                                                                    void Debug(string msg);
                                                                }
                                                            }

                                                            namespace GFramework.Cqrs.Abstractions.Cqrs
                                                            {
                                                                public interface IRequest<TResponse> { }
                                                                public interface INotification { }
                                                                public interface IStreamRequest<TResponse> { }

                                                                public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                            }

                                                            namespace GFramework.Cqrs
                                                            {
                                                                public interface ICqrsHandlerRegistry
                                                                {
                                                                    void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                }

                                                                [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                {
                                                                    public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                }
                                                            }

                                                            namespace TestApp
                                                            {
                                                                using GFramework.Cqrs.Abstractions.Cqrs;

                                                                public sealed class Container
                                                                {
                                                                    private sealed record HiddenResponse();

                                                                    private sealed record HiddenRequest() : IRequest<HiddenResponse[]>;

                                                                    private sealed class HiddenHandler : IRequestHandler<HiddenRequest, HiddenResponse[]> { }
                                                                }
                                                            }
                                                            """;

    private const string HiddenArrayResponseFallbackExpected = """
                                                               // <auto-generated />
                                                               #nullable enable

                                                               [assembly: global::GFramework.Cqrs.CqrsHandlerRegistryAttribute(typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry))]

                                                               namespace GFramework.Generated.Cqrs;

                                                               internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry
                                                               {
                                                                   public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::GFramework.Core.Abstractions.Logging.ILogger logger)
                                                                   {
                                                                       if (services is null)
                                                                           throw new global::System.ArgumentNullException(nameof(services));
                                                                       if (logger is null)
                                                                           throw new global::System.ArgumentNullException(nameof(logger));

                                                                       var registryAssembly = typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry).Assembly;

                                                                       var implementationType0 = registryAssembly.GetType("TestApp.Container+HiddenHandler", throwOnError: false, ignoreCase: false);
                                                                       if (implementationType0 is not null)
                                                                       {
                                                                           var serviceType0_0Argument0 = registryAssembly.GetType("TestApp.Container+HiddenRequest", throwOnError: false, ignoreCase: false);
                                                                           var serviceType0_0Argument1Element = registryAssembly.GetType("TestApp.Container+HiddenResponse", throwOnError: false, ignoreCase: false);
                                                                           if (serviceType0_0Argument0 is not null && serviceType0_0Argument1Element is not null)
                                                                           {
                                                                               var serviceType0_0 = typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType(serviceType0_0Argument0, serviceType0_0Argument1Element.MakeArrayType());
                                                                               global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                   services,
                                                                                   serviceType0_0,
                                                                                   implementationType0);
                                                                               logger.Debug("Registered CQRS handler TestApp.Container.HiddenHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<TestApp.Container.HiddenRequest, TestApp.Container.HiddenResponse[]>.");
                                                                           }
                                                                       }
                                                                   }
                                                               }

                                                               """;

    private const string HiddenMultiDimensionalArrayResponseSource = """
                                                                      using System;

                                                                      namespace Microsoft.Extensions.DependencyInjection
                                                                      {
                                                                          public interface IServiceCollection { }

                                                                          public static class ServiceCollectionServiceExtensions
                                                                          {
                                                                              public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                          }
                                                                      }

                                                                      namespace GFramework.Core.Abstractions.Logging
                                                                      {
                                                                          public interface ILogger
                                                                          {
                                                                              void Debug(string msg);
                                                                          }
                                                                      }

                                                                      namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                      {
                                                                          public interface IRequest<TResponse> { }
                                                                          public interface INotification { }
                                                                          public interface IStreamRequest<TResponse> { }

                                                                          public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                          public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                          public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                      }

                                                                      namespace GFramework.Cqrs
                                                                      {
                                                                          public interface ICqrsHandlerRegistry
                                                                          {
                                                                              void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                          }

                                                                          [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                          public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                          {
                                                                              public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                          }
                                                                      }

                                                                      namespace TestApp
                                                                      {
                                                                          using GFramework.Cqrs.Abstractions.Cqrs;

                                                                          public sealed class Container
                                                                          {
                                                                              private sealed record HiddenResponse();

                                                                              private sealed record HiddenRequest() : IRequest<HiddenResponse[,]>;

                                                                              private sealed class HiddenHandler : IRequestHandler<HiddenRequest, HiddenResponse[,]> { }
                                                                          }
                                                                      }
                                                                      """;

    private const string HiddenJaggedArrayResponseSource = """
                                                             using System;

                                                             namespace Microsoft.Extensions.DependencyInjection
                                                             {
                                                                 public interface IServiceCollection { }

                                                                 public static class ServiceCollectionServiceExtensions
                                                                 {
                                                                     public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                 }
                                                             }

                                                             namespace GFramework.Core.Abstractions.Logging
                                                             {
                                                                 public interface ILogger
                                                                 {
                                                                     void Debug(string msg);
                                                                 }
                                                             }

                                                             namespace GFramework.Cqrs.Abstractions.Cqrs
                                                             {
                                                                 public interface IRequest<TResponse> { }
                                                                 public interface INotification { }
                                                                 public interface IStreamRequest<TResponse> { }

                                                                 public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                 public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                 public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                             }

                                                             namespace GFramework.Cqrs
                                                             {
                                                                 public interface ICqrsHandlerRegistry
                                                                 {
                                                                     void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                 }

                                                                 [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                 public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                 {
                                                                     public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                 }
                                                             }

                                                             namespace TestApp
                                                             {
                                                                 using GFramework.Cqrs.Abstractions.Cqrs;

                                                                 public sealed class Container
                                                                 {
                                                                     private sealed record HiddenResponse();

                                                                     private sealed record HiddenRequest() : IRequest<HiddenResponse[][]>;

                                                                     private sealed class HiddenHandler : IRequestHandler<HiddenRequest, HiddenResponse[][]> { }
                                                                 }
                                                             }
                                                             """;

    private const string HiddenGenericEnvelopeResponseSource = """
                                                               using System;

                                                               namespace Microsoft.Extensions.DependencyInjection
                                                               {
                                                                   public interface IServiceCollection { }

                                                                   public static class ServiceCollectionServiceExtensions
                                                                   {
                                                                       public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                   }
                                                               }

                                                               namespace GFramework.Core.Abstractions.Logging
                                                               {
                                                                   public interface ILogger
                                                                   {
                                                                       void Debug(string msg);
                                                                   }
                                                               }

                                                               namespace GFramework.Cqrs.Abstractions.Cqrs
                                                               {
                                                                   public interface IRequest<TResponse> { }
                                                                   public interface INotification { }
                                                                   public interface IStreamRequest<TResponse> { }

                                                                   public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                   public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                   public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                               }

                                                               namespace GFramework.Cqrs
                                                               {
                                                                   public interface ICqrsHandlerRegistry
                                                                   {
                                                                       void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                   }

                                                                   [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                   public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                   {
                                                                       public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                   }
                                                               }

                                                               namespace TestApp
                                                               {
                                                                   using GFramework.Cqrs.Abstractions.Cqrs;

                                                                   public sealed class Container
                                                                   {
                                                                       private sealed class HiddenEnvelope<T> { }

                                                                       private sealed record HiddenRequest() : IRequest<HiddenEnvelope<string>>;

                                                                       private sealed class HiddenHandler : IRequestHandler<HiddenRequest, HiddenEnvelope<string>> { }
                                                                   }
                                                               }
                                                               """;

    private const string HiddenGenericEnvelopeResponseExpected = """
                                                                 // <auto-generated />
                                                                 #nullable enable

                                                                 [assembly: global::GFramework.Cqrs.CqrsHandlerRegistryAttribute(typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry))]

                                                                 namespace GFramework.Generated.Cqrs;

                                                                 internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry
                                                                 {
                                                                     public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::GFramework.Core.Abstractions.Logging.ILogger logger)
                                                                     {
                                                                         if (services is null)
                                                                             throw new global::System.ArgumentNullException(nameof(services));
                                                                         if (logger is null)
                                                                             throw new global::System.ArgumentNullException(nameof(logger));

                                                                         var registryAssembly = typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry).Assembly;

                                                                         var implementationType0 = registryAssembly.GetType("TestApp.Container+HiddenHandler", throwOnError: false, ignoreCase: false);
                                                                         if (implementationType0 is not null)
                                                                         {
                                                                             var serviceType0_0Argument0 = registryAssembly.GetType("TestApp.Container+HiddenRequest", throwOnError: false, ignoreCase: false);
                                                                             var serviceType0_0Argument1GenericDefinition = registryAssembly.GetType("TestApp.Container+HiddenEnvelope`1", throwOnError: false, ignoreCase: false);
                                                                             if (serviceType0_0Argument0 is not null && serviceType0_0Argument1GenericDefinition is not null)
                                                                             {
                                                                                 var serviceType0_0 = typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType(serviceType0_0Argument0, serviceType0_0Argument1GenericDefinition.MakeGenericType(typeof(string)));
                                                                                 global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                     services,
                                                                                     serviceType0_0,
                                                                                     implementationType0);
                                                                                 logger.Debug("Registered CQRS handler TestApp.Container.HiddenHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<TestApp.Container.HiddenRequest, TestApp.Container.HiddenEnvelope<string>>.");
                                                                             }
                                                                         }
                                                                     }
                                                                 }

                                                                 """;

    private const string MixedDirectAndPreciseRegistrationsExpected = """
                                                                      // <auto-generated />
                                                                      #nullable enable

                                                                      [assembly: global::GFramework.Cqrs.CqrsHandlerRegistryAttribute(typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry))]

                                                                      namespace GFramework.Generated.Cqrs;

                                                                      internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry
                                                                      {
                                                                          public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::GFramework.Core.Abstractions.Logging.ILogger logger)
                                                                          {
                                                                              if (services is null)
                                                                                  throw new global::System.ArgumentNullException(nameof(services));
                                                                              if (logger is null)
                                                                                  throw new global::System.ArgumentNullException(nameof(logger));

                                                                              var registryAssembly = typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry).Assembly;

                                                                              var implementationType0 = typeof(global::TestApp.Container.MixedHandler);
                                                                              if (implementationType0 is not null)
                                                                              {
                                                                                  var serviceType0_0Argument0 = registryAssembly.GetType("TestApp.Container+HiddenRequest", throwOnError: false, ignoreCase: false);
                                                                                  var serviceType0_0Argument1Element = registryAssembly.GetType("TestApp.Container+HiddenResponse", throwOnError: false, ignoreCase: false);
                                                                                  if (serviceType0_0Argument0 is not null && serviceType0_0Argument1Element is not null)
                                                                                  {
                                                                                      var serviceType0_0 = typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType(serviceType0_0Argument0, serviceType0_0Argument1Element.MakeArrayType());
                                                                                      global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                          services,
                                                                                          serviceType0_0,
                                                                                          implementationType0);
                                                                                      logger.Debug("Registered CQRS handler TestApp.Container.MixedHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<TestApp.Container.HiddenRequest, TestApp.Container.HiddenResponse[]>.");
                                                                                  }
                                                                                  global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                      services,
                                                                                      typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<global::TestApp.VisibleRequest, string>),
                                                                                      implementationType0);
                                                                                  logger.Debug("Registered CQRS handler TestApp.Container.MixedHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<TestApp.VisibleRequest, string>.");
                                                                              }
                                                                          }
                                                                      }

                                                                      """;

    private const string MixedReflectedImplementationAndPreciseRegistrationsExpected = """
        // <auto-generated />
        #nullable enable

        [assembly: global::GFramework.Cqrs.CqrsHandlerRegistryAttribute(typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry))]

        namespace GFramework.Generated.Cqrs;

        internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry
        {
            public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::GFramework.Core.Abstractions.Logging.ILogger logger)
            {
                if (services is null)
                    throw new global::System.ArgumentNullException(nameof(services));
                if (logger is null)
                    throw new global::System.ArgumentNullException(nameof(logger));

                var registryAssembly = typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry).Assembly;

                var implementationType0 = registryAssembly.GetType("TestApp.Container+HiddenMixedHandler", throwOnError: false, ignoreCase: false);
                if (implementationType0 is not null)
                {
                    var serviceType0_0Argument0 = registryAssembly.GetType("TestApp.Container+HiddenRequest", throwOnError: false, ignoreCase: false);
                    var serviceType0_0Argument1Element = registryAssembly.GetType("TestApp.Container+HiddenResponse", throwOnError: false, ignoreCase: false);
                    if (serviceType0_0Argument0 is not null && serviceType0_0Argument1Element is not null)
                    {
                        var serviceType0_0 = typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType(serviceType0_0Argument0, serviceType0_0Argument1Element.MakeArrayType());
                        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                            services,
                            serviceType0_0,
                            implementationType0);
                        logger.Debug("Registered CQRS handler TestApp.Container.HiddenMixedHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<TestApp.Container.HiddenRequest, TestApp.Container.HiddenResponse[]>.");
                    }
                    global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                        services,
                        typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<global::TestApp.VisibleRequest, string>),
                        implementationType0);
                    logger.Debug("Registered CQRS handler TestApp.Container.HiddenMixedHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<TestApp.VisibleRequest, string>.");
                }
            }
        }

        """;

    private const string ExternalAssemblyPreciseLookupExpected = """
                                                                 // <auto-generated />
                                                                 #nullable enable

                                                                 [assembly: global::GFramework.Cqrs.CqrsHandlerRegistryAttribute(typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry))]

                                                                 namespace GFramework.Generated.Cqrs;

                                                                 internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry
                                                                 {
                                                                     public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::GFramework.Core.Abstractions.Logging.ILogger logger)
                                                                     {
                                                                         if (services is null)
                                                                             throw new global::System.ArgumentNullException(nameof(services));
                                                                         if (logger is null)
                                                                             throw new global::System.ArgumentNullException(nameof(logger));

                                                                         var registryAssembly = typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry).Assembly;

                                                                         var implementationType0 = typeof(global::TestApp.DerivedHandler);
                                                                         if (implementationType0 is not null)
                                                                         {
                                                                             var serviceType0_0Argument0 = ResolveReferencedAssemblyType("Dependency, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Dep.VisibilityScope+ProtectedRequest");
                                                                             var serviceType0_0Argument1Element = ResolveReferencedAssemblyType("Dependency, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Dep.VisibilityScope+ProtectedResponse");
                                                                             if (serviceType0_0Argument0 is not null && serviceType0_0Argument1Element is not null)
                                                                             {
                                                                                 var serviceType0_0 = typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType(serviceType0_0Argument0, serviceType0_0Argument1Element.MakeArrayType());
                                                                                 global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                     services,
                                                                                     serviceType0_0,
                                                                                     implementationType0);
                                                                                 logger.Debug("Registered CQRS handler TestApp.DerivedHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<Dep.VisibilityScope.ProtectedRequest, Dep.VisibilityScope.ProtectedResponse[]>.");
                                                                             }
                                                                             global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                 services,
                                                                                 typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<global::Dep.VisibleRequest, string>),
                                                                                 implementationType0);
                                                                             logger.Debug("Registered CQRS handler TestApp.DerivedHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<Dep.VisibleRequest, string>.");
                                                                         }
                                                                     }

                                                                     private static global::System.Type? ResolveReferencedAssemblyType(string assemblyIdentity, string typeMetadataName)
                                                                     {
                                                                         var assembly = ResolveReferencedAssembly(assemblyIdentity);
                                                                         return assembly?.GetType(typeMetadataName, throwOnError: false, ignoreCase: false);
                                                                     }

                                                                     private static global::System.Reflection.Assembly? ResolveReferencedAssembly(string assemblyIdentity)
                                                                     {
                                                                         global::System.Reflection.AssemblyName targetAssemblyName;
                                                                         try
                                                                         {
                                                                             targetAssemblyName = new global::System.Reflection.AssemblyName(assemblyIdentity);
                                                                         }
                                                                         catch
                                                                         {
                                                                             return null;
                                                                         }

                                                                         foreach (var assembly in global::System.AppDomain.CurrentDomain.GetAssemblies())
                                                                         {
                                                                             if (global::System.Reflection.AssemblyName.ReferenceMatchesDefinition(targetAssemblyName, assembly.GetName()))
                                                                                 return assembly;
                                                                         }

                                                                         try
                                                                         {
                                                                             return global::System.Reflection.Assembly.Load(targetAssemblyName);
                                                                         }
                                                                         catch
                                                                         {
                                                                             return null;
                                                                         }
                                                                     }
                                                                 }

                                                                 """;

    private const string AssemblyLevelCqrsHandlerRegistrySource = """
                                                                  using System;
                                                                  using System.Collections.Generic;

                                                                  namespace Microsoft.Extensions.DependencyInjection
                                                                  {
                                                                      public interface IServiceCollection { }

                                                                      public static class ServiceCollectionServiceExtensions
                                                                      {
                                                                          public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                      }
                                                                  }

                                                                  namespace GFramework.Core.Abstractions.Logging
                                                                  {
                                                                      public interface ILogger
                                                                      {
                                                                          void Debug(string msg);
                                                                      }
                                                                  }

                                                                  namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                  {
                                                                      public interface IRequest<TResponse> { }
                                                                      public interface INotification { }
                                                                      public interface IStreamRequest<TResponse> { }

                                                                      public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                      public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                      public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                  }

                                                                  namespace GFramework.Cqrs
                                                                  {
                                                                      public interface ICqrsHandlerRegistry
                                                                      {
                                                                          void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                      }

                                                                      [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                      public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                      {
                                                                          public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                      }

                                                                      [AttributeUsage(AttributeTargets.Assembly)]
                                                                      public sealed class CqrsReflectionFallbackAttribute : Attribute
                                                                      {
                                                                          public CqrsReflectionFallbackAttribute(params string[] fallbackHandlerTypeNames) { }
                                                                      }
                                                                  }

                                                                  namespace TestApp
                                                                  {
                                                                      using GFramework.Cqrs.Abstractions.Cqrs;

                                                                      public sealed record PingQuery() : IRequest<string>;
                                                                      public sealed record DomainEvent() : INotification;
                                                                      public sealed record NumberStream() : IStreamRequest<int>;

                                                                      public sealed class ZetaNotificationHandler : INotificationHandler<DomainEvent> { }
                                                                      public sealed class AlphaQueryHandler : IRequestHandler<PingQuery, string> { }
                                                                      public sealed class StreamHandler : IStreamRequestHandler<NumberStream, int> { }
                                                                  }
                                                                  """;

    private const string AssemblyLevelCqrsHandlerRegistryExpected = """
                                                                    // <auto-generated />
                                                                    #nullable enable

                                                                    [assembly: global::GFramework.Cqrs.CqrsHandlerRegistryAttribute(typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry))]

                                                                    namespace GFramework.Generated.Cqrs;

                                                                    internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry
                                                                    {
                                                                        public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::GFramework.Core.Abstractions.Logging.ILogger logger)
                                                                        {
                                                                            if (services is null)
                                                                                throw new global::System.ArgumentNullException(nameof(services));
                                                                            if (logger is null)
                                                                                throw new global::System.ArgumentNullException(nameof(logger));

                                                                            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                services,
                                                                                typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<global::TestApp.PingQuery, string>),
                                                                                typeof(global::TestApp.AlphaQueryHandler));
                                                                            logger.Debug("Registered CQRS handler TestApp.AlphaQueryHandler as GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<TestApp.PingQuery, string>.");
                                                                            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                services,
                                                                                typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<global::TestApp.NumberStream, int>),
                                                                                typeof(global::TestApp.StreamHandler));
                                                                            logger.Debug("Registered CQRS handler TestApp.StreamHandler as GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<TestApp.NumberStream, int>.");
                                                                            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(
                                                                                services,
                                                                                typeof(global::GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<global::TestApp.DomainEvent>),
                                                                                typeof(global::TestApp.ZetaNotificationHandler));
                                                                            logger.Debug("Registered CQRS handler TestApp.ZetaNotificationHandler as GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<TestApp.DomainEvent>.");
                                                                        }
                                                                    }

                                                                    """;

    // Keep large source fixtures at class scope so MA0051 reduction stays behavior-neutral for generator tests.
    private const string HiddenPointerResponseCompilationErrorSource = """
                                                                       using System;

                                                                       namespace Microsoft.Extensions.DependencyInjection
                                                                       {
                                                                           public interface IServiceCollection { }

                                                                           public static class ServiceCollectionServiceExtensions
                                                                           {
                                                                               public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                           }
                                                                       }

                                                                       namespace GFramework.Core.Abstractions.Logging
                                                                       {
                                                                           public interface ILogger
                                                                           {
                                                                               void Debug(string msg);
                                                                           }
                                                                       }

                                                                       namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                       {
                                                                           public interface IRequest<TResponse> { }
                                                                           public interface INotification { }
                                                                           public interface IStreamRequest<TResponse> { }

                                                                           public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                           public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                           public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                       }

                                                                       namespace GFramework.Cqrs
                                                                       {
                                                                           public interface ICqrsHandlerRegistry
                                                                           {
                                                                               void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                           }

                                                                           [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                           public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                           {
                                                                               public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                           }
                                                                       }

                                                                       namespace TestApp
                                                                       {
                                                                           using GFramework.Cqrs.Abstractions.Cqrs;

                                                                           public sealed class Container
                                                                           {
                                                                               private unsafe struct HiddenResponse
                                                                               {
                                                                               }

                                                                               private unsafe sealed record HiddenRequest() : IRequest<HiddenResponse*>;

                                                                               public unsafe sealed class HiddenHandler : IRequestHandler<HiddenRequest, HiddenResponse*>
                                                                               {
                                                                               }
                                                                           }
                                                                       }
                                                                       """;

    private const string MixedDirectAndPreciseRegistrationsSource = """
                                                                     using System;

                                                                     namespace Microsoft.Extensions.DependencyInjection
                                                                     {
                                                                         public interface IServiceCollection { }

                                                                         public static class ServiceCollectionServiceExtensions
                                                                         {
                                                                             public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                         }
                                                                     }

                                                                     namespace GFramework.Core.Abstractions.Logging
                                                                     {
                                                                         public interface ILogger
                                                                         {
                                                                             void Debug(string msg);
                                                                         }
                                                                     }

                                                                     namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                     {
                                                                         public interface IRequest<TResponse> { }
                                                                         public interface INotification { }
                                                                         public interface IStreamRequest<TResponse> { }

                                                                         public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                         public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                         public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                     }

                                                                     namespace GFramework.Cqrs
                                                                     {
                                                                         public interface ICqrsHandlerRegistry
                                                                         {
                                                                             void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                         }

                                                                         [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                         public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                         {
                                                                             public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                         }
                                                                     }

                                                                     namespace TestApp
                                                                     {
                                                                         using GFramework.Cqrs.Abstractions.Cqrs;

                                                                         public sealed record VisibleRequest() : IRequest<string>;

                                                                         public sealed class Container
                                                                         {
                                                                             private sealed record HiddenResponse();

                                                                             private sealed record HiddenRequest() : IRequest<HiddenResponse[]>;

                                                                             public sealed class MixedHandler :
                                                                                 IRequestHandler<HiddenRequest, HiddenResponse[]>,
                                                                                 IRequestHandler<VisibleRequest, string>
                                                                             {
                                                                             }
                                                                         }
                                                                     }
                                                                     """;

    private const string MixedReflectedImplementationAndPreciseRegistrationsSource = """
                                                                                     using System;

                                                                                     namespace Microsoft.Extensions.DependencyInjection
                                                                                     {
                                                                                         public interface IServiceCollection { }

                                                                                         public static class ServiceCollectionServiceExtensions
                                                                                         {
                                                                                             public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                                         }
                                                                                     }

                                                                                     namespace GFramework.Core.Abstractions.Logging
                                                                                     {
                                                                                         public interface ILogger
                                                                                         {
                                                                                             void Debug(string msg);
                                                                                         }
                                                                                     }

                                                                                     namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                                     {
                                                                                         public interface IRequest<TResponse> { }
                                                                                         public interface INotification { }
                                                                                         public interface IStreamRequest<TResponse> { }

                                                                                         public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                                         public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                                         public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                                     }

                                                                                     namespace GFramework.Cqrs
                                                                                     {
                                                                                         public interface ICqrsHandlerRegistry
                                                                                         {
                                                                                             void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                                         }

                                                                                         [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                                         public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                                         {
                                                                                             public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                                         }
                                                                                     }

                                                                                     namespace TestApp
                                                                                     {
                                                                                         using GFramework.Cqrs.Abstractions.Cqrs;

                                                                                         public sealed record VisibleRequest() : IRequest<string>;

                                                                                         public sealed class Container
                                                                                         {
                                                                                             private sealed record HiddenResponse();

                                                                                             private sealed record HiddenRequest() : IRequest<HiddenResponse[]>;

                                                                                             private sealed class HiddenMixedHandler :
                                                                                                 IRequestHandler<HiddenRequest, HiddenResponse[]>,
                                                                                                 IRequestHandler<VisibleRequest, string>
                                                                                             {
                                                                                             }
                                                                                         }
                                                                                     }
                                                                                     """;

    private const string ExternalProtectedTypeContractsSource = """
                                                                 namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                 {
                                                                     public interface IRequest<TResponse> { }
                                                                     public interface INotification { }
                                                                     public interface IStreamRequest<TResponse> { }

                                                                     public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                     public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                     public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                 }
                                                                 """;

    private const string ExternalProtectedTypeDependencySource = """
                                                                  using GFramework.Cqrs.Abstractions.Cqrs;

                                                                  namespace Dep;

                                                                  public sealed record VisibleRequest() : IRequest<string>;

                                                                  public abstract class VisibilityScope
                                                                  {
                                                                      protected internal sealed record ProtectedResponse();

                                                                      protected internal sealed record ProtectedRequest() : IRequest<ProtectedResponse[]>;
                                                                  }

                                                                  public abstract class HandlerBase :
                                                                      VisibilityScope,
                                                                      IRequestHandler<VisibleRequest, string>,
                                                                      IRequestHandler<VisibilityScope.ProtectedRequest, VisibilityScope.ProtectedResponse[]>
                                                                  {
                                                                  }
                                                                  """;

    private const string ExternalProtectedTypeLookupSource = """
                                                              using System;
                                                              using Dep;

                                                              namespace Microsoft.Extensions.DependencyInjection
                                                              {
                                                                  public interface IServiceCollection { }

                                                                  public static class ServiceCollectionServiceExtensions
                                                                  {
                                                                      public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                  }
                                                              }

                                                              namespace GFramework.Core.Abstractions.Logging
                                                              {
                                                                  public interface ILogger
                                                                  {
                                                                      void Debug(string msg);
                                                                  }
                                                              }

                                                              namespace GFramework.Cqrs
                                                              {
                                                                  public interface ICqrsHandlerRegistry
                                                                  {
                                                                      void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                  }

                                                                  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                  public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                  {
                                                                      public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                  }
                                                              }

                                                              namespace TestApp
                                                              {
                                                                  public sealed class DerivedHandler : HandlerBase
                                                                  {
                                                                  }
                                                              }
                                                              """;

    private const string ExternalProtectedMultiDimensionalTypeDependencySource = """
                                                                                  using GFramework.Cqrs.Abstractions.Cqrs;

                                                                                  namespace Dep;

                                                                                  public abstract class VisibilityScope
                                                                                  {
                                                                                      protected internal sealed record ProtectedResponse();

                                                                                      protected internal sealed record ProtectedRequest() : IRequest<ProtectedResponse[,]>;
                                                                                  }

                                                                                  public abstract class HandlerBase :
                                                                                      IRequestHandler<VisibilityScope.ProtectedRequest, VisibilityScope.ProtectedResponse[,]>
                                                                                  {
                                                                                  }
                                                                                  """;

    private const string ExternalProtectedGenericDefinitionDependencySource = """
                                                                              using GFramework.Cqrs.Abstractions.Cqrs;

                                                                              namespace Dep;

                                                                              public abstract class VisibilityScope
                                                                              {
                                                                                  protected internal sealed class ProtectedEnvelope<T>
                                                                                  {
                                                                                  }

                                                                                  protected internal sealed record ProtectedRequest() : IRequest<ProtectedEnvelope<string>>;
                                                                              }

                                                                              public abstract class HandlerBase :
                                                                                  IRequestHandler<VisibilityScope.ProtectedRequest, VisibilityScope.ProtectedEnvelope<string>>
                                                                              {
                                                                              }
                                                                              """;

    private const string LegacyFallbackMarkerHiddenHandlerSource = """
                                                                    using System;

                                                                    namespace Microsoft.Extensions.DependencyInjection
                                                                    {
                                                                        public interface IServiceCollection { }

                                                                        public static class ServiceCollectionServiceExtensions
                                                                        {
                                                                            public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                        }
                                                                    }

                                                                    namespace GFramework.Core.Abstractions.Logging
                                                                    {
                                                                        public interface ILogger
                                                                        {
                                                                            void Debug(string msg);
                                                                        }
                                                                    }

                                                                    namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                    {
                                                                        public interface IRequest<TResponse> { }
                                                                        public interface INotification { }
                                                                        public interface IStreamRequest<TResponse> { }

                                                                        public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                        public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                        public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                    }

                                                                    namespace GFramework.Cqrs
                                                                    {
                                                                        public interface ICqrsHandlerRegistry
                                                                        {
                                                                            void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                        }

                                                                        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                        public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                        {
                                                                            public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                        }

                                                                        [AttributeUsage(AttributeTargets.Assembly)]
                                                                        public sealed class CqrsReflectionFallbackAttribute : Attribute
                                                                        {
                                                                            public CqrsReflectionFallbackAttribute() { }
                                                                        }
                                                                    }

                                                                    namespace TestApp
                                                                    {
                                                                        using GFramework.Cqrs.Abstractions.Cqrs;

                                                                        public sealed record VisibleRequest() : IRequest<string>;

                                                                        public sealed class Container
                                                                        {
                                                                            private sealed record HiddenRequest() : IRequest<string>;

                                                                            private sealed class HiddenHandler : IRequestHandler<HiddenRequest, string> { }
                                                                        }

                                                                        public sealed class VisibleHandler : IRequestHandler<VisibleRequest, string> { }
                                                                    }
                                                                    """;

    private const string FallbackMarkerUnavailableHiddenHandlerSource = """
                                                                        using System;

                                                                        namespace Microsoft.Extensions.DependencyInjection
                                                                        {
                                                                            public interface IServiceCollection { }

                                                                            public static class ServiceCollectionServiceExtensions
                                                                            {
                                                                                public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                            }
                                                                        }

                                                                        namespace GFramework.Core.Abstractions.Logging
                                                                        {
                                                                            public interface ILogger
                                                                            {
                                                                                void Debug(string msg);
                                                                            }
                                                                        }

                                                                        namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                        {
                                                                            public interface IRequest<TResponse> { }
                                                                            public interface INotification { }
                                                                            public interface IStreamRequest<TResponse> { }

                                                                            public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                            public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                            public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                        }

                                                                        namespace GFramework.Cqrs
                                                                        {
                                                                            public interface ICqrsHandlerRegistry
                                                                            {
                                                                                void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                            }

                                                                            [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                            public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                            {
                                                                                public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                            }
                                                                        }

                                                                        namespace TestApp
                                                                        {
                                                                            using GFramework.Cqrs.Abstractions.Cqrs;

                                                                            public sealed record VisibleRequest() : IRequest<string>;

                                                                            public sealed class Container
                                                                            {
                                                                                private sealed record HiddenRequest() : IRequest<string>;

                                                                                private sealed class HiddenHandler : IRequestHandler<HiddenRequest, string> { }
                                                                            }

                                                                            public sealed class VisibleHandler : IRequestHandler<VisibleRequest, string> { }
                                                                        }
                                                                        """;

    private const string MissingFallbackAttributeDiagnosticSource = """
                                                                    using System;

                                                                    namespace Microsoft.Extensions.DependencyInjection
                                                                    {
                                                                        public interface IServiceCollection { }

                                                                        public static class ServiceCollectionServiceExtensions
                                                                        {
                                                                            public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                        }
                                                                    }

                                                                    namespace GFramework.Core.Abstractions.Logging
                                                                    {
                                                                        public interface ILogger
                                                                        {
                                                                            void Debug(string msg);
                                                                        }
                                                                    }

                                                                    namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                    {
                                                                        public interface IRequest<TResponse> { }
                                                                        public interface INotification { }
                                                                        public interface IStreamRequest<TResponse> { }

                                                                        public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                        public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                        public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                    }

                                                                    namespace GFramework.Cqrs
                                                                    {
                                                                        public interface ICqrsHandlerRegistry
                                                                        {
                                                                            void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                        }

                                                                        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                        public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                        {
                                                                            public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                        }
                                                                    }

                                                                    namespace TestApp
                                                                    {
                                                                        using GFramework.Cqrs.Abstractions.Cqrs;

                                                                        public sealed class Container
                                                                        {
                                                                            private unsafe struct HiddenResponse
                                                                            {
                                                                            }

                                                                            private unsafe sealed record HiddenRequest() : IRequest<delegate* unmanaged<HiddenResponse>>;

                                                                            public unsafe sealed class HiddenHandler : IRequestHandler<HiddenRequest, delegate* unmanaged<HiddenResponse>>
                                                                            {
                                                                            }
                                                                        }
                                                                    }
                                                                    """;

    private const string UnresolvedErrorTypeRuntimeLookupSource = """
                                                                  using System;

                                                                  namespace Microsoft.Extensions.DependencyInjection
                                                                  {
                                                                      public interface IServiceCollection { }

                                                                      public static class ServiceCollectionServiceExtensions
                                                                      {
                                                                          public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                      }
                                                                  }

                                                                  namespace GFramework.Core.Abstractions.Logging
                                                                  {
                                                                      public interface ILogger
                                                                      {
                                                                          void Debug(string msg);
                                                                      }
                                                                  }

                                                                  namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                  {
                                                                      public interface IRequest<TResponse> { }
                                                                      public interface INotification { }
                                                                      public interface IStreamRequest<TResponse> { }

                                                                      public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                      public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                      public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                  }

                                                                  namespace GFramework.Cqrs
                                                                  {
                                                                      public interface ICqrsHandlerRegistry
                                                                      {
                                                                          void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                      }

                                                                      [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                      public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                      {
                                                                          public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                      }

                                                                      [AttributeUsage(AttributeTargets.Assembly)]
                                                                      public sealed class CqrsReflectionFallbackAttribute : Attribute
                                                                      {
                                                                          public CqrsReflectionFallbackAttribute(params string[] fallbackHandlerTypeNames) { }
                                                                      }
                                                                  }

                                                                  namespace TestApp
                                                                  {
                                                                      using GFramework.Cqrs.Abstractions.Cqrs;

                                                                      public sealed record BrokenRequest() : IRequest<MissingResponse>;

                                                                      public sealed class BrokenHandler : IRequestHandler<BrokenRequest, MissingResponse>
                                                                      {
                                                                      }
                                                                  }
                                                                  """;

    private const string DynamicResponseNormalizationSource = """
                                                              using System;

                                                              namespace Microsoft.Extensions.DependencyInjection
                                                              {
                                                                  public interface IServiceCollection { }

                                                                  public static class ServiceCollectionServiceExtensions
                                                                  {
                                                                      public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                  }
                                                              }

                                                              namespace GFramework.Core.Abstractions.Logging
                                                              {
                                                                  public interface ILogger
                                                                  {
                                                                      void Debug(string msg);
                                                                  }
                                                              }

                                                              namespace GFramework.Cqrs.Abstractions.Cqrs
                                                              {
                                                                  public interface IRequest<TResponse> { }
                                                                  public interface INotification { }
                                                                  public interface IStreamRequest<TResponse> { }

                                                                  public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                  public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                  public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                              }

                                                              namespace GFramework.Cqrs
                                                              {
                                                                  public interface ICqrsHandlerRegistry
                                                                  {
                                                                      void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                  }

                                                                  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                  public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                  {
                                                                      public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                  }
                                                              }

                                                              namespace TestApp
                                                              {
                                                                  using GFramework.Cqrs.Abstractions.Cqrs;

                                                                  public sealed record DynamicRequest() : IRequest<dynamic>;

                                                                  public sealed class DynamicHandler : IRequestHandler<DynamicRequest, dynamic>
                                                                  {
                                                                  }
                                                              }
                                                              """;

    private const string AssemblyLevelFallbackMetadataSource = """
                                                               using System;

                                                               namespace Microsoft.Extensions.DependencyInjection
                                                               {
                                                                   public interface IServiceCollection { }

                                                                   public static class ServiceCollectionServiceExtensions
                                                                   {
                                                                       public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                   }
                                                               }

                                                               namespace GFramework.Core.Abstractions.Logging
                                                               {
                                                                   public interface ILogger
                                                                   {
                                                                       void Debug(string msg);
                                                                   }
                                                               }

                                                               namespace GFramework.Cqrs.Abstractions.Cqrs
                                                               {
                                                                   public interface IRequest<TResponse> { }
                                                                   public interface INotification { }
                                                                   public interface IStreamRequest<TResponse> { }

                                                                   public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                   public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                   public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                               }

                                                               namespace GFramework.Cqrs
                                                               {
                                                                   public interface ICqrsHandlerRegistry
                                                                   {
                                                                       void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                   }

                                                                   [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                   public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                   {
                                                                       public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                   }

                                                                   [AttributeUsage(AttributeTargets.Assembly)]
                                                                   public sealed class CqrsReflectionFallbackAttribute : Attribute
                                                                   {
                                                                       public CqrsReflectionFallbackAttribute(params string[] fallbackHandlerTypeNames) { }
                                                                   }
                                                               }

                                                               namespace TestApp
                                                               {
                                                                   using GFramework.Cqrs.Abstractions.Cqrs;

                                                                   public sealed class Container
                                                                   {
                                                                       private unsafe struct AlphaResponse
                                                                       {
                                                                       }

                                                                       private unsafe struct BetaResponse
                                                                       {
                                                                       }

                                                                       private unsafe sealed record AlphaRequest() : IRequest<delegate* unmanaged<AlphaResponse>>;

                                                                       private unsafe sealed record BetaRequest() : IRequest<delegate* unmanaged<BetaResponse>>;

                                                                       public unsafe sealed class BetaHandler : IRequestHandler<BetaRequest, delegate* unmanaged<BetaResponse>>
                                                                       {
                                                                       }

                                                                       public unsafe sealed class AlphaHandler : IRequestHandler<AlphaRequest, delegate* unmanaged<AlphaResponse>>
                                                                       {
                                                                       }
                                                                   }
                                                               }
                                                               """;

    private const string AssemblyLevelDirectFallbackMetadataSource = """
                                                                     using System;

                                                                     namespace Microsoft.Extensions.DependencyInjection
                                                                     {
                                                                         public interface IServiceCollection { }

                                                                         public static class ServiceCollectionServiceExtensions
                                                                         {
                                                                             public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                         }
                                                                     }

                                                                     namespace GFramework.Core.Abstractions.Logging
                                                                     {
                                                                         public interface ILogger
                                                                         {
                                                                             void Debug(string msg);
                                                                         }
                                                                     }

                                                                     namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                     {
                                                                         public interface IRequest<TResponse> { }
                                                                         public interface INotification { }
                                                                         public interface IStreamRequest<TResponse> { }

                                                                         public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                         public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                         public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                     }

                                                                     namespace GFramework.Cqrs
                                                                     {
                                                                         public interface ICqrsHandlerRegistry
                                                                         {
                                                                             void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                         }

                                                                         [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                         public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                         {
                                                                             public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                         }

                                                                         [AttributeUsage(AttributeTargets.Assembly)]
                                                                         public sealed class CqrsReflectionFallbackAttribute : Attribute
                                                                         {
                                                                             public CqrsReflectionFallbackAttribute(params string[] fallbackHandlerTypeNames) { }

                                                                             public CqrsReflectionFallbackAttribute(params Type[] fallbackHandlerTypes) { }
                                                                         }
                                                                     }

                                                                     namespace TestApp
                                                                     {
                                                                         using GFramework.Cqrs.Abstractions.Cqrs;

                                                                         public sealed class Container
                                                                         {
                                                                             private unsafe struct AlphaResponse
                                                                             {
                                                                             }

                                                                             private unsafe struct BetaResponse
                                                                             {
                                                                             }

                                                                             private unsafe sealed record AlphaRequest() : IRequest<delegate* unmanaged<AlphaResponse>>;

                                                                             private unsafe sealed record BetaRequest() : IRequest<delegate* unmanaged<BetaResponse>>;

                                                                             public unsafe sealed class BetaHandler : IRequestHandler<BetaRequest, delegate* unmanaged<BetaResponse>>
                                                                             {
                                                                             }

                                                                             public unsafe sealed class AlphaHandler : IRequestHandler<AlphaRequest, delegate* unmanaged<AlphaResponse>>
                                                                             {
                                                                             }
                                                                         }
                                                                     }
                                                                     """;

    private const string AssemblyLevelMixedFallbackMetadataSource = """
                                                                    using System;

                                                                    namespace Microsoft.Extensions.DependencyInjection
                                                                    {
                                                                        public interface IServiceCollection { }

                                                                        public static class ServiceCollectionServiceExtensions
                                                                        {
                                                                            public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                        }
                                                                    }

                                                                    namespace GFramework.Core.Abstractions.Logging
                                                                    {
                                                                        public interface ILogger
                                                                        {
                                                                            void Debug(string msg);
                                                                        }
                                                                    }

                                                                    namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                    {
                                                                        public interface IRequest<TResponse> { }
                                                                        public interface INotification { }
                                                                        public interface IStreamRequest<TResponse> { }

                                                                        public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                        public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                        public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                    }

                                                                    namespace GFramework.Cqrs
                                                                    {
                                                                        public interface ICqrsHandlerRegistry
                                                                        {
                                                                            void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                        }

                                                                        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                        public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                        {
                                                                            public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                        }

                                                                        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                        public sealed class CqrsReflectionFallbackAttribute : Attribute
                                                                        {
                                                                            public CqrsReflectionFallbackAttribute(params string[] fallbackHandlerTypeNames) { }

                                                                            public CqrsReflectionFallbackAttribute(params Type[] fallbackHandlerTypes) { }
                                                                        }
                                                                    }

                                                                    namespace TestApp
                                                                    {
                                                                        using GFramework.Cqrs.Abstractions.Cqrs;

                                                                        public sealed class Container
                                                                        {
                                                                            private unsafe struct AlphaResponse
                                                                            {
                                                                            }

                                                                            private unsafe struct BetaResponse
                                                                            {
                                                                            }

                                                                            private unsafe sealed record AlphaRequest() : IRequest<delegate* unmanaged<AlphaResponse>>;

                                                                            private unsafe sealed record BetaRequest() : IRequest<delegate* unmanaged<BetaResponse>>;

                                                                            public unsafe sealed class AlphaHandler : IRequestHandler<AlphaRequest, delegate* unmanaged<AlphaResponse>>
                                                                            {
                                                                            }

                                                                            private unsafe sealed class BetaHandler : IRequestHandler<BetaRequest, delegate* unmanaged<BetaResponse>>
                                                                            {
                                                                            }
                                                                        }
                                                                    }
                                                                    """;

    private const string RequestInvokerProviderSource = """
                                                        using System;
                                                        using System.Collections.Generic;
                                                        using System.Reflection;
                                                        using System.Threading;
                                                        using System.Threading.Tasks;

                                                        namespace Microsoft.Extensions.DependencyInjection
                                                        {
                                                            public interface IServiceCollection { }

                                                            public static class ServiceCollectionServiceExtensions
                                                            {
                                                                public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                            }
                                                        }

                                                        namespace GFramework.Core.Abstractions.Logging
                                                        {
                                                            public interface ILogger
                                                            {
                                                                void Debug(string msg);
                                                            }
                                                        }

                                                        namespace GFramework.Cqrs.Abstractions.Cqrs
                                                        {
                                                            public interface IRequest<TResponse> { }
                                                            public interface INotification { }
                                                            public interface IStreamRequest<TResponse> { }

                                                            public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
                                                            {
                                                                ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
                                                            }

                                                            public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                            public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                        }

                                                        namespace GFramework.Cqrs
                                                        {
                                                            public interface ICqrsHandlerRegistry
                                                            {
                                                                void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                            }

                                                            public interface ICqrsRequestInvokerProvider
                                                            {
                                                                bool TryGetDescriptor(Type requestType, Type responseType, out CqrsRequestInvokerDescriptor? descriptor);
                                                            }

                                                            public interface IEnumeratesCqrsRequestInvokerDescriptors
                                                            {
                                                                IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors();
                                                            }

                                                            public sealed class CqrsRequestInvokerDescriptor
                                                            {
                                                                public CqrsRequestInvokerDescriptor(Type handlerType, MethodInfo invokerMethod) { }
                                                            }

                                                            public sealed class CqrsRequestInvokerDescriptorEntry
                                                            {
                                                                public CqrsRequestInvokerDescriptorEntry(Type requestType, Type responseType, CqrsRequestInvokerDescriptor descriptor)
                                                                {
                                                                    RequestType = requestType;
                                                                    ResponseType = responseType;
                                                                    Descriptor = descriptor;
                                                                }

                                                                public Type RequestType { get; }

                                                                public Type ResponseType { get; }

                                                                public CqrsRequestInvokerDescriptor Descriptor { get; }
                                                            }

                                                            [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                            public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                            {
                                                                public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                            }
                                                        }

                                                        namespace TestApp
                                                        {
                                                            using GFramework.Cqrs.Abstractions.Cqrs;

                                                            public sealed record VisibleRequest(string Value) : IRequest<string>;

                                                            public sealed class VisibleHandler : IRequestHandler<VisibleRequest, string>
                                                            {
                                                                public ValueTask<string> Handle(VisibleRequest request, CancellationToken cancellationToken)
                                                                {
                                                                    return ValueTask.FromResult(request.Value);
                                                                }
                                                            }
                                                        }
                                                        """;

    private const string StreamInvokerProviderSource = """
                                                       using System;
                                                       using System.Collections.Generic;
                                                       using System.Reflection;
                                                       using System.Threading;
                                                       using System.Threading.Tasks;

                                                       namespace Microsoft.Extensions.DependencyInjection
                                                       {
                                                           public interface IServiceCollection { }

                                                           public static class ServiceCollectionServiceExtensions
                                                           {
                                                               public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                           }
                                                       }

                                                       namespace GFramework.Core.Abstractions.Logging
                                                       {
                                                           public interface ILogger
                                                           {
                                                               void Debug(string msg);
                                                           }
                                                       }

                                                       namespace GFramework.Cqrs.Abstractions.Cqrs
                                                       {
                                                           public interface IRequest<TResponse> { }
                                                           public interface INotification { }
                                                           public interface IStreamRequest<TResponse> { }

                                                           public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
                                                           {
                                                               ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
                                                           }

                                                           public interface INotificationHandler<in TNotification> where TNotification : INotification { }

                                                           public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse>
                                                           {
                                                               IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
                                                           }
                                                       }

                                                       namespace GFramework.Cqrs
                                                       {
                                                           public interface ICqrsHandlerRegistry
                                                           {
                                                               void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                           }

                                                           public interface ICqrsStreamInvokerProvider
                                                           {
                                                               bool TryGetDescriptor(Type requestType, Type responseType, out CqrsStreamInvokerDescriptor? descriptor);
                                                           }

                                                           public interface IEnumeratesCqrsStreamInvokerDescriptors
                                                           {
                                                               IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors();
                                                           }

                                                           public sealed class CqrsStreamInvokerDescriptor
                                                           {
                                                               public CqrsStreamInvokerDescriptor(Type handlerType, MethodInfo invokerMethod) { }
                                                           }

                                                           public sealed class CqrsStreamInvokerDescriptorEntry
                                                           {
                                                               public CqrsStreamInvokerDescriptorEntry(Type requestType, Type responseType, CqrsStreamInvokerDescriptor descriptor)
                                                               {
                                                                   RequestType = requestType;
                                                                   ResponseType = responseType;
                                                                   Descriptor = descriptor;
                                                               }

                                                               public Type RequestType { get; }

                                                               public Type ResponseType { get; }

                                                               public CqrsStreamInvokerDescriptor Descriptor { get; }
                                                           }

                                                           [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                           public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                           {
                                                               public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                           }
                                                       }

                                                       namespace TestApp
                                                       {
                                                           using GFramework.Cqrs.Abstractions.Cqrs;

                                                           public sealed record VisibleStream(int Count) : IStreamRequest<int>;

                                                           public sealed class VisibleStreamHandler : IStreamRequestHandler<VisibleStream, int>
                                                           {
                                                               public async IAsyncEnumerable<int> Handle(VisibleStream request, CancellationToken cancellationToken)
                                                               {
                                                                   yield return request.Count;
                                                                   await Task.CompletedTask;
                                                               }
                                                           }
                                                       }
                                                       """;

    private const string HiddenImplementationRequestInvokerProviderSource = """
                                                                           using System;
                                                                           using System.Collections.Generic;
                                                                           using System.Reflection;
                                                                           using System.Threading;
                                                                           using System.Threading.Tasks;

                                                                           namespace Microsoft.Extensions.DependencyInjection
                                                                           {
                                                                               public interface IServiceCollection { }

                                                                               public static class ServiceCollectionServiceExtensions
                                                                               {
                                                                                   public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                               }
                                                                           }

                                                                           namespace GFramework.Core.Abstractions.Logging
                                                                           {
                                                                               public interface ILogger
                                                                               {
                                                                                   void Debug(string msg);
                                                                               }
                                                                           }

                                                                           namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                           {
                                                                               public interface IRequest<TResponse> { }
                                                                               public interface INotification { }
                                                                               public interface IStreamRequest<TResponse> { }

                                                                               public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
                                                                               {
                                                                                   ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
                                                                               }

                                                                               public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                               public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                           }

                                                                           namespace GFramework.Cqrs
                                                                           {
                                                                               public interface ICqrsHandlerRegistry
                                                                               {
                                                                                   void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                               }

                                                                               public interface ICqrsRequestInvokerProvider
                                                                               {
                                                                                   bool TryGetDescriptor(Type requestType, Type responseType, out CqrsRequestInvokerDescriptor? descriptor);
                                                                               }

                                                                               public interface IEnumeratesCqrsRequestInvokerDescriptors
                                                                               {
                                                                                   IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors();
                                                                               }

                                                                               public sealed class CqrsRequestInvokerDescriptor
                                                                               {
                                                                                   public CqrsRequestInvokerDescriptor(Type handlerType, MethodInfo invokerMethod) { }
                                                                               }

                                                                               public sealed class CqrsRequestInvokerDescriptorEntry
                                                                               {
                                                                                   public CqrsRequestInvokerDescriptorEntry(Type requestType, Type responseType, CqrsRequestInvokerDescriptor descriptor)
                                                                                   {
                                                                                       RequestType = requestType;
                                                                                       ResponseType = responseType;
                                                                                       Descriptor = descriptor;
                                                                                   }

                                                                                   public Type RequestType { get; }

                                                                                   public Type ResponseType { get; }

                                                                                   public CqrsRequestInvokerDescriptor Descriptor { get; }
                                                                               }

                                                                               [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                               public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                               {
                                                                                   public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                               }
                                                                           }

                                                                           namespace TestApp
                                                                           {
                                                                               using GFramework.Cqrs.Abstractions.Cqrs;

                                                                               public sealed record VisibleRequest() : IRequest<string>;

                                                                               public sealed class Container
                                                                               {
                                                                                   private sealed class HiddenHandler : IRequestHandler<VisibleRequest, string>
                                                                                   {
                                                                                       public ValueTask<string> Handle(VisibleRequest request, CancellationToken cancellationToken)
                                                                                       {
                                                                                           return ValueTask.FromResult(string.Empty);
                                                                                       }
                                                                                   }
                                                                               }
                                                                           }
                                                                           """;

    private const string HiddenImplementationStreamInvokerProviderSource = """
                                                                          using System;
                                                                          using System.Collections.Generic;
                                                                          using System.Reflection;
                                                                          using System.Threading;
                                                                          using System.Threading.Tasks;

                                                                          namespace Microsoft.Extensions.DependencyInjection
                                                                          {
                                                                              public interface IServiceCollection { }

                                                                              public static class ServiceCollectionServiceExtensions
                                                                              {
                                                                                  public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                              }
                                                                          }

                                                                          namespace GFramework.Core.Abstractions.Logging
                                                                          {
                                                                              public interface ILogger
                                                                              {
                                                                                  void Debug(string msg);
                                                                              }
                                                                          }

                                                                          namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                          {
                                                                              public interface IRequest<TResponse> { }
                                                                              public interface INotification { }
                                                                              public interface IStreamRequest<TResponse> { }

                                                                              public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                              public interface INotificationHandler<in TNotification> where TNotification : INotification { }

                                                                              public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse>
                                                                              {
                                                                                  IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
                                                                              }
                                                                          }

                                                                          namespace GFramework.Cqrs
                                                                          {
                                                                              public interface ICqrsHandlerRegistry
                                                                              {
                                                                                  void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                              }

                                                                              public interface ICqrsStreamInvokerProvider
                                                                              {
                                                                                  bool TryGetDescriptor(Type requestType, Type responseType, out CqrsStreamInvokerDescriptor? descriptor);
                                                                              }

                                                                              public interface IEnumeratesCqrsStreamInvokerDescriptors
                                                                              {
                                                                                  IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors();
                                                                              }

                                                                              public sealed class CqrsStreamInvokerDescriptor
                                                                              {
                                                                                  public CqrsStreamInvokerDescriptor(Type handlerType, MethodInfo invokerMethod) { }
                                                                              }

                                                                              public sealed class CqrsStreamInvokerDescriptorEntry
                                                                              {
                                                                                  public CqrsStreamInvokerDescriptorEntry(Type requestType, Type responseType, CqrsStreamInvokerDescriptor descriptor)
                                                                                  {
                                                                                      RequestType = requestType;
                                                                                      ResponseType = responseType;
                                                                                      Descriptor = descriptor;
                                                                                  }

                                                                                  public Type RequestType { get; }

                                                                                  public Type ResponseType { get; }

                                                                                  public CqrsStreamInvokerDescriptor Descriptor { get; }
                                                                              }

                                                                              [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                              public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                              {
                                                                                  public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                              }
                                                                          }

                                                                          namespace TestApp
                                                                          {
                                                                              using GFramework.Cqrs.Abstractions.Cqrs;

                                                                              public sealed record VisibleStream() : IStreamRequest<int>;

                                                                              public sealed class Container
                                                                              {
                                                                                  private sealed class HiddenHandler : IStreamRequestHandler<VisibleStream, int>
                                                                                  {
                                                                                      public async IAsyncEnumerable<int> Handle(VisibleStream request, CancellationToken cancellationToken)
                                                                                      {
                                                                                          yield return 1;
                                                                                          await Task.CompletedTask;
                                                                                      }
                                                                                  }
                                                                              }
                                                                          }
                                                                          """;

    private const string PreciseReflectedRequestInvokerProviderBoundarySource = """
                                                                                using System;
                                                                                using System.Collections.Generic;
                                                                                using System.Reflection;
                                                                                using System.Threading;
                                                                                using System.Threading.Tasks;

                                                                                namespace Microsoft.Extensions.DependencyInjection
                                                                                {
                                                                                    public interface IServiceCollection { }

                                                                                    public static class ServiceCollectionServiceExtensions
                                                                                    {
                                                                                        public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                                    }
                                                                                }

                                                                                namespace GFramework.Core.Abstractions.Logging
                                                                                {
                                                                                    public interface ILogger
                                                                                    {
                                                                                        void Debug(string msg);
                                                                                    }
                                                                                }

                                                                                namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                                {
                                                                                    public interface IRequest<TResponse> { }
                                                                                    public interface INotification { }
                                                                                    public interface IStreamRequest<TResponse> { }

                                                                                    public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
                                                                                    {
                                                                                        ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
                                                                                    }

                                                                                    public interface INotificationHandler<in TNotification> where TNotification : INotification { }
                                                                                    public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse> { }
                                                                                }

                                                                                namespace GFramework.Cqrs
                                                                                {
                                                                                    public interface ICqrsHandlerRegistry
                                                                                    {
                                                                                        void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                                    }

                                                                                    public interface ICqrsRequestInvokerProvider
                                                                                    {
                                                                                        bool TryGetDescriptor(Type requestType, Type responseType, out CqrsRequestInvokerDescriptor? descriptor);
                                                                                    }

                                                                                    public interface IEnumeratesCqrsRequestInvokerDescriptors
                                                                                    {
                                                                                        IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors();
                                                                                    }

                                                                                    public sealed class CqrsRequestInvokerDescriptor
                                                                                    {
                                                                                        public CqrsRequestInvokerDescriptor(Type handlerType, MethodInfo invokerMethod) { }
                                                                                    }

                                                                                    public sealed class CqrsRequestInvokerDescriptorEntry
                                                                                    {
                                                                                        public CqrsRequestInvokerDescriptorEntry(Type requestType, Type responseType, CqrsRequestInvokerDescriptor descriptor)
                                                                                        {
                                                                                            RequestType = requestType;
                                                                                            ResponseType = responseType;
                                                                                            Descriptor = descriptor;
                                                                                        }

                                                                                        public Type RequestType { get; }

                                                                                        public Type ResponseType { get; }

                                                                                        public CqrsRequestInvokerDescriptor Descriptor { get; }
                                                                                    }

                                                                                    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                                    public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                                    {
                                                                                        public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                                    }
                                                                                }

                                                                                namespace TestApp
                                                                                {
                                                                                    using GFramework.Cqrs.Abstractions.Cqrs;

                                                                                    public sealed class Container
                                                                                    {
                                                                                        private sealed record HiddenResponse();

                                                                                        private sealed record HiddenRequest() : IRequest<HiddenResponse[]>;

                                                                                        private sealed class HiddenHandler : IRequestHandler<HiddenRequest, HiddenResponse[]>
                                                                                        {
                                                                                            public ValueTask<HiddenResponse[]> Handle(HiddenRequest request, CancellationToken cancellationToken)
                                                                                            {
                                                                                                return ValueTask.FromResult(Array.Empty<HiddenResponse>());
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                                """;

    private const string PreciseReflectedStreamInvokerProviderBoundarySource = """
                                                                               using System;
                                                                               using System.Collections.Generic;
                                                                               using System.Reflection;
                                                                               using System.Threading;
                                                                               using System.Threading.Tasks;

                                                                               namespace Microsoft.Extensions.DependencyInjection
                                                                               {
                                                                                   public interface IServiceCollection { }

                                                                                   public static class ServiceCollectionServiceExtensions
                                                                                   {
                                                                                       public static void AddTransient(IServiceCollection services, Type serviceType, Type implementationType) { }
                                                                                   }
                                                                               }

                                                                               namespace GFramework.Core.Abstractions.Logging
                                                                               {
                                                                                   public interface ILogger
                                                                                   {
                                                                                       void Debug(string msg);
                                                                                   }
                                                                               }

                                                                               namespace GFramework.Cqrs.Abstractions.Cqrs
                                                                               {
                                                                                   public interface IRequest<TResponse> { }
                                                                                   public interface INotification { }
                                                                                   public interface IStreamRequest<TResponse> { }

                                                                                   public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> { }
                                                                                   public interface INotificationHandler<in TNotification> where TNotification : INotification { }

                                                                                   public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse>
                                                                                   {
                                                                                       IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
                                                                                   }
                                                                               }

                                                                               namespace GFramework.Cqrs
                                                                               {
                                                                                   public interface ICqrsHandlerRegistry
                                                                                   {
                                                                                       void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services, GFramework.Core.Abstractions.Logging.ILogger logger);
                                                                                   }

                                                                                   public interface ICqrsStreamInvokerProvider
                                                                                   {
                                                                                       bool TryGetDescriptor(Type requestType, Type responseType, out CqrsStreamInvokerDescriptor? descriptor);
                                                                                   }

                                                                                   public interface IEnumeratesCqrsStreamInvokerDescriptors
                                                                                   {
                                                                                       IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors();
                                                                                   }

                                                                                   public sealed class CqrsStreamInvokerDescriptor
                                                                                   {
                                                                                       public CqrsStreamInvokerDescriptor(Type handlerType, MethodInfo invokerMethod) { }
                                                                                   }

                                                                                   public sealed class CqrsStreamInvokerDescriptorEntry
                                                                                   {
                                                                                       public CqrsStreamInvokerDescriptorEntry(Type requestType, Type responseType, CqrsStreamInvokerDescriptor descriptor)
                                                                                       {
                                                                                           RequestType = requestType;
                                                                                           ResponseType = responseType;
                                                                                           Descriptor = descriptor;
                                                                                       }

                                                                                       public Type RequestType { get; }

                                                                                       public Type ResponseType { get; }

                                                                                       public CqrsStreamInvokerDescriptor Descriptor { get; }
                                                                                   }

                                                                                   [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                                                                                   public sealed class CqrsHandlerRegistryAttribute : Attribute
                                                                                   {
                                                                                       public CqrsHandlerRegistryAttribute(Type registryType) { }
                                                                                   }
                                                                               }

                                                                               namespace TestApp
                                                                               {
                                                                                   using GFramework.Cqrs.Abstractions.Cqrs;

                                                                                   public sealed class Container
                                                                                   {
                                                                                       private sealed record HiddenResponse();

                                                                                       private sealed record HiddenStream() : IStreamRequest<HiddenResponse[]>;

                                                                                       private sealed class HiddenHandler : IStreamRequestHandler<HiddenStream, HiddenResponse[]>
                                                                                       {
                                                                                           public async IAsyncEnumerable<HiddenResponse[]> Handle(HiddenStream request, CancellationToken cancellationToken)
                                                                                           {
                                                                                               yield return Array.Empty<HiddenResponse>();
                                                                                               await Task.CompletedTask;
                                                                                           }
                                                                                       }
                                                                                   }
                                                                               }
                                                                               """;

    /// <summary>
    ///     验证生成器会为当前程序集中的 request、notification 和 stream 处理器生成稳定顺序的注册器。
    /// </summary>
    [Test]
    public async Task Generates_Assembly_Level_Cqrs_Handler_Registry()
    {
        await GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(
            AssemblyLevelCqrsHandlerRegistrySource,
            ("CqrsHandlerRegistry.g.cs", AssemblyLevelCqrsHandlerRegistryExpected));
    }

    /// <summary>
    ///     验证当程序集包含生成代码无法合法引用的私有嵌套处理器时，生成器会在生成注册器内部执行定向反射注册，
    ///     不再依赖程序集级 fallback marker。
    /// </summary>
    [Test]
    public async Task
        Generates_Visible_Handlers_And_Self_Registers_Private_Nested_Handler_When_Assembly_Contains_Hidden_Handler()
    {
        await GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(
            HiddenNestedHandlerSelfRegistrationSource,
            ("CqrsHandlerRegistry.g.cs", HiddenNestedHandlerSelfRegistrationExpected));
    }

    /// <summary>
    ///     验证当隐藏实现类型的 handler 接口仍可被生成代码直接引用时，
    ///     生成器只会定向反射实现类型，而不会再生成基于 <c>GetInterfaces()</c> 的接口发现辅助逻辑。
    /// </summary>
    [Test]
    public async Task
        Generates_Direct_Interface_Registrations_For_Hidden_Implementation_When_Handler_Interface_Is_Public()
    {
        await GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(
            HiddenImplementationDirectInterfaceRegistrationSource,
            ("CqrsHandlerRegistry.g.cs", HiddenImplementationDirectInterfaceRegistrationExpected));
    }

    /// <summary>
    ///     验证精确重建路径会递归覆盖隐藏元素类型数组，
    ///     使这类 handler interface 也能直接生成 closed service type，而不再退回 <c>GetInterfaces()</c>。
    /// </summary>
    [Test]
    public async Task Generates_Precise_Service_Type_For_Hidden_Array_Type_Arguments()
    {
        await GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(
            HiddenArrayResponseFallbackSource,
            ("CqrsHandlerRegistry.g.cs", HiddenArrayResponseFallbackExpected));
    }

    /// <summary>
    ///     验证精确重建路径会递归覆盖隐藏泛型定义，
    ///     使“隐藏泛型定义 + 可见/常量型实参”的闭包类型也能直接生成 closed service type。
    /// </summary>
    [Test]
    public async Task Generates_Precise_Service_Type_For_Hidden_Generic_Type_Definitions()
    {
        await GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(
            HiddenGenericEnvelopeResponseSource,
            ("CqrsHandlerRegistry.g.cs", HiddenGenericEnvelopeResponseExpected));
    }

    /// <summary>
    ///     验证精确重建路径会保留隐藏元素类型的多维数组秩信息，
    ///     使生成注册器继续走定向运行时类型重建，而不是退回宽松接口发现。
    /// </summary>
    [Test]
    public void Generates_Precise_Service_Type_For_Hidden_MultiDimensional_Array_Type_Arguments()
    {
        var generatedSource = RunGenerator(HiddenMultiDimensionalArrayResponseSource);

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain("registryAssembly.GetType(\"TestApp.Container+HiddenResponse\", throwOnError: false, ignoreCase: false);"));
            Assert.That(
                generatedSource,
                Does.Contain("typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType("));
            Assert.That(generatedSource, Does.Contain(".MakeArrayType(2)"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsReflectionFallbackAttribute("));
        });
    }

    /// <summary>
    ///     验证精确重建路径会递归覆盖交错数组，
    ///     确保隐藏元素类型的每一层数组都继续通过数组发射分支稳定重建。
    /// </summary>
    [Test]
    public void Generates_Precise_Service_Type_For_Hidden_Jagged_Array_Type_Arguments()
    {
        var generatedSource = RunGenerator(HiddenJaggedArrayResponseSource);

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain("registryAssembly.GetType(\"TestApp.Container+HiddenResponse\", throwOnError: false, ignoreCase: false);"));
            Assert.That(
                generatedSource,
                Does.Contain("typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType("));
            Assert.That(generatedSource, Does.Contain(".MakeArrayType().MakeArrayType()"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsReflectionFallbackAttribute("));
        });
    }

    /// <summary>
    ///     验证当 handler 合同把 pointer 响应类型放进 CQRS 泛型参数时，
    ///     生成器会保守回退而不是继续发射不可构造的精确注册代码。
    /// </summary>
    [Test]
    public void Reports_Compilation_Error_And_Skips_Precise_Registration_For_Hidden_Pointer_Response()
    {
        var execution = ExecuteGenerator(
            HiddenPointerResponseCompilationErrorSource,
            allowUnsafe: true);
        var inputCompilationErrors = execution.InputCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatedCompilationErrors = execution.GeneratedCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatorErrors = execution.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var missingContractDiagnostic =
            generatorErrors.SingleOrDefault(static diagnostic =>
                string.Equals(diagnostic.Id, "GF_Cqrs_001", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(inputCompilationErrors.Select(static diagnostic => diagnostic.Id), Does.Contain("CS0306"));
            Assert.That(generatedCompilationErrors, Is.Empty);
            Assert.That(execution.GeneratedSources, Is.Empty);
            Assert.That(missingContractDiagnostic, Is.Not.Null);
            Assert.That(
                missingContractDiagnostic!.GetMessage(),
                Does.Contain("TestApp.Container+HiddenHandler"));
            Assert.That(
                missingContractDiagnostic.GetMessage(),
                Does.Contain("GFramework.Cqrs.CqrsReflectionFallbackAttribute"));
        });
    }

    /// <summary>
    ///     验证同一个 implementation 同时包含可直接注册接口与需精确重建接口时，
    ///     生成器会保留两类注册，并继续按 handler interface 名称稳定排序。
    /// </summary>
    [Test]
    public async Task Generates_Mixed_Direct_And_Precise_Registrations_For_Same_Implementation()
    {
        await GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(
            MixedDirectAndPreciseRegistrationsSource,
            ("CqrsHandlerRegistry.g.cs", MixedDirectAndPreciseRegistrationsExpected));
    }

    /// <summary>
    ///     验证隐藏 implementation 同时包含可见 handler interface 与需精确重建接口时，
    ///     生成器会保留两类注册，而不会让可见接口被整实现回退吞掉。
    /// </summary>
    [Test]
    public async Task Generates_Mixed_Reflected_Implementation_And_Precise_Registrations_For_Same_Implementation()
    {
        await GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(
            MixedReflectedImplementationAndPreciseRegistrationsSource,
            ("CqrsHandlerRegistry.g.cs", MixedReflectedImplementationAndPreciseRegistrationsExpected));
    }

    /// <summary>
    ///     验证当外部基类暴露的 handler interface 含有生成注册器顶层上下文不可直接引用的 protected 类型时，
    ///     生成器会输出定向程序集查找，而不是继续退回 implementation 级接口发现。
    /// </summary>
    [Test]
    public void Generates_Precise_Assembly_Type_Lookups_For_Inaccessible_External_Protected_Types()
    {
        var contractsReference = MetadataReferenceTestBuilder.CreateFromSource(
            "Contracts",
            ExternalProtectedTypeContractsSource);
        var dependencyReference = MetadataReferenceTestBuilder.CreateFromSource(
            "Dependency",
            ExternalProtectedTypeDependencySource,
            contractsReference);
        var generatedSource = RunGenerator(
            ExternalProtectedTypeLookupSource,
            contractsReference,
            dependencyReference);

        Assert.That(
            generatedSource,
            Does.Not.Contain("RegisterRemainingReflectedHandlerInterfaces("));
        Assert.That(
            generatedSource,
            Does.Not.Contain("Remaining runtime interface discovery target:"));
        Assert.That(
            generatedSource,
            Is.EqualTo(ExternalAssemblyPreciseLookupExpected));
    }

    /// <summary>
    ///     验证当外部程序集隐藏元素类型以多维数组形式参与 CQRS 合同时，
    ///     生成器仍会保留外部程序集定向查找与数组秩信息，而不是退回 fallback 元数据。
    /// </summary>
    [Test]
    public void Generates_Precise_Assembly_Type_Lookups_For_Inaccessible_External_MultiDimensional_Array_Elements()
    {
        var contractsReference = MetadataReferenceTestBuilder.CreateFromSource(
            "Contracts",
            ExternalProtectedTypeContractsSource);
        var dependencyReference = MetadataReferenceTestBuilder.CreateFromSource(
            "Dependency",
            ExternalProtectedMultiDimensionalTypeDependencySource,
            contractsReference);
        var generatedSource = RunGenerator(
            ExternalProtectedTypeLookupSource,
            contractsReference,
            dependencyReference);

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain(
                    "ResolveReferencedAssemblyType(\"Dependency, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\", \"Dep.VisibilityScope+ProtectedResponse\")"));
            Assert.That(
                generatedSource,
                Does.Contain("typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType("));
            Assert.That(generatedSource, Does.Contain(".MakeArrayType(2)"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsReflectionFallbackAttribute("));
        });
    }

    /// <summary>
    ///     验证当外部程序集隐藏泛型定义以“隐藏定义 + 可见类型实参”的形式参与 CQRS 合同时，
    ///     生成器会继续输出定向程序集查找与运行时泛型重建，而不是退回字符串 fallback 元数据。
    /// </summary>
    [Test]
    public void Generates_Precise_Assembly_Type_Lookups_For_Inaccessible_External_Generic_Definitions_With_Visible_Type_Arguments()
    {
        var contractsReference = MetadataReferenceTestBuilder.CreateFromSource(
            "Contracts",
            ExternalProtectedTypeContractsSource);
        var dependencyReference = MetadataReferenceTestBuilder.CreateFromSource(
            "Dependency",
            ExternalProtectedGenericDefinitionDependencySource,
            contractsReference);
        var generatedSource = RunGenerator(
            ExternalProtectedTypeLookupSource,
            contractsReference,
            dependencyReference);

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain(
                    "ResolveReferencedAssemblyType(\"Dependency, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\", \"Dep.VisibilityScope+ProtectedRequest\")"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "ResolveReferencedAssemblyType(\"Dependency, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\", \"Dep.VisibilityScope+ProtectedEnvelope`1\")"));
            Assert.That(
                generatedSource,
                Does.Contain("typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType("));
            Assert.That(generatedSource, Does.Contain(".MakeGenericType(typeof(string))"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsReflectionFallbackAttribute("));
        });
    }

    /// <summary>
    ///     验证即使 runtime 仍暴露旧版无参 fallback marker，生成器也会优先在生成注册器内部处理隐藏 handler，
    ///     不再输出 fallback marker。
    /// </summary>
    [Test]
    public async Task Does_Not_Emit_Legacy_Fallback_Marker_When_Generated_Registry_Can_Self_Register_Hidden_Handler()
    {
        await GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(
            LegacyFallbackMarkerHiddenHandlerSource,
            ("CqrsHandlerRegistry.g.cs", HiddenNestedHandlerSelfRegistrationExpected));
    }

    /// <summary>
    ///     验证即使 runtime 合同中完全不存在 reflection fallback 标记特性，
    ///     生成器仍能通过生成注册器内部的定向反射逻辑覆盖隐藏 handler。
    /// </summary>
    [Test]
    public async Task Generates_Registry_For_Hidden_Handler_When_Fallback_Marker_Is_Unavailable()
    {
        await GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(
            FallbackMarkerUnavailableHiddenHandlerSource,
            ("CqrsHandlerRegistry.g.cs", HiddenNestedHandlerSelfRegistrationExpected));
    }

    /// <summary>
    ///     验证当某轮生成仍然需要程序集级 reflection fallback 元数据，且 runtime 合同缺少承载该元数据的特性时，
    ///     生成器会给出明确诊断并停止输出注册器。
    /// </summary>
    [Test]
    public void
        Reports_Diagnostic_And_Skips_Registry_When_Fallback_Metadata_Is_Required_But_Runtime_Contract_Lacks_Fallback_Attribute()
    {
        var execution = ExecuteGenerator(
            MissingFallbackAttributeDiagnosticSource,
            allowUnsafe: true);
        var inputCompilationErrors = execution.InputCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatedCompilationErrors = execution.GeneratedCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatorErrors = execution.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var missingContractDiagnostic =
            generatorErrors.SingleOrDefault(static diagnostic =>
                string.Equals(diagnostic.Id, "GF_Cqrs_001", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(inputCompilationErrors.Select(static diagnostic => diagnostic.Id), Does.Contain("CS0306"));
            Assert.That(generatedCompilationErrors, Is.Empty);
            Assert.That(execution.GeneratedSources, Is.Empty);
            Assert.That(missingContractDiagnostic, Is.Not.Null);
            Assert.That(
                missingContractDiagnostic!.GetMessage(),
                Does.Contain("TestApp.Container+HiddenHandler"));
            Assert.That(
                missingContractDiagnostic.GetMessage(),
                Does.Contain("GFramework.Cqrs.CqrsReflectionFallbackAttribute"));
        });
    }

    /// <summary>
    ///     验证 handler 合同里出现未解析错误类型时，生成器会改为运行时精确查找该类型，
    ///     而不会把无效类型名直接写进生成代码中的 <c>typeof(...)</c>。
    /// </summary>
    [Test]
    public void Emits_Runtime_Type_Lookup_When_Handler_Contract_Contains_Unresolved_Error_Types()
    {
        var execution = ExecuteGenerator(UnresolvedErrorTypeRuntimeLookupSource);
        var inputCompilationErrors = execution.InputCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatedCompilationErrors = execution.GeneratedCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatorErrors = execution.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(inputCompilationErrors.Select(static diagnostic => diagnostic.Id), Does.Contain("CS0246"));
            Assert.That(generatedCompilationErrors, Is.Empty);
            Assert.That(generatorErrors, Is.Empty);
            Assert.That(execution.GeneratedSources, Has.Length.EqualTo(1));
            Assert.That(execution.GeneratedSources[0].filename, Is.EqualTo("CqrsHandlerRegistry.g.cs"));
            var generatedSource = execution.GeneratedSources[0].content;
            Assert.That(
                generatedSource,
                Does.Contain("registryAssembly.GetType(\"MissingResponse\", throwOnError: false, ignoreCase: false);"));
            Assert.That(
                generatedSource,
                Does.Contain("internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry"));
            Assert.That(generatedSource, Does.Not.Contain("typeof(MissingResponse)"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsReflectionFallbackAttribute("));
        });
    }

    /// <summary>
    ///     验证 <see langword="dynamic" /> 响应类型会在生成阶段归一化为 <see cref="System.Object" />，
    ///     避免注册器发射非法的 <c>typeof(dynamic)</c>。
    /// </summary>
    [Test]
    public void Emits_Object_Type_Reference_When_Handler_Response_Uses_Dynamic()
    {
        var execution = ExecuteGenerator(DynamicResponseNormalizationSource);
        var inputCompilationErrors = execution.InputCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatedCompilationErrors = execution.GeneratedCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatorErrors = execution.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(inputCompilationErrors.Select(static diagnostic => diagnostic.Id), Does.Contain("CS1966"));
            Assert.That(generatedCompilationErrors, Is.Empty);
            Assert.That(generatorErrors, Is.Empty);
            Assert.That(execution.GeneratedSources, Has.Length.EqualTo(1));
            Assert.That(execution.GeneratedSources[0].filename, Is.EqualTo("CqrsHandlerRegistry.g.cs"));
            var generatedSource = execution.GeneratedSources[0].content;
            Assert.That(generatedSource, Does.Contain("typeof(global::System.Object)"));
            Assert.That(generatedSource, Does.Not.Contain("typeof(dynamic)"));
            Assert.That(generatedSource, Does.Contain("internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry"));
        });
    }

    /// <summary>
    ///     验证当 fallback metadata 仍然必需且 runtime 提供了承载契约时，
    ///     生成器会继续产出注册器并发射程序集级 <c>CqrsReflectionFallbackAttribute</c>。
    /// </summary>
    [Test]
    public void
        Emits_Assembly_Level_Fallback_Metadata_When_Fallback_Is_Required_And_Runtime_Contract_Is_Available()
    {
        var execution = ExecuteGenerator(
            AssemblyLevelFallbackMetadataSource,
            allowUnsafe: true);
        var inputCompilationErrors = execution.InputCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatedCompilationErrors = execution.GeneratedCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatorErrors = execution.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(inputCompilationErrors.Select(static diagnostic => diagnostic.Id), Does.Contain("CS0306"));
            Assert.That(generatedCompilationErrors, Is.Empty);
            Assert.That(generatorErrors, Is.Empty);
            Assert.That(execution.GeneratedSources, Has.Length.EqualTo(1));
            Assert.That(execution.GeneratedSources[0].filename, Is.EqualTo("CqrsHandlerRegistry.g.cs"));
            Assert.That(
                execution.GeneratedSources[0].content,
                Does.Contain(
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute(\"TestApp.Container+AlphaHandler\", \"TestApp.Container+BetaHandler\")]"));
            Assert.That(
                execution.GeneratedSources[0].content,
                Does.Contain(
                    "[assembly: global::GFramework.Cqrs.CqrsHandlerRegistryAttribute(typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry))]"));
            Assert.That(
                execution.GeneratedSources[0].content,
                Does.Contain("internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry"));
        });
    }

    /// <summary>
    ///     验证当所有 fallback handlers 本身都可直接引用，且 runtime 同时支持字符串与 <see cref="Type" /> 元数据承载时，
    ///     生成器会优先发射直接 <c>typeof(...)</c> 的 fallback 特性，减少运行时按名称回查程序集类型。
    /// </summary>
    [Test]
    public void
        Emits_Direct_Type_Fallback_Metadata_When_All_Fallback_Handlers_Are_Referenceable_And_Runtime_Type_Contract_Is_Available()
    {
        var execution = ExecuteGenerator(
            AssemblyLevelDirectFallbackMetadataSource,
            allowUnsafe: true);
        var inputCompilationErrors = execution.InputCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatedCompilationErrors = execution.GeneratedCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatorErrors = execution.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(inputCompilationErrors.Select(static diagnostic => diagnostic.Id), Does.Contain("CS0306"));
            Assert.That(generatedCompilationErrors, Is.Empty);
            Assert.That(generatorErrors, Is.Empty);
            Assert.That(execution.GeneratedSources, Has.Length.EqualTo(1));
            Assert.That(execution.GeneratedSources[0].filename, Is.EqualTo("CqrsHandlerRegistry.g.cs"));
            var generatedSource = execution.GeneratedSources[0].content;
            Assert.That(
                generatedSource,
                Does.Contain(
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute(typeof(global::TestApp.Container.AlphaHandler), typeof(global::TestApp.Container.BetaHandler))]"));
            Assert.That(
                generatedSource,
                Does.Not.Contain(
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute(\"TestApp.Container+AlphaHandler\", \"TestApp.Container+BetaHandler\")]"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsReflectionFallbackAttribute()"));
            Assert.That(
                CountOccurrences(
                    generatedSource,
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute"),
                Is.EqualTo(1));
            Assert.That(
                CountOccurrences(
                    generatedSource,
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute(\""),
                Is.Zero);
        });
    }

    /// <summary>
    ///     验证当 runtime 允许多个 fallback 特性实例，且本轮 fallback 同时包含可直接引用与仅能按名称恢复的 handlers 时，
    ///     生成器会拆分出直接 <see cref="Type" /> 与字符串两类元数据，避免 mixed 场景整体退回字符串 fallback。
    /// </summary>
    [Test]
    public void
        Emits_Mixed_Direct_Type_And_String_Fallback_Metadata_When_Runtime_Allows_Multiple_Fallback_Attributes()
    {
        var execution = ExecuteGenerator(
            AssemblyLevelMixedFallbackMetadataSource,
            allowUnsafe: true);
        var inputCompilationErrors = execution.InputCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatedCompilationErrors = execution.GeneratedCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatorErrors = execution.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(inputCompilationErrors.Select(static diagnostic => diagnostic.Id), Does.Contain("CS0306"));
            Assert.That(generatedCompilationErrors, Is.Empty);
            Assert.That(generatorErrors, Is.Empty);
            Assert.That(execution.GeneratedSources, Has.Length.EqualTo(1));
            Assert.That(execution.GeneratedSources[0].filename, Is.EqualTo("CqrsHandlerRegistry.g.cs"));
            var generatedSource = execution.GeneratedSources[0].content;
            Assert.That(
                generatedSource,
                Does.Contain(
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute(typeof(global::TestApp.Container.AlphaHandler))]"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute(\"TestApp.Container+BetaHandler\")]"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsReflectionFallbackAttribute()"));
            Assert.That(
                CountOccurrences(
                    generatedSource,
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute"),
                Is.EqualTo(2));
            Assert.That(
                CountOccurrences(
                    generatedSource,
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute(\""),
                Is.EqualTo(1));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute(typeof(global::TestApp.Container.AlphaHandler))]" +
                    Environment.NewLine +
                    "[assembly: global::GFramework.Cqrs.CqrsReflectionFallbackAttribute(\"TestApp.Container+BetaHandler\")]" +
                    Environment.NewLine +
                    "[assembly: global::GFramework.Cqrs.CqrsHandlerRegistryAttribute(typeof(global::GFramework.Generated.Cqrs.__GFrameworkGeneratedCqrsHandlerRegistry))]"));
        });
    }

    /// <summary>
    ///     验证当 runtime 暴露 request invoker provider 契约时，生成器会让 generated registry 同时发射
    ///     request invoker 描述符与对应的开放静态 invoker 方法。
    /// </summary>
    [Test]
    public void Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available()
    {
        var execution = ExecuteGenerator(RequestInvokerProviderSource);
        var inputCompilationErrors = execution.InputCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatedCompilationErrors = execution.GeneratedCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatorErrors = execution.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(inputCompilationErrors, Is.Empty);
            Assert.That(generatedCompilationErrors, Is.Empty);
            Assert.That(generatorErrors, Is.Empty);
            Assert.That(execution.GeneratedSources, Has.Length.EqualTo(1));
            Assert.That(execution.GeneratedSources[0].filename, Is.EqualTo("CqrsHandlerRegistry.g.cs"));
            var generatedSource = execution.GeneratedSources[0].content;
            Assert.That(
                generatedSource,
                Does.Contain(
                    "internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry, global::GFramework.Cqrs.ICqrsRequestInvokerProvider, global::GFramework.Cqrs.IEnumeratesCqrsRequestInvokerDescriptors"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "new global::GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry(typeof(global::TestApp.VisibleRequest), typeof(string),"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "new global::GFramework.Cqrs.CqrsRequestInvokerDescriptor(typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<global::TestApp.VisibleRequest, string>), typeof(__GFrameworkGeneratedCqrsHandlerRegistry).GetMethod(nameof(InvokeRequestHandler0), global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static)!)"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "private static global::System.Threading.Tasks.ValueTask<string> InvokeRequestHandler0(object handler, object request, global::System.Threading.CancellationToken cancellationToken)"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "global::System.Collections.Generic.IReadOnlyList<global::GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry> global::GFramework.Cqrs.IEnumeratesCqrsRequestInvokerDescriptors.GetDescriptors()"));
        });
    }

    /// <summary>
    ///     验证当 handler 实现类型隐藏、但 request handler interface 仍可见时，
    ///     生成器仍会发射 request invoker provider 元数据，而不是因为实现类型不可直接引用而整体退回反射路径。
    /// </summary>
    [Test]
    public void Emits_Request_Invoker_Provider_Metadata_For_Hidden_Implementation_With_Visible_Handler_Interface()
    {
        var generatedSource = RunGenerator(HiddenImplementationRequestInvokerProviderSource);

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain(
                    "internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry, global::GFramework.Cqrs.ICqrsRequestInvokerProvider, global::GFramework.Cqrs.IEnumeratesCqrsRequestInvokerDescriptors"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "new global::GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry(typeof(global::TestApp.VisibleRequest), typeof(string),"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "new global::GFramework.Cqrs.CqrsRequestInvokerDescriptor(typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<global::TestApp.VisibleRequest, string>), typeof(__GFrameworkGeneratedCqrsHandlerRegistry).GetMethod(nameof(InvokeRequestHandler0), global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static)!)"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "var typedHandler = (global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<global::TestApp.VisibleRequest, string>)handler;"));
        });
    }

    /// <summary>
    ///     验证当 runtime 缺少 <c>ICqrsRequestInvokerProvider</c> 时，
    ///     生成器会整体跳过 request invoker provider 元数据发射，而不是输出半套 descriptor 成员。
    /// </summary>
    [Test]
    public void Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Provider_Interface()
    {
        var generatedSource = RunGenerator(RemoveBlock(
            RequestInvokerProviderSource,
            "public interface ICqrsRequestInvokerProvider",
            "public interface IEnumeratesCqrsRequestInvokerDescriptors"));

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain(
                    "internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry"));
            Assert.That(generatedSource, Does.Not.Contain("ICqrsRequestInvokerProvider"));
            Assert.That(generatedSource, Does.Not.Contain("IEnumeratesCqrsRequestInvokerDescriptors"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsRequestInvokerDescriptorEntry("));
            Assert.That(generatedSource, Does.Not.Contain("InvokeRequestHandler0"));
        });
    }

    /// <summary>
    ///     验证当 runtime 缺少 <c>IEnumeratesCqrsRequestInvokerDescriptors</c> 时，
    ///     生成器不会只发射 request provider 的部分成员，而是整体保持不生成 provider 元数据。
    /// </summary>
    [Test]
    public void Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Enumerator()
    {
        var generatedSource = RunGenerator(RemoveBlock(
            RequestInvokerProviderSource,
            "public interface IEnumeratesCqrsRequestInvokerDescriptors",
            "public sealed class CqrsRequestInvokerDescriptor"));

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain(
                    "internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry"));
            Assert.That(generatedSource, Does.Not.Contain("ICqrsRequestInvokerProvider"));
            Assert.That(generatedSource, Does.Not.Contain("IEnumeratesCqrsRequestInvokerDescriptors"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsRequestInvokerDescriptorEntry("));
            Assert.That(generatedSource, Does.Not.Contain("InvokeRequestHandler0"));
        });
    }

    /// <summary>
    ///     验证当 request handler 仍需走 precise reflected 注册时，
    ///     生成器即使检测到 request invoker provider runtime 合同，也不会错误发射无法稳定表达隐藏请求/响应类型的 provider 元数据。
    /// </summary>
    [Test]
    public void Does_Not_Emit_Request_Invoker_Provider_Metadata_For_Precise_Reflected_Request_Registrations()
    {
        var generatedSource = RunGenerator(PreciseReflectedRequestInvokerProviderBoundarySource);

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain("typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<,>).MakeGenericType("));
            Assert.That(generatedSource, Does.Contain(".MakeArrayType()"));
            Assert.That(generatedSource, Does.Not.Contain("ICqrsRequestInvokerProvider"));
            Assert.That(generatedSource, Does.Not.Contain("IEnumeratesCqrsRequestInvokerDescriptors"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsRequestInvokerDescriptorEntry("));
            Assert.That(generatedSource, Does.Not.Contain("InvokeRequestHandler0"));
        });
    }

    /// <summary>
    ///     验证当 runtime 暴露 stream invoker provider 契约时，生成器会让 generated registry 同时发射
    ///     stream invoker 描述符与对应的开放静态 invoker 方法。
    /// </summary>
    [Test]
    public void Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available()
    {
        var execution = ExecuteGenerator(StreamInvokerProviderSource);
        var inputCompilationErrors = execution.InputCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatedCompilationErrors = execution.GeneratedCompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatorErrors = execution.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(inputCompilationErrors, Is.Empty);
            Assert.That(generatedCompilationErrors, Is.Empty);
            Assert.That(generatorErrors, Is.Empty);
            Assert.That(execution.GeneratedSources, Has.Length.EqualTo(1));
            Assert.That(execution.GeneratedSources[0].filename, Is.EqualTo("CqrsHandlerRegistry.g.cs"));
            var generatedSource = execution.GeneratedSources[0].content;
            Assert.That(
                generatedSource,
                Does.Contain(
                    "internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry, global::GFramework.Cqrs.ICqrsStreamInvokerProvider, global::GFramework.Cqrs.IEnumeratesCqrsStreamInvokerDescriptors"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "new global::GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry(typeof(global::TestApp.VisibleStream), typeof(int),"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "new global::GFramework.Cqrs.CqrsStreamInvokerDescriptor(typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<global::TestApp.VisibleStream, int>), typeof(__GFrameworkGeneratedCqrsHandlerRegistry).GetMethod(nameof(InvokeStreamHandler0), global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static)!)"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "public bool TryGetDescriptor(global::System.Type requestType, global::System.Type responseType, out global::GFramework.Cqrs.CqrsStreamInvokerDescriptor? descriptor)"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "private static object InvokeStreamHandler0(object handler, object request, global::System.Threading.CancellationToken cancellationToken)"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "var typedHandler = (global::GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<global::TestApp.VisibleStream, int>)handler;"));
            Assert.That(
                generatedSource,
                Does.Contain("return typedHandler.Handle(typedRequest, cancellationToken);"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "global::System.Collections.Generic.IReadOnlyList<global::GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry> global::GFramework.Cqrs.IEnumeratesCqrsStreamInvokerDescriptors.GetDescriptors()"));
        });
    }

    /// <summary>
    ///     验证当 handler 实现类型隐藏、但 stream handler interface 仍可见时，
    ///     生成器仍会发射 stream invoker provider 元数据，而不是放弃生成稳定的 generated invoker 桥接。
    /// </summary>
    [Test]
    public void Emits_Stream_Invoker_Provider_Metadata_For_Hidden_Implementation_With_Visible_Handler_Interface()
    {
        var generatedSource = RunGenerator(HiddenImplementationStreamInvokerProviderSource);

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain(
                    "internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry, global::GFramework.Cqrs.ICqrsStreamInvokerProvider, global::GFramework.Cqrs.IEnumeratesCqrsStreamInvokerDescriptors"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "new global::GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry(typeof(global::TestApp.VisibleStream), typeof(int),"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "new global::GFramework.Cqrs.CqrsStreamInvokerDescriptor(typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<global::TestApp.VisibleStream, int>), typeof(__GFrameworkGeneratedCqrsHandlerRegistry).GetMethod(nameof(InvokeStreamHandler0), global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static)!)"));
            Assert.That(
                generatedSource,
                Does.Contain(
                    "var typedHandler = (global::GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<global::TestApp.VisibleStream, int>)handler;"));
        });
    }

    /// <summary>
    ///     验证当 runtime 缺少 <c>ICqrsStreamInvokerProvider</c> 时，
    ///     生成器会整体跳过 stream invoker provider 元数据发射，而不是保留孤立的 descriptor 成员。
    /// </summary>
    [Test]
    public void Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Provider_Interface()
    {
        var generatedSource = RunGenerator(RemoveBlock(
            StreamInvokerProviderSource,
            "public interface ICqrsStreamInvokerProvider",
            "public interface IEnumeratesCqrsStreamInvokerDescriptors"));

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain(
                    "internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry"));
            Assert.That(generatedSource, Does.Not.Contain("ICqrsStreamInvokerProvider"));
            Assert.That(generatedSource, Does.Not.Contain("IEnumeratesCqrsStreamInvokerDescriptors"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsStreamInvokerDescriptorEntry("));
            Assert.That(generatedSource, Does.Not.Contain("InvokeStreamHandler0"));
        });
    }

    /// <summary>
    ///     验证当 runtime 缺少 <c>IEnumeratesCqrsStreamInvokerDescriptors</c> 时，
    ///     生成器不会只发射 stream provider 的局部成员，而是整体保持不生成 provider 元数据。
    /// </summary>
    [Test]
    public void Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Enumerator()
    {
        var generatedSource = RunGenerator(RemoveBlock(
            StreamInvokerProviderSource,
            "public interface IEnumeratesCqrsStreamInvokerDescriptors",
            "public sealed class CqrsStreamInvokerDescriptor"));

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain(
                    "internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry"));
            Assert.That(generatedSource, Does.Not.Contain("ICqrsStreamInvokerProvider"));
            Assert.That(generatedSource, Does.Not.Contain("IEnumeratesCqrsStreamInvokerDescriptors"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsStreamInvokerDescriptorEntry("));
            Assert.That(generatedSource, Does.Not.Contain("InvokeStreamHandler0"));
        });
    }

    /// <summary>
    ///     验证当 runtime 缺少 <c>CqrsStreamInvokerDescriptor</c> 时，
    ///     生成器不会继续发射依赖描述符类型的 stream provider 元数据。
    /// </summary>
    [Test]
    public void Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Type()
    {
        var source = RenameTypeIdentifier(
            StreamInvokerProviderSource,
            "CqrsStreamInvokerDescriptor",
            "MissingCqrsStreamInvokerDescriptor");
        var generatedSource = RunGenerator(source);

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain(
                    "internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry"));
            Assert.That(generatedSource, Does.Not.Contain("ICqrsStreamInvokerProvider"));
            Assert.That(generatedSource, Does.Not.Contain("IEnumeratesCqrsStreamInvokerDescriptors"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsStreamInvokerDescriptorEntry("));
            Assert.That(generatedSource, Does.Not.Contain("InvokeStreamHandler0"));
        });
    }

    /// <summary>
    ///     验证当 runtime 缺少 <c>CqrsStreamInvokerDescriptorEntry</c> 时，
    ///     生成器不会继续保留 stream provider 的枚举接口或静态 invoker 元数据。
    /// </summary>
    [Test]
    public void Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Entry_Type()
    {
        var source = RenameTypeIdentifier(
            StreamInvokerProviderSource,
            "CqrsStreamInvokerDescriptorEntry",
            "MissingCqrsStreamInvokerDescriptorEntry");
        var generatedSource = RunGenerator(source);

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain(
                    "internal sealed class __GFrameworkGeneratedCqrsHandlerRegistry : global::GFramework.Cqrs.ICqrsHandlerRegistry"));
            Assert.That(generatedSource, Does.Not.Contain("ICqrsStreamInvokerProvider"));
            Assert.That(generatedSource, Does.Not.Contain("IEnumeratesCqrsStreamInvokerDescriptors"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsStreamInvokerDescriptorEntry("));
            Assert.That(generatedSource, Does.Not.Contain("InvokeStreamHandler0"));
        });
    }

    /// <summary>
    ///     验证当 stream handler 仍需走 precise reflected 注册时，
    ///     生成器即使检测到 stream invoker provider runtime 合同，也不会错误发射无法稳定表达隐藏请求/响应类型的 provider 元数据。
    /// </summary>
    [Test]
    public void Does_Not_Emit_Stream_Invoker_Provider_Metadata_For_Precise_Reflected_Stream_Registrations()
    {
        var generatedSource = RunGenerator(PreciseReflectedStreamInvokerProviderBoundarySource);

        Assert.Multiple(() =>
        {
            Assert.That(
                generatedSource,
                Does.Contain("typeof(global::GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<,>).MakeGenericType("));
            Assert.That(generatedSource, Does.Contain(".MakeArrayType()"));
            Assert.That(generatedSource, Does.Not.Contain("ICqrsStreamInvokerProvider"));
            Assert.That(generatedSource, Does.Not.Contain("IEnumeratesCqrsStreamInvokerDescriptors"));
            Assert.That(generatedSource, Does.Not.Contain("CqrsStreamInvokerDescriptorEntry("));
            Assert.That(generatedSource, Does.Not.Contain("InvokeStreamHandler0"));
        });
    }

    /// <summary>
    ///     验证日志字符串转义会覆盖换行、反斜杠和双引号，避免生成代码中的字符串字面量被意外截断。
    /// </summary>
    [Test]
    public void Escape_String_Literal_Handles_Control_Characters()
    {
        var method = typeof(CqrsHandlerRegistryGenerator).GetMethod(
            "EscapeStringLiteral",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);

        const string input = "line1\r\nline2\\\"";
        const string expected = "line1\\r\\nline2\\\\\\\"";
        var escaped = method!.Invoke(null, [input]) as string;

        Assert.That(escaped, Is.EqualTo(expected));
    }

    /// <summary>
    ///     运行 CQRS handler registry generator，并返回单个生成文件的源码文本。
    /// </summary>
    private static string RunGenerator(
        string source,
        params MetadataReference[] additionalReferences)
    {
        var execution = ExecuteGenerator(
            source,
            allowUnsafe: false,
            additionalReferences: additionalReferences);
        var generatorErrors = execution.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.That(
            generatorErrors,
            Is.Empty,
            () =>
                $"执行生成器时出现错误:{Environment.NewLine}{string.Join(Environment.NewLine, generatorErrors.Select(static diagnostic => diagnostic.ToString()))}");
        var compilationErrors = execution.CompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.That(
            compilationErrors,
            Is.Empty,
            () =>
                $"编译生成的代码时出现错误:{Environment.NewLine}{string.Join(Environment.NewLine, compilationErrors.Select(static diagnostic => diagnostic.ToString()))}");
        Assert.That(execution.GeneratedSources, Has.Length.EqualTo(1));

        return execution.GeneratedSources[0].content;
    }

    /// <summary>
    ///     从测试输入源码中移除两个稳定标记之间的整段合同定义，
    ///     避免回归用例依赖三引号字符串中的精确缩进。
    /// </summary>
    /// <param name="source">原始测试源码。</param>
    /// <param name="startMarker">待移除代码块的起始标记。</param>
    /// <param name="endMarker">待移除代码块之后紧邻的下一个稳定标记。</param>
    /// <returns>移除指定代码块后的新源码。</returns>
    private static string RemoveBlock(string source, string startMarker, string endMarker)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(startMarker);
        ArgumentNullException.ThrowIfNull(endMarker);

        var startIndex = source.IndexOf(startMarker, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            throw new InvalidOperationException("The requested start marker was not found in the generator test input.");
        }

        var endIndex = source.IndexOf(endMarker, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            throw new InvalidOperationException("The requested end marker was not found in the generator test input.");
        }

        return source.Remove(startIndex, endIndex - startIndex);
    }

    /// <summary>
    ///     仅按完整类型标识符重命名测试输入中的合同类型，避免误伤共享前缀的其他类型名。
    /// </summary>
    /// <param name="source">原始测试源码。</param>
    /// <param name="originalTypeName">原始合同类型名。</param>
    /// <param name="replacementTypeName">替换后的占位类型名。</param>
    /// <returns>完成精确类型重命名后的源码。</returns>
    private static string RenameTypeIdentifier(string source, string originalTypeName, string replacementTypeName)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(originalTypeName);
        ArgumentNullException.ThrowIfNull(replacementTypeName);

        var result = new System.Text.StringBuilder(source.Length);
        var currentIndex = 0;

        while (currentIndex < source.Length)
        {
            var matchIndex = source.IndexOf(originalTypeName, currentIndex, StringComparison.Ordinal);
            if (matchIndex < 0)
            {
                result.Append(source, currentIndex, source.Length - currentIndex);
                break;
            }

            result.Append(source, currentIndex, matchIndex - currentIndex);

            if (IsIdentifierBoundary(source, matchIndex - 1) &&
                IsIdentifierBoundary(source, matchIndex + originalTypeName.Length))
            {
                result.Append(replacementTypeName);
            }
            else
            {
                result.Append(originalTypeName);
            }

            currentIndex = matchIndex + originalTypeName.Length;
        }

        return result.ToString();
    }

    /// <summary>
    ///     判断给定位置是否位于 C# 标识符边界，用于避免把共享前缀的其他类型名一并改写。
    /// </summary>
    /// <param name="source">待检查的完整源码。</param>
    /// <param name="index">边界位置；允许落在字符串两端之外。</param>
    /// <returns>若当前位置不在标识符内部，则返回 <see langword="true" />。</returns>
    private static bool IsIdentifierBoundary(string source, int index)
    {
        if (index < 0 || index >= source.Length)
        {
            return true;
        }

        var character = source[index];
        return !char.IsLetterOrDigit(character) && character != '_';
    }

    /// <summary>
    ///     统计生成源码中某个固定片段的出现次数，用于锁定程序集级 fallback 特性的发射个数。
    /// </summary>
    /// <param name="text">待统计的完整生成源码。</param>
    /// <param name="value">需要计数的固定片段。</param>
    /// <returns><paramref name="value" /> 在 <paramref name="text" /> 中出现的次数。</returns>
    private static int CountOccurrences(string text, string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("The search value must not be null or empty.", nameof(value));

        var count = 0;
        var startIndex = 0;

        while (true)
        {
            var nextIndex = text.IndexOf(value, startIndex, global::System.StringComparison.Ordinal);
            if (nextIndex < 0)
                return count;

            count++;
            startIndex = nextIndex + value.Length;
        }
    }

    /// <summary>
    ///     运行 CQRS handler registry generator，并返回生成输出及相关诊断。
    /// </summary>
    /// <param name="source">输入源码。</param>
    /// <param name="allowUnsafe">
    ///     是否允许测试编译包含 <c>unsafe</c> 代码。
    ///     某些回归用例会故意构造带指针类型的非法 handler 合同，以覆盖 fallback 防御分支，此时需要启用该选项避免把缺少
    ///     <c>unsafe</c> 编译上下文的错误与目标生成器行为混淆。
    /// </param>
    /// <param name="additionalReferences">附加元数据引用，用于构造跨程序集场景。</param>
    /// <returns>包含生成源、生成器诊断和更新后编译诊断的执行结果。</returns>
    private static GeneratorExecutionResult ExecuteGenerator(
        string source,
        bool allowUnsafe = false,
        params MetadataReference[] additionalReferences)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestProject",
            [syntaxTree],
            MetadataReferenceTestBuilder.GetRuntimeMetadataReferences().AddRange(additionalReferences),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: allowUnsafe));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new CqrsHandlerRegistryGenerator().AsSourceGenerator()],
            parseOptions: (CSharpParseOptions)syntaxTree.Options);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out var generatorDiagnostics);

        var runResult = driver.GetRunResult();
        Assert.That(runResult.Results, Has.Length.EqualTo(1));
        var generatedSyntaxTrees = runResult.Results[0].GeneratedSources
            .Select(static sourceResult => sourceResult.SyntaxTree)
            .ToHashSet();
        var generatedSources = runResult.Results[0].GeneratedSources
            .Select(static sourceResult =>
                (filename: sourceResult.HintName, content: sourceResult.SourceText.ToString()))
            .ToArray();
        var compilationDiagnostics = updatedCompilation.GetDiagnostics().ToArray();
        var inputCompilationDiagnostics = compilationDiagnostics
            .Where(diagnostic =>
                diagnostic.Location.SourceTree is null ||
                !generatedSyntaxTrees.Contains(diagnostic.Location.SourceTree))
            .ToArray();
        var generatedCompilationDiagnostics = compilationDiagnostics
            .Where(diagnostic =>
                diagnostic.Location.SourceTree is not null &&
                generatedSyntaxTrees.Contains(diagnostic.Location.SourceTree))
            .ToArray();
        return new GeneratorExecutionResult(
            generatedSources,
            generatorDiagnostics.ToArray(),
            compilationDiagnostics,
            inputCompilationDiagnostics,
            generatedCompilationDiagnostics);
    }

    /// <summary>
    ///     封装 CQRS handler registry generator 的单次执行结果。
    /// </summary>
    /// <param name="GeneratedSources">本轮生成产生的源文件集合。</param>
    /// <param name="GeneratorDiagnostics">生成器自身报告的诊断集合。</param>
    /// <param name="CompilationDiagnostics">将生成结果并回编译后的完整编译诊断集合。</param>
    /// <param name="InputCompilationDiagnostics">仅来自输入源文件的编译诊断集合。</param>
    /// <param name="GeneratedCompilationDiagnostics">仅来自生成源文件的编译诊断集合。</param>
    private sealed record GeneratorExecutionResult(
        (string filename, string content)[] GeneratedSources,
        Diagnostic[] GeneratorDiagnostics,
        Diagnostic[] CompilationDiagnostics,
        Diagnostic[] InputCompilationDiagnostics,
        Diagnostic[] GeneratedCompilationDiagnostics);
}
