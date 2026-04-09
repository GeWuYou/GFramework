using System;
using System.Threading;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Architectures;
using GFramework.Core.Utility;
using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     提供基于 <see cref="Architecture" /> 的官方配置模块接入入口。
///     该模块负责把 <see cref="GameConfigBootstrap" /> 挂接到架构生命周期中，统一完成注册表暴露、
///     首次加载以及架构销毁时的资源回收。
/// </summary>
/// <remarks>
///     使用该模块时，推荐在 <c>Architecture.OnInitialize()</c> 的较早位置调用 <see cref="IArchitecture.InstallModule" />，
///     以便其他 utility、model 和 system 在各自初始化阶段都能读取到已经完成首次加载的配置表。
///     如果消费项目不基于 <see cref="Architecture" />，则继续直接使用 <see cref="GameConfigBootstrap" /> 更合适。
/// </remarks>
public sealed class GameConfigModule : IArchitectureModule
{
    private const int InstallStateNotInstalled = 0;
    private const int InstallStateInstalling = 1;
    private const int InstallStateConsumed = 2;

    private readonly GameConfigBootstrap _bootstrap;
    private readonly ModuleBootstrapLifetimeUtility _lifetimeUtility;
    private int _installState;

    /// <summary>
    ///     使用指定的启动选项创建配置模块。
    /// </summary>
    /// <param name="options">配置启动帮助器选项。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="options" /> 为空时抛出。</exception>
    /// <exception cref="ArgumentException">
    ///     当 <paramref name="options" /> 不满足 <see cref="GameConfigBootstrap" /> 的构造约束时抛出。
    /// </exception>
    public GameConfigModule(GameConfigBootstrapOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _bootstrap = new GameConfigBootstrap(options);
        _lifetimeUtility = new ModuleBootstrapLifetimeUtility(_bootstrap);
    }

    /// <summary>
    ///     获取当前模块复用的配置注册表。
    ///     该实例会在模块安装时注册为架构 utility，供其他组件通过上下文直接读取。
    /// </summary>
    public IConfigRegistry Registry => _bootstrap.Registry;

    /// <summary>
    ///     获取一个值，指示模块绑定的配置启动器是否已经完成首次加载。
    /// </summary>
    public bool IsInitialized => _bootstrap.IsInitialized;

    /// <summary>
    ///     获取一个值，指示模块绑定的开发期热重载是否已启用。
    /// </summary>
    public bool IsHotReloadEnabled => _bootstrap.IsHotReloadEnabled;

    /// <summary>
    ///     获取当前生效的 YAML 配置加载器。
    ///     只有在模块完成首次加载后该属性才可访问。
    /// </summary>
    /// <exception cref="ObjectDisposedException">当模块所属架构已销毁时抛出。</exception>
    /// <exception cref="InvalidOperationException">当首次加载尚未成功完成时抛出。</exception>
    public YamlConfigLoader Loader => _bootstrap.Loader;

    /// <summary>
    ///     将配置模块安装到指定架构中。
    ///     安装后会立即暴露 <see cref="Registry" />，并注册一个生命周期钩子，
    ///     以便在 utility 初始化之前完成一次确定性的配置加载。
    /// </summary>
    /// <param name="architecture">目标架构实例。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="architecture" /> 为空时抛出。</exception>
    /// <exception cref="InvalidOperationException">当同一个模块实例被重复安装时抛出。</exception>
    /// <remarks>
    ///     生命周期阶段校验会在任何注册动作前执行，因此错过安装窗口的调用不会消耗当前模块实例。
    ///     一旦开始向架构注册 utility 或生命周期钩子，就不存在回滚 API；因此后续任何失败都会把该模块实例视为已消耗，
    ///     调用方必须创建新的 <see cref="GameConfigModule" /> 再重试。
    /// </remarks>
    public void Install(IArchitecture architecture)
    {
        ArgumentNullException.ThrowIfNull(architecture);

        ValidateInstallationPhase(architecture);

        if (Interlocked.CompareExchange(
                ref _installState,
                InstallStateInstalling,
                InstallStateNotInstalled) != InstallStateNotInstalled)
        {
            throw new InvalidOperationException(
                "The same GameConfigModule instance cannot be installed more than once.");
        }

        try
        {
            // 阶段窗口已经在前面做过无副作用校验，因此这里优先注册 utility，
            // 让常见的容器/上下文接线失败在不可回滚的 hook 注册之前暴露出来。
            architecture.RegisterUtility(Registry);
            architecture.RegisterUtility(_lifetimeUtility);
            architecture.RegisterLifecycleHook(new BootstrapInitializationHook(_bootstrap));
            Volatile.Write(ref _installState, InstallStateConsumed);
        }
        catch
        {
            // 架构对 utility / hook 注册都不提供回滚入口，因此一旦进入注册阶段，
            // 即使安装失败也必须禁止复用同一个模块实例，避免重复暴露共享注册表或挂上第二个 hook。
            Volatile.Write(ref _installState, InstallStateConsumed);
            throw;
        }
    }

