using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.lifecycle;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.properties;
using GFramework.Core.logging;
using GFramework.Core.services.modules;

namespace GFramework.Core.services;

/// <summary>
///     服务模块管理器，负责注册、初始化和销毁架构中的服务模块。
///     支持模块的优先级排序、异步初始化和异常安全的销毁流程。
/// </summary>
public sealed class ServiceModuleManager : IServiceModuleManager
{
    private readonly ILogger _logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(ServiceModuleManager));
    private readonly List<IServiceModule> _modules = [];

    /// <summary>
    ///     注册单个服务模块。
    ///     如果模块为空或已存在同名模块，则记录警告日志并跳过注册。
    /// </summary>
    /// <param name="module">要注册的服务模块实例。</param>
    public void RegisterModule(IServiceModule? module)
    {
        if (module == null)
        {
            _logger.Warn("Attempted to register null module");
            return;
        }

        if (_modules.Any(m => m.ModuleName == module.ModuleName))
        {
            _logger.Warn($"Module {module.ModuleName} already registered");
            return;
        }

        _modules.Add(module);
        _logger.Debug($"Module registered: {module.ModuleName} (Priority: {module.Priority})");
    }

    /// <summary>
    ///     注册内置服务模块。
    ///     根据配置属性决定是否启用特定模块（如ECS模块），并对模块按优先级排序后注册到容器中。
    /// </summary>
    /// <param name="container">依赖注入容器，用于注册模块提供的服务。</param>
    /// <param name="properties">架构配置属性，用于控制模块的启用状态。</param>
    public void RegisterBuiltInModules(IIocContainer container, ArchitectureProperties properties)
    {
        RegisterModule(new EventBusModule());
        RegisterModule(new CommandExecutorModule());
        RegisterModule(new QueryExecutorModule());
        RegisterModule(new AsyncQueryExecutorModule());

        if (properties.EnableEcs)
        {
            RegisterModule(new EcsModule(enabled: true));
            _logger.Info("ECS module enabled via configuration");
        }

        var sortedModules = _modules.OrderBy(m => m.Priority).ToList();
        _modules.Clear();
        _modules.AddRange(sortedModules);

        foreach (var module in _modules.Where(module => module.IsEnabled))
        {
            _logger.Debug($"Registering services for module: {module.ModuleName}");
            module.Register(container);
        }

        _logger.Info($"Registered {_modules.Count} built-in service modules");
    }

    /// <summary>
    ///     获取所有已注册的服务模块列表。
    /// </summary>
    /// <returns>只读的服务模块列表。</returns>
    public IReadOnlyList<IServiceModule> GetModules()
    {
        return _modules.AsReadOnly();
    }

    /// <summary>
    ///     异步初始化所有启用的服务模块。
    ///     支持同步和异步初始化模式，优先使用异步接口（如果模块实现了IAsyncInitializable）。
    /// </summary>
    /// <param name="asyncMode">是否启用异步初始化模式。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task InitializeAllAsync(bool asyncMode)
    {
        _logger.Info($"Initializing {_modules.Count} service modules");

        foreach (var module in _modules.Where(m => m.IsEnabled))
        {
            _logger.Debug($"Initializing module: {module.ModuleName}");

            if (asyncMode && module is IAsyncInitializable asyncInitializable)
            {
                await asyncInitializable.InitializeAsync();
            }
            else
            {
                module.Initialize();
            }
        }

        _logger.Info("All service modules initialized");
    }

    /// <summary>
    ///     异步销毁所有启用的服务模块。
    ///     按逆序销毁模块以确保依赖关系正确释放，并捕获异常避免中断整个销毁流程。
    /// </summary>
    /// <returns>表示异步操作的值任务。</returns>
    public async ValueTask DestroyAllAsync()
    {
        _logger.Info($"Destroying {_modules.Count} service modules");

        for (var i = _modules.Count - 1; i >= 0; i--)
        {
            var module = _modules[i];
            if (!module.IsEnabled) continue;

            try
            {
                _logger.Debug($"Destroying module: {module.ModuleName}");
                await module.DestroyAsync();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error destroying module {module.ModuleName}", ex);
            }
        }

        _modules.Clear();
        _logger.Info("All service modules destroyed");
    }
}