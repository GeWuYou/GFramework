using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.lifecycle;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.properties;
using GFramework.Core.ecs;
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
    private bool _builtInModulesRegistered;

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
    ///     注册内置服务模块，并根据优先级排序后完成服务注册。
    ///     内置模块包括事件总线、命令执行器、查询执行器等核心模块，
    ///     并根据配置决定是否启用ECS模块。
    /// </summary>
    /// <param name="container">IoC容器实例，用于模块服务注册。</param>
    /// <param name="properties">架构属性配置，用于判断是否启用ECS模块。</param>
    public void RegisterBuiltInModules(IIocContainer container, ArchitectureProperties properties)
    {
        if (_builtInModulesRegistered)
        {
            _logger.Warn("Built-in modules already registered, skipping duplicate registration");
            return;
        }

        RegisterModule(new EventBusModule());
        RegisterModule(new CommandExecutorModule());
        RegisterModule(new QueryExecutorModule());
        RegisterModule(new AsyncQueryExecutorModule());

        if (properties.EnableEcs)
        {
            RegisterModule(new ArchEcsModule(enabled: true));
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

        _builtInModulesRegistered = true;
        _logger.Info($"Registered {_modules.Count} built-in service modules");
    }

    /// <summary>
    ///     获取当前已注册的所有服务模块。
    /// </summary>
    /// <returns>只读的服务模块列表。</returns>
    public IReadOnlyList<IServiceModule> GetModules()
    {
        return _modules.AsReadOnly();
    }

    /// <summary>
    ///     异步初始化所有已启用的服务模块。
    ///     根据模块是否实现异步初始化接口，选择同步或异步初始化方式。
    /// </summary>
    /// <param name="asyncMode">是否以异步模式初始化模块。</param>
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
    ///     异步销毁所有已启用的服务模块。
    ///     按照逆序销毁模块，确保依赖关系正确处理，并捕获销毁过程中的异常。
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
        _builtInModulesRegistered = false;
        _logger.Info("All service modules destroyed");
    }
}