    /// <summary>
    ///     在首次加载成功后显式启用开发期热重载。
    ///     该入口与 <see cref="GameConfigBootstrap.StartHotReload" /> 的语义保持一致，
    ///     供已经保留模块实例引用的架构启动层按环境决定是否追加监听。
    /// </summary>
    /// <param name="options">热重载选项；为空时使用默认行为。</param>
    public void StartHotReload(YamlConfigHotReloadOptions? options = null)
    {
        _bootstrap.StartHotReload(options);
    }

    /// <summary>
    ///     停止开发期热重载并释放监听资源。
    ///     该方法是幂等的，允许架构外部的开发期开关无条件调用。
    /// </summary>
    public void StopHotReload()
    {
        _bootstrap.StopHotReload();
    }

    /// <summary>
    ///     在 utility 初始化之前完成首次配置加载的生命周期钩子。
    ///     这样后续 utility、model 和 system 在各自初始化阶段就能直接依赖已加载的注册表。
    /// </summary>
    private sealed class BootstrapInitializationHook(GameConfigBootstrap bootstrap) : IArchitectureLifecycleHook
    {
        /// <summary>
        ///     在目标阶段触发配置加载。
        /// </summary>
        /// <param name="phase">当前架构阶段。</param>
        /// <param name="architecture">相关架构实例；当前实现不直接使用，但保留用于接口契约一致性。</param>
        public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
        {
            if (phase != ArchitecturePhase.BeforeUtilityInit)
            {
                return;
            }

            // 架构生命周期钩子当前是同步接口，因此这里显式桥接到统一的 bootstrap 异步实现，
            // 让 Architecture 模式和独立运行时模式保持同一套加载、诊断和热重载启动语义。
            bootstrap.InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    ///     验证模块仍处于允许接入的生命周期窗口。
    ///     该模块依赖 <see cref="ArchitecturePhase.BeforeUtilityInit" /> 钩子完成首次加载，
    ///     因此一旦架构已经离开 <see cref="ArchitecturePhase.None" />，继续安装只会错过首载时机。
    /// </summary>
    /// <param name="architecture">目标架构实例。</param>
    /// <exception cref="InvalidOperationException">
    ///     当目标架构已经开始组件初始化阶段时抛出。
    /// </exception>
    private static void ValidateInstallationPhase(IArchitecture architecture)
    {
        if (architecture is not Architecture concreteArchitecture)
        {
            return;
        }

        if (concreteArchitecture.CurrentPhase != ArchitecturePhase.None)
        {
            throw new InvalidOperationException(
                "GameConfigModule must be installed before the architecture enters BeforeUtilityInit.");
        }
    }

    /// <summary>
    ///     跟随架构 utility 生命周期释放底层 bootstrap 资源的薄封装。
    ///     该 utility 本身不承担加载职责，只负责在架构销毁时停止热重载并释放监听句柄。
    /// </summary>
    private sealed class ModuleBootstrapLifetimeUtility(GameConfigBootstrap bootstrap) : AbstractContextUtility
    {
        /// <summary>
        ///     该 utility 不需要在初始化阶段执行额外逻辑。
        ///     首次加载已经由 <see cref="BootstrapInitializationHook" /> 在 utility 初始化前完成。
        /// </summary>
        protected override void OnInit()
        {
        }

        /// <summary>
        ///     架构销毁时释放 bootstrap 资源，确保热重载监听句柄不会泄漏到架构生命周期之外。
        /// </summary>
        protected override void OnDestroy()
        {
            bootstrap.Dispose();
        }
    }
}
