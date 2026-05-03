// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Core.Abstractions.Events;
using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     提供官方的 C# 配置启动帮助器。
///     该类型负责把配置注册表、YAML 加载器与开发期热重载句柄收敛到一个长生命周期对象中，
///     让消费者项目可以通过一个稳定入口完成配置启动，而不是在多个脚本里重复拼装运行时细节。
/// </summary>
/// <remarks>
///     生命周期转换会串行化执行，因此并发调用只会观察到已经提交完成的加载器与热重载句柄。
///     如果初始化或热重载启动在中途失败，当前实例会保留失败前的稳定状态，而不会公开半初始化对象。
/// </remarks>
public sealed class GameConfigBootstrap : IDisposable
{
    private const string ConfigureLoaderCannotBeNullMessage = "ConfigureLoader must be provided.";
    private const string RootPathCannotBeNullOrWhiteSpaceMessage = "Root path cannot be null or whitespace.";

    // All lifecycle transitions share one gate so initialization, hot-reload startup,
    // stop, and disposal never publish half-finished state to concurrent callers.
#if NET9_0_OR_GREATER
    private readonly Lock _stateGate = new();
#else
    private readonly object _stateGate = new();
#endif
    private readonly GameConfigBootstrapOptions _options;
    private IUnRegister? _hotReload;
    private YamlConfigLoader? _loader;
    private bool _disposed;
    private bool _isInitializing;
    private bool _isStartingHotReload;
    private bool _stopHotReloadAfterStart;

