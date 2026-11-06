// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Architectures;

/// <summary>
///     在用户配置之前登记当前实例的环境、上下文和内置基础设施描述。
///     不构建 provider、不启动模块，也不绑定进程级 GameContext。
/// </summary>
internal sealed class ArchitectureBootstrapper(
    Type architectureType,
    IEnvironment environment,
    IArchitectureServices services,
    ILoggerFactoryProvider loggerFactoryProvider)
{
    /// <summary>登记可供后续正式 provider 使用的实例与模块。</summary>
    /// <param name="context">当前实例上下文。</param>
    /// <param name="configurator">额外服务描述。</param>
    public void Prepare(IArchitectureContext context, Action<IServiceCollection>? configurator)
    {
        environment.Initialize();
        services.SetContext(context);
        if (!services.Container.HasRegistration(typeof(ILoggerFactoryProvider)))
            services.Container.Register(loggerFactoryProvider);
        if (!services.Container.HasRegistration(typeof(IEnvironment)))
            services.Container.Register<IEnvironment>(environment);
        if (!services.Container.HasRegistration(typeof(IArchitectureContext)))
            services.Container.Register<IArchitectureContext>(context);
        services.ModuleManager.RegisterBuiltInModules(services.Container);
        services.Container.RegisterCqrsHandlersFromAssemblies(
        [
            architectureType.Assembly,
            typeof(ArchitectureContext).Assembly
        ]);
        services.Container.ExecuteServicesHook(configurator);
    }
}
