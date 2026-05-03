// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Architectures;
using GFramework.Core.Constants;

namespace GFramework.Godot.Architectures;

/// <summary>
///     抽象架构类，为特定类型的架构提供基础实现框架。
///     此类负责管理架构的初始化、生命周期绑定以及扩展模块的安装与销毁。
/// </summary>
public abstract class AbstractArchitecture(
    IArchitectureConfiguration? configuration = null,
    IEnvironment? environment = null,
    IArchitectureServices? services = null,
    IArchitectureContext? context = null
) : Architecture(configuration, environment, services, context)
{
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

    /// <summary>
    ///     标记架构是否已被销毁的状态标志
    ///     用于防止架构被重复销毁，确保资源清理只执行一次
    /// </summary>
    private bool _destroyed;

    /// <summary>
    ///     获取架构根节点。如果尚未初始化或已被销毁，则抛出异常。
    /// </summary>
    /// <exception cref="InvalidOperationException">当架构未准备就绪时抛出。</exception>
    protected Node ArchitectureRoot => _anchor ?? throw new InvalidOperationException("Architecture root not ready");


    /// <summary>
    ///     初始化架构，按顺序注册模型、系统和工具。
    ///     包括将架构绑定到Godot生命周期并调用模块安装逻辑。
    /// </summary>
    protected override void OnInitialize()
    {
        _architectureAnchorName =
            $"__{GFrameworkConstants.FrameworkName}__{GetType().Name}__{GetHashCode()}__ArchitectureAnchor__";
        AttachToGodotLifecycle();
        InstallModules();
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
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        // 防止重复挂载（热重载 / 多次 OnInitialize）
        if (tree.Root.GetNodeOrNull(_architectureAnchorName) != null)
            return;

        _anchor = new ArchitectureAnchor
        {
            Name = _architectureAnchorName
        };

        _anchor.Bind(ObserveDestroyAsync);

        tree.Root.CallDeferred(Node.MethodName.AddChild, _anchor);
    }


    /// <summary>
    ///     安装Godot模块扩展
    /// </summary>
    /// <typeparam name="TModule">模块类型，必须实现IGodotModule接口</typeparam>
    /// <param name="module">要安装的模块实例</param>
    /// <returns>异步任务</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="module" /> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">当架构锚点尚未初始化时抛出。</exception>
    /// <remarks>
    ///     该方法会等待锚点进入场景树后再继续执行附加回调，避免模块在非主线程或未就绪状态下访问 Godot 节点 API。
    /// </remarks>
    protected async Task InstallGodotModule<TModule>(TModule module) where TModule : IGodotModule
    {
        ArgumentNullException.ThrowIfNull(module);

        // 先确认锚点可用，避免模块安装产生副作用后再因架构未绑定场景树而失败。
        var anchor = _anchor ?? throw new InvalidOperationException("Anchor not initialized");

        module.Install(this);

        // 在附加流程完成前先登记模块，保证后续任一步失败时仍能参与架构销毁阶段的清理。
        _extensions.Add(module);

        // 显式保留 Godot 同步上下文，确保后续 AddChild 和 OnAttach 仍在节点可访问的主线程执行。
        await anchor.WaitUntilReadyAsync().ConfigureAwait(true);

        // 延迟调用将扩展节点添加为锚点的子节点
        anchor.CallDeferred(Node.MethodName.AddChild, module.Node);

        // 调用扩展的附加回调方法
        module.OnAttach(this);
    }


    /// <summary>
    ///     销毁架构及其相关资源。
    ///     调用所有已安装扩展的OnDetach方法，并清空扩展列表。
    ///     若已被销毁则直接返回。
    /// </summary>
    public override async ValueTask DestroyAsync()
    {
        if (_destroyed)
            return;

        _destroyed = true;

        foreach (var ext in _extensions)
            ext.OnDetach();

        _extensions.Clear();

        await base.DestroyAsync().ConfigureAwait(false);
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
