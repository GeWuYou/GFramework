// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Architectures;

/// <summary>
///     配置阶段只记录组件的 DI 描述和生命周期参与关系。正式 provider 构建后统一激活，
///     所有构造依赖都由同一容器拥有，不再维护预注册解析器或第二份单例缓存。
/// </summary>
internal sealed class ArchitectureComponentRegistry(
    IArchitecture architecture,
    IArchitectureServices services,
    ArchitectureLifecycle lifecycle)
{
    private readonly List<Action> _activations = [];
    private readonly HashSet<Type> _registeredTypes = [];
    private readonly HashSet<object> _registeredInstances = new(ReferenceEqualityComparer.Instance);

    /// <summary>登记借用的 System 实例，停止业务但不接管其 IDisposable 所有权。</summary>
    /// <typeparam name="T">组件契约。</typeparam>
    /// <param name="system">调用者拥有的实例。</param>
    /// <returns>传入实例。</returns>
    public T RegisterSystem<T>(T system) where T : ISystem => RegisterInstance(system);

    /// <summary>登记由正式 DI 创建的 System。</summary>
    /// <typeparam name="T">实现类型。</typeparam>
    /// <param name="onCreated">激活后、初始化前执行一次的回调。</param>
    public void RegisterSystem<T>(Action<T>? onCreated = null) where T : class, ISystem => RegisterType(onCreated);

    /// <summary>登记借用的 Model。</summary>
    /// <typeparam name="T">组件契约。</typeparam>
    /// <param name="model">调用者拥有的实例。</param>
    /// <returns>传入实例。</returns>
    public T RegisterModel<T>(T model) where T : IModel => RegisterInstance(model);

    /// <summary>登记由正式 DI 创建的 Model。</summary>
    /// <typeparam name="T">实现类型。</typeparam>
    /// <param name="onCreated">激活后回调。</param>
    public void RegisterModel<T>(Action<T>? onCreated = null) where T : class, IModel => RegisterType(onCreated);

    /// <summary>登记借用的 Utility。</summary>
    /// <typeparam name="T">工具契约。</typeparam>
    /// <param name="utility">调用者拥有的实例。</param>
    /// <returns>传入实例。</returns>
    public T RegisterUtility<T>(T utility) where T : IUtility => RegisterInstance(utility);

    /// <summary>登记由正式 DI 创建的 Utility，包含普通无上下文工具。</summary>
    /// <typeparam name="T">实现类型。</typeparam>
    /// <param name="onCreated">激活后回调。</param>
    public void RegisterUtility<T>(Action<T>? onCreated = null) where T : class, IUtility => RegisterType(onCreated);

    /// <summary>统一激活，不允许回调在此阶段添加新的服务描述。</summary>
    public void ActivateComponents()
    {
        foreach (var activate in _activations)
            activate();
        _activations.Clear();
    }

    /// <summary>实例 alias 全部为外部实例绑定，由原创建者负责最终释放。</summary>
    /// <typeparam name="T">实例类型。</typeparam>
    /// <param name="instance">组件。</param>
    /// <returns>传入实例。</returns>
    private T RegisterInstance<T>(T instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (!_registeredInstances.Add(instance))
            return instance;
        services.Container.RegisterPlurality(instance);
        _activations.Add(() => Activate(instance, onCreated: null));
        return instance;
    }

    /// <summary>
    ///     可释放实现只使用一个 owning 服务键，避免标准 DI 将多个 factory alias 重复跟踪为资源。
    ///     普通不可释放组件保留接口 alias；生命周期由这里显式登记，不依赖 alias 扫描。
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    /// <param name="onCreated">正式激活后的回调。</param>
    private void RegisterType<T>(Action<T>? onCreated) where T : class
    {
        if (!_registeredTypes.Add(typeof(T)))
            throw new InvalidOperationException($"Component type {typeof(T)} is already registered. Select one lifecycle role.");
        if (typeof(IDisposable).IsAssignableFrom(typeof(T)) ||
            typeof(IAsyncDisposable).IsAssignableFrom(typeof(T)))
            services.Container.RegisterSingleton<T, T>();
        else
            services.Container.RegisterPlurality<T>();
        _activations.Add(() => Activate(services.Container.GetRequired<T>(), onCreated));
    }

    /// <summary>显式注入上下文，登记参与者，再执行用户激活回调。</summary>
    /// <typeparam name="T">组件类型。</typeparam>
    /// <param name="component">正式解析的实例。</param>
    /// <param name="onCreated">可选回调。</param>
    private void Activate<T>(T component, Action<T>? onCreated)
    {
        if (component is IContextAware aware)
            aware.SetContext(architecture.Context);
        lifecycle.RegisterLifecycleComponent(component!);
        onCreated?.Invoke(component);
    }
}
