using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Architectures;

/// <summary>
///     协调架构初始化期间的基础设施准备工作。
///     该类型将环境初始化、服务模块启动、上下文绑定和服务容器配置从 <see cref="Architecture" /> 中拆出，
///     使核心架构类只保留生命周期入口和公共 API 协调职责。
/// </summary>
internal sealed class ArchitectureBootstrapper(
    Type architectureType,
    IEnvironment environment,
    IArchitectureServices services,
    ILogger logger)
{
    /// <summary>
    ///     在执行用户 <c>OnInitialize</c> 之前准备架构运行时。
    ///     该流程必须保证环境、内置服务、上下文和服务钩子已经可用，
    ///     因为用户初始化逻辑通常会立即访问事件总线、查询执行器或环境对象。
    /// </summary>
    /// <param name="existingContext">调用方已经提供的上下文；如果为空则创建默认上下文。</param>
    /// <param name="configurator">可选的容器配置委托，用于接入额外服务或覆盖默认依赖绑定。</param>
    /// <param name="asyncMode">是否以异步模式初始化服务模块。</param>
    /// <returns>已绑定到当前架构类型的架构上下文。</returns>
    public async Task<IArchitectureContext> PrepareForInitializationAsync(
        IArchitectureContext? existingContext,
        Action<IServiceCollection>? configurator,
        bool asyncMode)
    {
        InitializeEnvironment();
        RegisterBuiltInModules();
        EnsureEnvironmentRegistered();

        var context = EnsureContext(existingContext);
        ConfigureServices(context, configurator);
        await InitializeServiceModulesAsync(asyncMode);
        return context;
    }

    /// <summary>
    ///     完成用户组件初始化之后的收尾工作。
    ///     冻结容器可以阻止 Ready 阶段之后的意外服务注册，保持运行时依赖图稳定。
    /// </summary>
    public void CompleteInitialization()
    {
        services.Container.Freeze();
        logger.Info("IOC container frozen");
    }

    /// <summary>
    ///     初始化运行环境，使环境对象在后续服务构建和用户初始化前进入可用状态。
    /// </summary>
    private void InitializeEnvironment()
    {
        environment.Initialize();
    }

    /// <summary>
    ///     注册框架内置服务模块。
    ///     该步骤必须先于执行服务钩子，以便容器具备 CQRS 和事件总线等基础服务。
    /// </summary>
    private void RegisterBuiltInModules()
    {
        services.ModuleManager.RegisterBuiltInModules(services.Container);
    }

    /// <summary>
    ///     确保环境对象可以通过架构容器解析。
    ///     如果调用方已经预先注册了自定义环境实例，则保留现有绑定，避免覆盖外部配置。
    /// </summary>
    private void EnsureEnvironmentRegistered()
    {
        if (!services.Container.Contains<IEnvironment>())
            services.Container.RegisterPlurality(environment);
    }

    /// <summary>
    ///     获取本次初始化使用的架构上下文，并将其绑定到全局游戏上下文表。
    ///     绑定发生在用户初始化之前，确保组件在注册阶段即可通过架构类型解析上下文。
    /// </summary>
    /// <param name="existingContext">外部提供的上下文。</param>
    /// <returns>实际用于本次初始化的上下文实例。</returns>
    private IArchitectureContext EnsureContext(IArchitectureContext? existingContext)
    {
        var context = existingContext ?? new ArchitectureContext(services.Container);
        GameContext.Bind(architectureType, context);
        return context;
    }

    /// <summary>
    ///     为服务容器设置上下文并执行扩展配置钩子。
    ///     这一步统一承接 CQRS 运行时与容器扩展的接入点，避免 <see cref="Architecture" /> 直接操作容器细节。
    /// </summary>
    /// <param name="context">当前架构上下文。</param>
    /// <param name="configurator">可选的服务集合配置委托。</param>
    private void ConfigureServices(IArchitectureContext context, Action<IServiceCollection>? configurator)
    {
        services.SetContext(context);
        services.Container.RegisterCqrsHandlersFromAssemblies(
        [
            architectureType.Assembly,
            typeof(ArchitectureContext).Assembly
        ]);

        if (configurator is null)
            logger.Debug("No external service configurator provided. Using built-in CQRS runtime registration only.");

        services.Container.ExecuteServicesHook(configurator);
    }

    /// <summary>
    ///     初始化所有服务模块。
    ///     该过程在用户注册系统、模型和工具之前完成，避免组件在初始化期间访问未准备好的服务。
    /// </summary>
    /// <param name="asyncMode">是否允许异步初始化服务模块。</param>
    private async Task InitializeServiceModulesAsync(bool asyncMode)
    {
        await services.ModuleManager.InitializeAllAsync(asyncMode);
    }
}
