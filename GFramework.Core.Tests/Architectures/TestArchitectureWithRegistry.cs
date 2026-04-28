using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="RegistryInitializationHookBaseTests" /> 提供已挂接 <see cref="TestRegistry" /> 的架构测试替身。
/// </summary>
public class TestArchitectureWithRegistry : IArchitecture
{
    /// <summary>
    ///     使用给定测试注册表创建架构测试替身。
    /// </summary>
    /// <param name="registry">要通过架构上下文暴露给钩子的测试注册表。</param>
    public TestArchitectureWithRegistry(TestRegistry registry)
    {
        Context = new TestArchitectureContextWithRegistry(registry);
    }

    /// <summary>
    ///     获取测试替身公开的服务配置入口。
    ///     当前切片不验证服务配置流程，因此始终保持为空。
    /// </summary>
    public Action<IServiceCollection>? Configurator { get; }

    /// <summary>
    ///     获取当前测试替身使用的架构上下文。
    /// </summary>
    public IArchitectureContext Context { get; }

    Action<IServiceCollection>? IArchitecture.Configurator => Configurator;

    T IArchitecture.RegisterSystem<T>(T system)
    {
        throw new NotSupportedException();
    }

    T IArchitecture.RegisterModel<T>(T model)
    {
        throw new NotSupportedException();
    }

    T IArchitecture.RegisterUtility<T>(T utility)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试替身未实现 CQRS 管道行为注册。
    /// </summary>
    /// <typeparam name="TBehavior">行为类型。</typeparam>
    /// <exception cref="NotSupportedException">该测试替身不参与 CQRS 管道配置验证。</exception>
    public void RegisterCqrsPipelineBehavior<TBehavior>() where TBehavior : class
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试替身未实现显式程序集 CQRS 处理器接入入口。
    /// </summary>
    /// <param name="assembly">包含 CQRS 处理器或生成注册器的程序集。</param>
    /// <exception cref="NotSupportedException">该测试替身不参与 CQRS 程序集接入路径验证。</exception>
    public void RegisterCqrsHandlersFromAssembly(Assembly assembly)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试替身未实现显式程序集 CQRS 处理器接入入口。
    /// </summary>
    /// <param name="assemblies">要接入的程序集集合。</param>
    /// <exception cref="NotSupportedException">该测试替身不参与 CQRS 程序集接入路径验证。</exception>
    public void RegisterCqrsHandlersFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试替身未实现模块安装流程。
    /// </summary>
    /// <param name="module">要安装的模块。</param>
    /// <returns>此方法始终抛出异常，不返回模块实例。</returns>
    /// <exception cref="NotSupportedException">该测试替身不参与模块安装路径验证。</exception>
    public IArchitectureModule InstallModule(IArchitectureModule module)
    {
        throw new NotSupportedException();
    }

    IArchitectureLifecycleHook IArchitecture.RegisterLifecycleHook(IArchitectureLifecycleHook hook)
    {
        RegisterLifecycleHook(hook);
        return hook;
    }

    Task IArchitecture.WaitUntilReadyAsync()
    {
        return WaitUntilReadyAsync();
    }

    /// <summary>
    ///     测试替身未实现工具延迟注册入口。
    /// </summary>
    /// <typeparam name="T">工具类型。</typeparam>
    /// <param name="onCreated">工具创建后的回调。</param>
    /// <exception cref="NotSupportedException">该测试替身不参与工具注册路径验证。</exception>
    public void RegisterUtility<T>(Action<T>? onCreated = default) where T : class, IUtility
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试替身未实现 Model 延迟注册入口。
    /// </summary>
    /// <typeparam name="T">Model 类型。</typeparam>
    /// <param name="onCreated">Model 创建后的回调。</param>
    /// <exception cref="NotSupportedException">该测试替身不参与 Model 注册路径验证。</exception>
    public void RegisterModel<T>(Action<T>? onCreated = default) where T : class, IModel
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试替身未实现 System 延迟注册入口。
    /// </summary>
    /// <typeparam name="T">System 类型。</typeparam>
    /// <param name="onCreated">System 创建后的回调。</param>
    /// <exception cref="NotSupportedException">该测试替身不参与 System 注册路径验证。</exception>
    public void RegisterSystem<T>(Action<T>? onCreated = default) where T : class, ISystem
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     初始化测试替身。
    ///     该切片只需要上下文可用，因此初始化过程保持为空实现。
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    ///     测试替身未实现销毁路径。
    /// </summary>
    /// <exception cref="NotSupportedException">该测试替身不参与销毁路径验证。</exception>
    public void Destroy()
    {
        throw new NotSupportedException();
    }

    Task IAsyncInitializable.InitializeAsync()
    {
        return InitializeAsync();
    }

    ValueTask IAsyncDestroyable.DestroyAsync()
    {
        return DestroyAsync();
    }

    /// <summary>
    ///     测试替身未实现就绪等待流程。
    /// </summary>
    /// <returns>此方法始终抛出异常，不返回等待任务。</returns>
    /// <exception cref="NotSupportedException">该测试替身不参与就绪等待路径验证。</exception>
    public Task WaitUntilReadyAsync()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     注册架构生命周期钩子。
    ///     当前切片不依赖生命周期钩子执行，因此保持空实现。
    /// </summary>
    /// <param name="hook">要忽略的生命周期钩子。</param>
    public void RegisterLifecycleHook(IArchitectureLifecycleHook hook)
    {
    }

    /// <summary>
    ///     测试替身未实现异步初始化路径。
    /// </summary>
    /// <returns>此方法始终抛出异常，不返回初始化任务。</returns>
    /// <exception cref="NotSupportedException">该测试替身不参与异步初始化验证。</exception>
    public Task InitializeAsync()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     测试替身未实现异步销毁路径。
    /// </summary>
    /// <returns>此方法始终抛出异常，不返回销毁任务。</returns>
    /// <exception cref="NotSupportedException">该测试替身不参与异步销毁验证。</exception>
    public ValueTask DestroyAsync()
    {
        throw new NotSupportedException();
    }
}
