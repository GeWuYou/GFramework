using System.ComponentModel;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
///     架构接口，专注于生命周期管理，包括系统、模型、工具的注册和获取
///     业务操作通过 ArchitectureRuntime 提供
/// </summary>
public interface IArchitecture : IAsyncInitializable, IAsyncDestroyable, IInitializable, IDestroyable
{
    /// <summary>
    ///     获取架构上下文
    /// </summary>
    IArchitectureContext Context { get; }

    /// <summary>
    ///     获取或设置用于配置服务集合的委托
    /// </summary>
    /// <value>
    ///     一个可为空的委托，用于配置IServiceCollection实例
    /// </value>
    Action<IServiceCollection>? Configurator { get; }

    /// <summary>
    ///     注册系统实例到架构中
    /// </summary>
    /// <typeparam name="T">系统类型，必须实现ISystem接口</typeparam>
    /// <param name="system">要注册的系统实例</param>
    /// <returns>注册的系统实例</returns>
    T RegisterSystem<T>(T system) where T : ISystem;

    /// <summary>
    ///     注册系统实例到架构中
    /// </summary>
    /// <typeparam name="T">系统类型，必须实现ISystem接口</typeparam>
    /// <param name="onCreated">系统实例创建后的回调函数，可为null</param>
    void RegisterSystem<T>(Action<T>? onCreated = null) where T : class, ISystem;

    /// <summary>
    ///     注册模型实例到架构中
    /// </summary>
    /// <typeparam name="T">模型类型，必须实现IModel接口</typeparam>
    /// <param name="model">要注册的模型实例</param>
    /// <returns>注册的模型实例</returns>
    T RegisterModel<T>(T model) where T : IModel;

    /// <summary>
    ///     注册模型实例到架构中
    /// </summary>
    /// <typeparam name="T">模型类型，必须实现IModel接口</typeparam>
    /// <param name="onCreated">模型实例创建后的回调函数，可为null</param>
    void RegisterModel<T>(Action<T>? onCreated = null) where T : class, IModel;

    /// <summary>
    ///     注册工具实例到架构中
    /// </summary>
    /// <typeparam name="T">工具类型，必须实现IUtility接口</typeparam>
    /// <param name="utility">要注册的工具实例</param>
    /// <returns>注册的工具实例</returns>
    T RegisterUtility<T>(T utility) where T : IUtility;


    /// <summary>
    ///     注册工具类型并可选地指定创建回调
    ///     当工具实例被创建时会调用指定的回调函数
    /// </summary>
    /// <typeparam name="T">工具类型，必须是引用类型且实现IUtility接口</typeparam>
    /// <param name="onCreated">工具实例创建后的回调函数，可为null</param>
    void RegisterUtility<T>(Action<T>? onCreated = null) where T : class, IUtility;

    /// <summary>
    ///     注册 CQRS 请求管道行为。
    ///     既支持实现 <c>IPipelineBehavior&lt;,&gt;</c> 的开放泛型行为类型，
    ///     也支持绑定到单一请求/响应对的封闭行为类型。
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型</typeparam>
    void RegisterCqrsPipelineBehavior<TBehavior>()
        where TBehavior : class;

    /// <summary>
    ///     注册 CQRS 请求管道行为。
    ///     该成员保留旧名称以兼容历史调用点，内部行为与 <see cref="RegisterCqrsPipelineBehavior{TBehavior}" /> 一致。
    ///     新代码不应继续依赖该别名；兼容层计划在未来的 major 版本中移除。
    ///     既支持实现 <c>IPipelineBehavior&lt;,&gt;</c> 的开放泛型行为类型，
    ///     也支持绑定到单一请求/响应对的封闭行为类型。
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Use RegisterCqrsPipelineBehavior<TBehavior>() instead. This compatibility alias will be removed in a future major version.")]
    void RegisterMediatorBehavior<TBehavior>()
        where TBehavior : class;

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
    IArchitectureLifecycleHook RegisterLifecycleHook(IArchitectureLifecycleHook hook);

    /// <summary>
    ///     等待直到架构准备就绪的异步操作
    /// </summary>
    /// <returns>表示异步等待操作的任务</returns>
    Task WaitUntilReadyAsync();
}