    /// <summary>
    ///     使用指定选项创建配置启动帮助器。
    /// </summary>
    /// <param name="options">配置启动约定。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="options" /> 为空时抛出。</exception>
    /// <exception cref="ArgumentException">
    ///     当 <paramref name="options" /> 的 <see cref="GameConfigBootstrapOptions.RootPath" /> 为空，
    ///     或 <see cref="GameConfigBootstrapOptions.ConfigureLoader" /> 未提供时抛出。
    /// </exception>
    public GameConfigBootstrap(GameConfigBootstrapOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.RootPath))
        {
            throw new ArgumentException(
                RootPathCannotBeNullOrWhiteSpaceMessage,
                nameof(options));
        }

        if (options.ConfigureLoader == null)
        {
            throw new ArgumentException(
                ConfigureLoaderCannotBeNullMessage,
                nameof(options));
        }

        _options = options;
        RootPath = options.RootPath;
        Registry = options.Registry ?? new ConfigRegistry();
    }

    /// <summary>
    ///     获取配置根目录。
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    ///     获取当前配置生命周期共享的注册表。
    ///     默认情况下该实例由启动帮助器创建；如调用方传入自定义注册表，则返回同一个对象。
    /// </summary>
    public IConfigRegistry Registry { get; }

    /// <summary>
    ///     获取一个值，指示启动帮助器是否已经成功完成初次加载。
    ///     该状态只会在完整初始化链路（包括可选热重载启动）成功后才对外可见，
    ///     避免并发调用观察到半初始化生命周期。
    /// </summary>
    public bool IsInitialized
    {
        get
        {
            lock (_stateGate)
            {
                return _loader != null;
            }
        }
    }

    /// <summary>
    ///     获取一个值，指示开发期热重载是否已启用。
    ///     只有当监听句柄已经成功创建并提交到当前生命周期后，该属性才会返回 <see langword="true" />。
    /// </summary>
    public bool IsHotReloadEnabled
    {
        get
        {
            lock (_stateGate)
            {
                return _hotReload != null;
            }
        }
    }

    /// <summary>
    ///     获取当前生效的 YAML 配置加载器。
    ///     只有在 <see cref="InitializeAsync" /> 成功返回后该属性才可访问。
    /// </summary>
    /// <exception cref="ObjectDisposedException">当当前实例已释放时抛出。</exception>
    /// <exception cref="InvalidOperationException">当启动帮助器尚未初始化成功时抛出。</exception>
    public YamlConfigLoader Loader
    {
        get
        {
            lock (_stateGate)
            {
                ThrowIfDisposedCore();

                return _loader ?? throw new InvalidOperationException(
                    "The config bootstrap has not been initialized yet.");
            }
        }
    }

    /// <summary>
    ///     执行初次配置加载，并在需要时启动开发期热重载。
    ///     该方法只能成功调用一次，避免同一个生命周期对象在运行中被重新拼装为另一套加载约定。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <remarks>
    ///     该入口会被 <see cref="GameConfigModule" /> 的同步生命周期钩子桥接调用，
    ///     因此内部所有异步等待都必须使用 <c>ConfigureAwait(false)</c>，避免在 Unity 主线程、
    ///     UI Dispatcher 或带 <see cref="SynchronizationContext" /> 的测试线程上发生同步阻塞死锁。
    /// </remarks>
    /// <returns>表示异步初始化流程的任务。</returns>
    /// <exception cref="ObjectDisposedException">当当前实例已释放时抛出。</exception>
    /// <exception cref="InvalidOperationException">当当前实例已经初始化成功时抛出。</exception>
    /// <exception cref="ConfigLoadException">当配置加载失败时抛出。</exception>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            ThrowIfDisposedCore();

            if (_isInitializing || _loader != null)
            {
                throw new InvalidOperationException(
                    "The config bootstrap can only be initialized once per instance.");
            }

            _isInitializing = true;
        }

        IUnRegister? hotReload = null;

        try
        {
            var loader = new YamlConfigLoader(RootPath);
            _options.ConfigureLoader!(loader);
            await loader.LoadAsync(Registry, cancellationToken).ConfigureAwait(false);

            if (_options.EnableHotReload)
            {
                hotReload = loader.EnableHotReload(Registry, _options.HotReloadOptions);
            }

            lock (_stateGate)
            {
                try
                {
                    ThrowIfDisposedCore();

                    // 仅在初次加载与可选热重载都完整成功后才提交结果，
                    // 避免 IsInitialized / Loader 暴露半初始化生命周期。
                    _loader = loader;
                    _hotReload = hotReload;
                    hotReload = null;
                }
                finally
                {
                    _isInitializing = false;
                }
            }
        }
        catch
        {
            lock (_stateGate)
            {
                _isInitializing = false;
            }

            hotReload?.UnRegister();
            throw;
        }
    }

    /// <summary>
    ///     启用开发期热重载。
    ///     该入口让调用方可以先完成一次确定性的初始加载，再按环境决定是否追加文件监听。
    /// </summary>
    /// <param name="options">热重载选项；为空时使用 <see cref="YamlConfigLoader" /> 的默认行为。</param>
    /// <exception cref="ObjectDisposedException">当当前实例已释放时抛出。</exception>
    /// <exception cref="InvalidOperationException">
    ///     当初始加载尚未完成，或热重载已经处于启用状态时抛出。
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     当 <paramref name="options" /> 的 <see cref="YamlConfigHotReloadOptions.DebounceDelay" /> 小于
    ///     <see cref="TimeSpan.Zero" /> 时抛出。
    /// </exception>
    public void StartHotReload(YamlConfigHotReloadOptions? options = null)
    {
        var loader = BeginHotReloadStart();
        IUnRegister? hotReload = null;
        try
        {
            hotReload = loader.EnableHotReload(Registry, options);
            hotReload = CompleteHotReloadStart(hotReload);
        }
        catch
        {
            ResetHotReloadStartAfterFailure();
            hotReload?.UnRegister();
            throw;
        }
    }

    /// <summary>
    ///     停止开发期热重载并释放监听资源。
    ///     该方法是幂等的，允许启动层在销毁阶段无条件调用。
    /// </summary>
    public void StopHotReload()
    {
        IUnRegister? hotReload;
        lock (_stateGate)
        {
            if (_isStartingHotReload && _hotReload == null)
            {
                _stopHotReloadAfterStart = true;
                return;
            }

            hotReload = _hotReload;
            _hotReload = null;
        }

        hotReload?.UnRegister();
    }

    /// <summary>
    ///     停止热重载并释放当前帮助器持有的资源。
    /// </summary>
    public void Dispose()
    {
        IUnRegister? hotReload;
        lock (_stateGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_isStartingHotReload && _hotReload == null)
            {
                _stopHotReloadAfterStart = true;
            }

            hotReload = _hotReload;
            _hotReload = null;
        }

        hotReload?.UnRegister();
    }

    private void ThrowIfDisposedCore()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(GameConfigBootstrap));
        }
    }

    private YamlConfigLoader BeginHotReloadStart()
    {
        lock (_stateGate)
        {
            ThrowIfDisposedCore();

            var loader = _loader ?? throw new InvalidOperationException(
                "Hot reload can only be started after the initial config load succeeds.");

            if (_isStartingHotReload || _hotReload != null)
            {
                throw new InvalidOperationException("Hot reload is already enabled.");
            }

            _isStartingHotReload = true;
            _stopHotReloadAfterStart = false;
            return loader;
        }
    }

    private IUnRegister? CompleteHotReloadStart(IUnRegister? hotReload)
    {
        var shouldStop = false;
        lock (_stateGate)
        {
            try
            {
                ThrowIfDisposedCore();

                // Stop/Dispose may arrive while the watcher is being created. In that
                // case, release the new handle immediately instead of publishing it.
                if (_stopHotReloadAfterStart)
                {
                    shouldStop = true;
                    _stopHotReloadAfterStart = false;
                }
                else
                {
                    _hotReload = hotReload;
                    hotReload = null;
                }
            }
            finally
            {
                _isStartingHotReload = false;
            }
        }

        if (shouldStop)
        {
            hotReload?.UnRegister();
            return null;
        }

        return hotReload;
    }

    private void ResetHotReloadStartAfterFailure()
    {
        lock (_stateGate)
        {
            _isStartingHotReload = false;
            _stopHotReloadAfterStart = false;
        }
    }
}
