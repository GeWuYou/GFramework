using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构组件注册管理器
///     负责管理 System、Model、Utility 的注册
/// </summary>
internal sealed class ArchitectureComponentRegistry(
    IArchitecture architecture,
    IArchitectureConfiguration configuration,
    IArchitectureServices services,
    ArchitectureLifecycle lifecycle,
    ILogger logger)
{
    private readonly ArchitectureComponentActivator _activator = new(services.Container, logger);

    #region Validation

    /// <summary>
    ///     验证是否允许注册组件
    /// </summary>
    /// <param name="componentType">组件类型描述</param>
    /// <exception cref="InvalidOperationException">当不允许注册时抛出</exception>
    private void ValidateRegistration(string componentType)
    {
        if (lifecycle.CurrentPhase < ArchitecturePhase.Ready ||
            configuration.ArchitectureProperties.AllowLateRegistration) return;
        var errorMsg = $"Cannot register {componentType} after Architecture is Ready";
        logger.Error(errorMsg);
        throw new InvalidOperationException(errorMsg);
    }

    #endregion

    #region System Registration

    /// <summary>
    ///     注册一个系统到架构中
    /// </summary>
    /// <typeparam name="TSystem">要注册的系统类型</typeparam>
    /// <param name="system">要注册的系统实例</param>
    /// <returns>注册成功的系统实例</returns>
    public TSystem RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem
    {
        ValidateRegistration("system");

        logger.Debug($"Registering system: {typeof(TSystem).Name}");

        system.SetContext(architecture.Context);
        services.Container.RegisterPlurality(system);

        // 处理生命周期
        lifecycle.RegisterLifecycleComponent(system);

        logger.Info($"System registered: {typeof(TSystem).Name}");
        return system;
    }

    /// <summary>
    ///     注册系统类型，并在注册阶段由当前服务集合立即创建实例。
    ///     这样可以确保该系统参与当前架构初始化批次，而不是等到 Ready 之后首次解析时才延迟创建。
    /// </summary>
    /// <typeparam name="T">系统类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调</param>
    public void RegisterSystem<T>(Action<T>? onCreated = null) where T : class, ISystem
    {
        ValidateRegistration("system");
        logger.Debug($"Registering system type: {typeof(T).Name}");

        // 类型注册路径在注册阶段就物化实例，确保组件能参与当前初始化批次。
        var system = _activator.CreateInstance<T>();
        system.SetContext(architecture.Context);
        lifecycle.RegisterLifecycleComponent(system);
        onCreated?.Invoke(system);
        services.Container.RegisterPlurality(system);

        logger.Info($"System type registered: {typeof(T).Name}");
    }

    #endregion

    #region Model Registration

    /// <summary>
    ///     注册一个模型到架构中
    /// </summary>
    /// <typeparam name="TModel">要注册的模型类型</typeparam>
    /// <param name="model">要注册的模型实例</param>
    /// <returns>注册成功的模型实例</returns>
    public TModel RegisterModel<TModel>(TModel model) where TModel : IModel
    {
        ValidateRegistration("model");

        logger.Debug($"Registering model: {typeof(TModel).Name}");

        model.SetContext(architecture.Context);
        services.Container.RegisterPlurality(model);

        // 处理生命周期
        lifecycle.RegisterLifecycleComponent(model);

        logger.Info($"Model registered: {typeof(TModel).Name}");
        return model;
    }

    /// <summary>
    ///     注册模型类型，并在注册阶段由当前服务集合立即创建实例。
    ///     这样可以确保该模型参与当前架构初始化批次，而不是等到 Ready 之后首次解析时才延迟创建。
    /// </summary>
    /// <typeparam name="T">模型类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调</param>
    public void RegisterModel<T>(Action<T>? onCreated = null) where T : class, IModel
    {
        ValidateRegistration("model");
        logger.Debug($"Registering model type: {typeof(T).Name}");

        var model = _activator.CreateInstance<T>();
        model.SetContext(architecture.Context);
        lifecycle.RegisterLifecycleComponent(model);
        onCreated?.Invoke(model);
        services.Container.RegisterPlurality(model);

        logger.Info($"Model type registered: {typeof(T).Name}");
    }

    #endregion

    #region Utility Registration

    /// <summary>
    ///     注册一个工具到架构中
    /// </summary>
    /// <typeparam name="TUtility">要注册的工具类型</typeparam>
    /// <param name="utility">要注册的工具实例</param>
    /// <returns>注册成功的工具实例</returns>
    public TUtility RegisterUtility<TUtility>(TUtility utility) where TUtility : IUtility
    {
        ValidateRegistration("utility");
        logger.Debug($"Registering utility: {typeof(TUtility).Name}");

        // 处理上下文工具类型的设置和生命周期管理
        utility.IfType<IContextUtility>(contextUtility =>
        {
            contextUtility.SetContext(architecture.Context);
            // 处理生命周期
            lifecycle.RegisterLifecycleComponent(contextUtility);
        });

        services.Container.RegisterPlurality(utility);
        logger.Info($"Utility registered: {typeof(TUtility).Name}");
        return utility;
    }

    /// <summary>
    ///     注册工具类型，由 DI 容器自动创建实例
    /// </summary>
    /// <typeparam name="T">工具类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调</param>
    public void RegisterUtility<T>(Action<T>? onCreated = null) where T : class, IUtility
    {
        ValidateRegistration("utility");
        logger.Debug($"Registering utility type: {typeof(T).Name}");

        services.Container.RegisterFactory<T>(sp =>
        {
            var utility = ActivatorUtilities.CreateInstance<T>(sp);

            // 如果是 IContextUtility，设置上下文
            if (utility is IContextUtility contextUtility)
            {
                contextUtility.SetContext(architecture.Context);
                lifecycle.RegisterLifecycleComponent(contextUtility);
            }

            // 用户自定义钩子
            onCreated?.Invoke(utility);

            logger.Debug($"Utility created: {typeof(T).Name}");
            return utility;
        });

        logger.Info($"Utility type registered: {typeof(T).Name}");
    }

    #endregion
}