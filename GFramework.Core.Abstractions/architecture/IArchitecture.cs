using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;

namespace GFramework.Core.Abstractions.architecture;

/// <summary>
///     架构接口，专注于生命周期管理，包括系统、模型、工具的注册和获取
///     业务操作通过 ArchitectureRuntime 提供
/// </summary>
public interface IArchitecture : IAsyncInitializable
{
    /// <summary>
    ///     获取架构上下文
    /// </summary>
    IArchitectureContext Context { get; }

    /// <summary>
    ///     初始化方法，用于执行对象的初始化操作
    /// </summary>
    /// <remarks>
    ///     该方法通常用于设置初始状态、初始化成员变量或执行必要的预处理操作
    /// </remarks>
    void Initialize();

    /// <summary>
    ///     销毁方法，用于执行对象的清理和销毁操作
    /// </summary>
    /// <remarks>
    ///     该方法通常用于释放资源、清理内存或执行必要的清理操作
    /// </remarks>
    void Destroy();

    /// <summary>
    ///     注册系统实例到架构中
    /// </summary>
    /// <typeparam name="T">系统类型，必须实现ISystem接口</typeparam>
    /// <param name="system">要注册的系统实例</param>
    /// <returns>注册的系统实例</returns>
    T RegisterSystem<T>(T system) where T : ISystem;

    /// <summary>
    ///     注册模型实例到架构中
    /// </summary>
    /// <typeparam name="T">模型类型，必须实现IModel接口</typeparam>
    /// <param name="model">要注册的模型实例</param>
    /// <returns>注册的模型实例</returns>
    T RegisterModel<T>(T model) where T : IModel;

    /// <summary>
    ///     注册工具实例到架构中
    /// </summary>
    /// <typeparam name="T">工具类型，必须实现IUtility接口</typeparam>
    /// <param name="utility">要注册的工具实例</param>
    /// <returns>注册的工具实例</returns>
    T RegisterUtility<T>(T utility) where T : IUtility;

    /// <summary>
    ///     安装架构模块
    /// </summary>
    /// <param name="module">要安装的模块</param>
    /// <returns>安装的模块实例</returns>
    IArchitectureModule InstallModule(IArchitectureModule module);

    /// <summary>
    ///     注册生命周期钩子
    /// </summary>
    /// <param name="hook">生命周期钩子实例</param>
    /// <returns>注册的钩子实例</returns>
    IArchitectureLifecycle RegisterLifecycleHook(IArchitectureLifecycle hook);

    /// <summary>
    ///     等待直到架构准备就绪的异步操作
    /// </summary>
    /// <returns>表示异步等待操作的任务</returns>
    Task WaitUntilReadyAsync();
}