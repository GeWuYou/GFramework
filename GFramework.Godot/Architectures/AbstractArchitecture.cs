// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Architectures;
using GFramework.Core.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Godot.Architectures;

/// <summary>
///     抽象架构类，为特定类型的架构提供基础实现框架。
///     此类负责管理架构的初始化、生命周期绑定以及扩展模块的安装与销毁。
/// </summary>
public abstract class AbstractArchitecture : Architecture
{
    /// <summary>
    ///     初始化新的 Godot 架构，并为未显式提供上下文的场景自动启用 Godot 主线程同步 CQRS 保护。
    /// </summary>
    /// <param name="configuration">可选的架构配置；当为 <see langword="null" /> 时沿用基类默认配置解析流程。</param>
    /// <param name="environment">可选的运行环境实例；当为 <see langword="null" /> 时由基类按默认规则解析。</param>
    /// <param name="services">可选的服务容器包装；若未提供，则创建新的 <see cref="ArchitectureServices" />。</param>
    /// <param name="context">
    ///     可选的架构上下文；若未提供，则自动包装为 <see cref="GodotArchitectureContext" />，
    ///     以确保默认同步 CQRS 调用在 Godot 主线程上受到保护。
    /// </param>
    /// <remarks>
    ///     该构造函数会让默认服务实例与默认上下文共享同一容器，避免模块安装阶段出现服务注册与解析容器不一致的问题。
    /// </remarks>
    protected AbstractArchitecture(
        IArchitectureConfiguration? configuration = null,
        IEnvironment? environment = null,
        IArchitectureServices? services = null,
        IArchitectureContext? context = null)
        : this(ResolveConstructorArgs(configuration, environment, services, context))
    {
    }

    /// <summary>
    ///     存储所有已安装的Godot架构扩展组件列表
    ///     用于在架构销毁时正确清理所有扩展资源
    /// </summary>
    private readonly List<IGodotModule> _extensions = [];

    /// <summary>
    ///     架构锚点节点引用
    ///     用于将架构绑定到Godot生命周期并作为扩展节点的父节点
    /// </summary>
    private ArchitectureAnchor? _anchor;

    /// <summary>
    ///     架构锚点节点的唯一标识名称
    ///     用于在Godot场景树中创建和查找架构锚点节点
    /// </summary>
    private string _architectureAnchorName = null!;

    private readonly List<IGodotModule> _attachedExtensions = [];
    private readonly GodotMainThreadDispatcher _dispatcher = new(
        GodotThread.IsMainThread,
        action => Callable.From(action).CallDeferred());

    /// <summary>
    ///     使用已解析的构造参数继续初始化基类。
    /// </summary>
    private AbstractArchitecture(ResolvedConstructorArgs args)
        : base(args.Configuration, args.Environment, args.Services, args.Context)
    {
    }

    /// <summary>
    ///     获取架构根节点。如果尚未初始化或已被销毁，则抛出异常。
    /// </summary>
    /// <exception cref="InvalidOperationException">当架构未准备就绪时抛出。</exception>
    protected Node ArchitectureRoot => _anchor ?? throw new InvalidOperationException("Architecture root not ready");

    /// <summary>
    ///     统一解析 Godot 架构构造参数，确保服务实例与默认上下文共享同一容器。
    /// </summary>
    private static ResolvedConstructorArgs ResolveConstructorArgs(
        IArchitectureConfiguration? configuration,
        IEnvironment? environment,
        IArchitectureServices? services,
        IArchitectureContext? context)
    {
        var resolvedServices = services ?? new ArchitectureServices();
        var resolvedContext = context ?? new GodotArchitectureContext(new ArchitectureContext(resolvedServices.Container));
        return new ResolvedConstructorArgs(configuration, environment, resolvedServices, resolvedContext);
    }

    /// <summary>
    ///     承载构造期间已解析的服务与上下文。
    /// </summary>
    private sealed record ResolvedConstructorArgs(
        IArchitectureConfiguration? Configuration,
        IEnvironment? Environment,
        IArchitectureServices Services,
        IArchitectureContext Context);


    /// <summary>
    ///     只声明模块和组件，不访问场景树；正式 provider 由核心随后构建。
    /// </summary>
    /// <param name="services">当前实例的标准服务集合。</param>
    protected override void ConfigureServices(IServiceCollection services)
    {
        InstallModules();
    }

