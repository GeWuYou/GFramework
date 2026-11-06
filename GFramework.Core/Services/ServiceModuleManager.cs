// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;
using GFramework.Core.Services.Modules;

namespace GFramework.Core.Services;

/// <summary>
///     当前架构的服务模块集合：配置时登记描述，正式 provider 构建后按优先级启动，
///     停止时逆序处理并聚合失败。不保存跨架构运行态，也不吞掉清理错误。
/// </summary>
public sealed class ServiceModuleManager : IServiceModuleManager
{
    private readonly ILogger _logger;
    private readonly List<IServiceModule> _modules = [];
    private bool _builtInModulesRegistered;
    private bool _initializationStarted;
    private IIocContainer? _container;

    /// <summary>使用实例工厂创建管理器；缺省不访问全局日志解析器。</summary>
    /// <param name="loggerFactoryProvider">当前实例日志工厂。</param>
    public ServiceModuleManager(ILoggerFactoryProvider? loggerFactoryProvider = null)
    {
        _logger = (loggerFactoryProvider ?? new ConsoleLoggerFactoryProvider())
            .CreateLogger(nameof(ServiceModuleManager));
    }

    /// <summary>登记模块；默认基础描述已登记时立即加入同一未冻结服务集合。</summary>
    /// <param name="module">当前实例拥有的模块。</param>
    public void RegisterModule(IServiceModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (_initializationStarted)
            throw new InvalidOperationException("Service module registration is closed.");
        if (_modules.Any(existing => string.Equals(existing.ModuleName, module.ModuleName, StringComparison.Ordinal)))
            return;
        var insertion = _modules.FindIndex(existing => existing.Priority > module.Priority);
        if (insertion < 0)
            _modules.Add(module);
        else
            _modules.Insert(insertion, module);
        if (_builtInModulesRegistered && module.IsEnabled)
            module.Register(_container!);
    }

    /// <summary>登记内置服务描述及显式外部模块工厂，尚不启动任何模块。</summary>
    /// <param name="container">当前架构拥有的容器。</param>
    public void RegisterBuiltInModules(IIocContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        if (_builtInModulesRegistered)
            return;
        _container = container;
        RegisterModule(new EventBusModule());
        RegisterModule(new CommandExecutorModule());
        RegisterModule(new CqrsRuntimeModule());
        RegisterModule(new QueryExecutorModule());
        RegisterModule(new AsyncQueryExecutorModule());
        foreach (var module in ArchitectureModuleRegistry.CreateModules())
            RegisterModule(module);
        foreach (var module in _modules.Where(module => module.IsEnabled))
            module.Register(container);
        _builtInModulesRegistered = true;
    }

    /// <summary>获取当前实例的模块只读视图。</summary>
    /// <returns>已登记模块。</returns>
    public IReadOnlyList<IServiceModule> GetModules() => _modules.AsReadOnly();

    /// <summary>provider 构建后按优先级启动模块，取消后不再开始下一模块。</summary>
    /// <param name="asyncMode">是否允许异步模块。</param>
    /// <param name="cancellationToken">启动取消令牌。</param>
    /// <returns>模块启动完成。</returns>
    public async Task InitializeAllAsync(bool asyncMode, CancellationToken cancellationToken = default)
    {
        if (_initializationStarted)
            throw new InvalidOperationException("Service modules can only be initialized once.");
        _initializationStarted = true;
        foreach (var module in _modules.Where(module => module.IsEnabled))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (module is IAsyncInitializable asyncInitializable)
            {
                if (!asyncMode)
                    throw new InvalidOperationException("An asynchronous module requires InitializeAsync().");
                await asyncInitializable.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }
            else
                module.Initialize();
        }
    }

    /// <summary>包括部分注册/启动的模块在内，逆序尝试全部清理后报告异常。</summary>
    /// <returns>所有模块停止完成。</returns>
    public async ValueTask DestroyAllAsync()
    {
        var errors = new List<Exception>();
        for (var i = _modules.Count - 1; i >= 0; i--)
        {
            if (!_modules[i].IsEnabled)
                continue;
            try
            {
                await _modules[i].DestroyAsync().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                _logger.Error($"Module {_modules[i].ModuleName} cleanup failed.", error);
                errors.Add(error);
            }
        }
        _modules.Clear();
        if (errors.Count != 0)
            throw new AggregateException("Service module cleanup failed.", errors);
    }
}
