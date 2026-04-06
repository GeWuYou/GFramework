using GFramework.Core.Abstractions.Events;
using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     提供官方的 C# 配置启动帮助器。
///     该类型负责把配置注册表、YAML 加载器与开发期热重载句柄收敛到一个长生命周期对象中，
///     让消费者项目可以通过一个稳定入口完成配置启动，而不是在多个脚本里重复拼装运行时细节。
/// </summary>
public sealed class GameConfigBootstrap : IDisposable
{
    private const string ConfigureLoaderCannotBeNullMessage = "ConfigureLoader must be provided.";
    private const string RootPathCannotBeNullOrWhiteSpaceMessage = "Root path cannot be null or whitespace.";

    private readonly GameConfigBootstrapOptions _options;
    private IUnRegister? _hotReload;
    private YamlConfigLoader? _loader;
    private bool _disposed;

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
                nameof(options.RootPath));
        }

        if (options.ConfigureLoader == null)
        {
            throw new ArgumentException(
                ConfigureLoaderCannotBeNullMessage,
                nameof(options.ConfigureLoader));
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
    /// </summary>
    public bool IsInitialized => _loader != null;

    /// <summary>
    ///     获取一个值，指示开发期热重载是否已启用。
    /// </summary>
    public bool IsHotReloadEnabled => _hotReload != null;

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
            ThrowIfDisposed();

            return _loader ?? throw new InvalidOperationException(
                "The config bootstrap has not been initialized yet.");
        }
    }

    /// <summary>
    ///     执行初次配置加载，并在需要时启动开发期热重载。
    ///     该方法只能成功调用一次，避免同一个生命周期对象在运行中被重新拼装为另一套加载约定。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步初始化流程的任务。</returns>
    /// <exception cref="ObjectDisposedException">当当前实例已释放时抛出。</exception>
    /// <exception cref="InvalidOperationException">当当前实例已经初始化成功时抛出。</exception>
    /// <exception cref="ConfigLoadException">当配置加载失败时抛出。</exception>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_loader != null)
        {
            throw new InvalidOperationException(
                "The config bootstrap can only be initialized once per instance.");
        }

        var loader = new YamlConfigLoader(RootPath);
        _options.ConfigureLoader!(loader);
        await loader.LoadAsync(Registry, cancellationToken);

        // 仅在初次加载完全成功后才公开加载器实例，避免上层观察到半初始化状态。
        _loader = loader;

        if (_options.EnableHotReload)
        {
            StartHotReload(_options.HotReloadOptions);
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
        ThrowIfDisposed();

        var loader = _loader ?? throw new InvalidOperationException(
            "Hot reload can only be started after the initial config load succeeds.");

        if (_hotReload != null)
        {
            throw new InvalidOperationException("Hot reload is already enabled.");
        }

        _hotReload = loader.EnableHotReload(Registry, options);
    }

    /// <summary>
    ///     停止开发期热重载并释放监听资源。
    ///     该方法是幂等的，允许启动层在销毁阶段无条件调用。
    /// </summary>
    public void StopHotReload()
    {
        var hotReload = _hotReload;
        _hotReload = null;
        hotReload?.UnRegister();
    }

    /// <summary>
    ///     停止热重载并释放当前帮助器持有的资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopHotReload();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(GameConfigBootstrap));
        }
    }
}