    /// <summary>
    ///     将所有节点操作显式派发回 Godot 主线程，并等待注册模块完成附加后才允许 Ready。
    /// </summary>
    /// <param name="cancellationToken">架构启动取消令牌。</param>
    /// <returns>宿主锚点与模块附加完成的值任务。</returns>
    protected override async ValueTask OnStartedAsync(CancellationToken cancellationToken)
    {
        await _dispatcher.InvokeAsync(AttachToGodotLifecycle, cancellationToken).ConfigureAwait(false);
        var anchor = _anchor;
        if (anchor is null)
        {
            if (_extensions.Count != 0)
                throw new InvalidOperationException("Godot modules require an active SceneTree.");

            await InvokeInheritedStartupAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        Task anchorReady = Task.CompletedTask;
        await _dispatcher.InvokeAsync(
            () => anchorReady = anchor.WaitUntilReadyAsync(),
            cancellationToken).ConfigureAwait(false);
        await anchorReady.WaitAsync(cancellationToken).ConfigureAwait(false);

        foreach (var module in _extensions)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                anchor.AddChild(module.Node);
                // OnAttach 失败后同样必须执行 OnDetach，所以先登记清理责任。
                _attachedExtensions.Add(module);
                module.OnAttach(this);
            }, cancellationToken).ConfigureAwait(false);
        }
        await InvokeInheritedStartupAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs the inherited startup hook on the Godot thread after all host attachments exist.</summary>
    /// <param name="cancellationToken">The startup lifetime.</param>
    /// <returns>Completion of the inherited host setup.</returns>
    private async ValueTask InvokeInheritedStartupAsync(CancellationToken cancellationToken)
    {
        var started = ValueTask.CompletedTask;
        await _dispatcher.InvokeAsync(() => started = base.OnStartedAsync(cancellationToken), cancellationToken)
            .ConfigureAwait(false);
        await started.ConfigureAwait(false);
    }

    /// <summary>
    ///     安装模块抽象方法，由子类实现具体的模块注册逻辑。
    ///     子类应在此方法中完成所有模型、系统及工具的注册工作。
    /// </summary>
    protected abstract void InstallModules();

    /// <summary>
    ///     将架构绑定到Godot生命周期中，确保在场景树销毁时能够正确清理资源。
    ///     通过创建一个锚节点来监听场景树的销毁事件。
    /// </summary>
    private void AttachToGodotLifecycle()
    {
        _architectureAnchorName =
            string.Create(CultureInfo.InvariantCulture, $"__{GFrameworkConstants.FrameworkName}__{GetType().Name}__{GetHashCode()}__ArchitectureAnchor__");
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        // 当前实例只创建一个锚点；同名节点不会被另一个初始化流程复用。
        if (tree.Root.GetNodeOrNull(_architectureAnchorName) != null)
            return;

        _anchor = new ArchitectureAnchor
        {
            Name = _architectureAnchorName,
        };

        _anchor.Bind(ObserveDestroyAsync);

        tree.Root.CallDeferred(Node.MethodName.AddChild, _anchor);
    }


    /// <summary>
    ///     在配置期声明 Godot 模块，模块只在异步启动阶段访问节点并附加到场景树。
    /// </summary>
    /// <typeparam name="TModule">模块类型。</typeparam>
    /// <param name="module">属于当前架构的模块实例。</param>
    /// <exception cref="InvalidOperationException">当前架构已经离开配置阶段。</exception>
    protected void InstallGodotModule<TModule>(TModule module) where TModule : IGodotModule
    {
        ArgumentNullException.ThrowIfNull(module);
        InstallModule(module);
        _extensions.Add(module);
    }

    /// <summary>
    ///     核心已等待组件和服务模块停止后，才分离本实例宿主节点。
    ///     异步场景退出和 UI 清理期间，模块根节点及锚点仍然存活；分离和并发销毁由核心唯一清理任务负责。
    /// </summary>
    /// <returns>宿主分离完成的值任务。</returns>
    protected override ValueTask OnStoppedAsync()
    {
        return new ValueTask(CleanupHostAsync());
    }

    /// <summary>
    ///     仅在主线程分离已经附加的模块，并释放本实例拥有的锚点。
    /// </summary>
    /// <returns>宿主对象清理任务。</returns>
    private Task CleanupHostAsync()
    {
        if (_anchor is null && _attachedExtensions.Count == 0)
            return Task.CompletedTask;

        return _dispatcher.InvokeAsync(() =>
        {
            List<Exception>? errors = null;
            for (var i = _attachedExtensions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _attachedExtensions[i].OnDetach();
                }
                catch (Exception exception)
                {
                    (errors ??= []).Add(exception);
                }
            }

            _attachedExtensions.Clear();
            _extensions.Clear();
            var anchor = _anchor;
            _anchor = null;
            if (GodotObject.IsInstanceValid(anchor) && !anchor!.IsQueuedForDeletion())
                anchor.QueueFree();

            if (errors is not null)
                throw new AggregateException("One or more Godot modules failed to detach.", errors);
        });
    }

    /// <summary>
    ///     观察架构异步销毁流程，确保退出树时触发的 fire-and-forget 清理失败可见。
    /// </summary>
    /// <remarks>
    ///     Godot 的 <see cref="Node._ExitTree" /> 回调是同步入口，无法直接等待异步销毁完成；
    ///     因此这里显式附加错误观察器，把异常写入 Godot 错误输出，避免未观测任务异常被静默吞掉。
    /// </remarks>
    private void ObserveDestroyAsync()
    {
        _ = ObserveDestroyCoreAsync();
    }

    /// <summary>
    ///     执行并观察异步销毁流程。
    /// </summary>
    /// <returns>表示观察任务本身完成的任务。</returns>
    private async Task ObserveDestroyCoreAsync()
    {
        try
        {
            await DestroyAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            GD.PushError($"Architecture destruction failed: {ex}");
        }
    }
}
