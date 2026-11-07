// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GFramework.Game.Abstractions.Scene;
using GFramework.Godot.Extensions;
using Godot;

namespace GFramework.Godot.Scene;

/// <summary>
///     场景行为基类，封装通用的场景生命周期管理逻辑。
///     提供对 Node 类型场景节点的统一管理，包括加载、进入、暂停、恢复、退出、卸载等操作。
/// </summary>
/// <typeparam name="T">Node 类型的场景节点。</typeparam>
public abstract class SceneBehaviorBase<T> : ISceneBehavior
    where T : Node
{
    /// <summary>
    ///     场景的唯一标识键。
    /// </summary>
    private readonly string _key;

    /// <summary>
    ///     IScene 接口引用（如果节点实现了该接口）。
    /// </summary>
    private readonly IScene? _scene;

    /// <summary>
    ///     场景节点的所有者实例。
    /// </summary>
    protected readonly T Owner;

    /// <summary>
    ///     场景是否处于活跃状态。
    /// </summary>
    private bool _isActive;

    /// <summary>
    ///     场景是否已加载。
    /// </summary>
    private bool _isLoaded;

    /// <summary>
    ///     场景是否正在过渡中。
    /// </summary>
    private bool _isTransitioning;

    // Retain the first pre-pause state through repeated calls and callback failures.
    // Only successful restoration consumes the snapshot, allowing compensating resume.
    private (bool Process, bool Physics, bool Input)? _pausedProcessing;

    /// <summary>
    ///     初始化 SceneBehaviorBase 实例。
    /// </summary>
    /// <param name="owner">场景节点的所有者实例。</param>
    /// <param name="key">场景的唯一标识键。</param>
    protected SceneBehaviorBase(T owner, string key)
    {
        Owner = owner;
        _key = key;
        _scene = owner as IScene;
    }

    #region ISceneBehavior 实现

    /// <summary>
    ///     获取场景的唯一标识键。
    ///     该属性返回场景的唯一标识符，用于区分不同的场景实例。
    /// </summary>
    public string Key => _key;

    /// <summary>
    ///     获取场景的原始数据对象。
    ///     该属性返回场景的底层数据对象，通常用于序列化或反序列化操作。
    /// </summary>
    public object Original => Owner;


    /// <summary>
    ///     获取场景是否已加载完成的状态。
    /// </summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>
    ///     获取场景是否处于活跃运行状态。
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    ///     获取场景是否正在进行切换操作。
    /// </summary>
    public bool IsTransitioning => _isTransitioning;

    /// <summary>
    ///     获取场景节点是否有效。
    /// </summary>
    public bool IsAlive => Owner.IsValidNode();

    /// <summary>
    ///     异步加载场景所需资源。
    ///     在场景正式进入前调用，负责预加载场景所需的各类资源。
    /// </summary>
    /// <param name="param">场景进入参数，可能包含初始化数据或上下文信息。</param>
    /// <param name="cancellationToken">Cancellation of host preparation; cleanup receives an uncancelled token.</param>
    /// <returns>表示加载操作完成的ValueTask。</returns>
    public virtual async ValueTask OnLoadAsync(ISceneEnterParam? param, CancellationToken cancellationToken = default)
    {
        _isTransitioning = true;

        // 调用可选接口
        if (_scene != null)
            await _scene.OnLoadAsync(param, cancellationToken).ConfigureAwait(true);

        _isLoaded = true;
        _isTransitioning = false;
    }

    /// <summary>
    ///     异步处理场景正式进入逻辑。
    ///     在资源加载完成后调用，启动场景的主要运行逻辑。
    /// </summary>
    /// <param name="cancellationToken">Cancellation of host preparation; cleanup receives an uncancelled token.</param>
    /// <returns>表示进入操作完成的ValueTask。</returns>
    public virtual async ValueTask OnEnterAsync(CancellationToken cancellationToken = default)
    {
        _isTransitioning = true;

        if (_scene != null)
            await _scene.OnEnterAsync(cancellationToken).ConfigureAwait(true);

        _isActive = true;
        _isTransitioning = false;
    }

    /// <summary>
    ///     异步处理场景暂停逻辑。
    ///     当场景被其他场景覆盖或失去焦点时调用。
    ///     首次暂停保存节点原有的处理开关；重复暂停及业务回调失败保留同一快照。
    /// </summary>
    /// <param name="cancellationToken">Cancellation of host preparation; cleanup receives an uncancelled token.</param>
    /// <returns>表示暂停操作完成的ValueTask。</returns>
    public virtual async ValueTask OnPauseAsync(CancellationToken cancellationToken = default)
    {
        _pausedProcessing ??= (Owner.IsProcessing(), Owner.IsPhysicsProcessing(), Owner.IsProcessingInput());

        if (_scene != null)
            // 暂停后紧接着会修改 Owner 的处理开关，必须回到 Godot 主线程继续执行。
            await _scene.OnPauseAsync(cancellationToken).ConfigureAwait(true);

        // 暂停处理
        Owner.SetProcess(false);
        Owner.SetPhysicsProcess(false);
        Owner.SetProcessInput(false);

        _isActive = false;
    }

    /// <summary>
    ///     异步处理场景恢复逻辑。
    ///     当场景重新获得焦点或从暂停状态恢复时调用。
    ///     业务恢复成功后还原暂停前的处理开关；没有快照时保持当前原生设置。
    /// </summary>
    /// <param name="cancellationToken">Cancellation of host preparation; cleanup receives an uncancelled token.</param>
    /// <returns>表示恢复操作完成的ValueTask。</returns>
    public virtual async ValueTask OnResumeAsync(CancellationToken cancellationToken = default)
    {
        if (Owner.IsInvalidNode())
            return;

        if (_scene != null)
            // Keep processing paused until business resume succeeds on the native host context.
            await _scene.OnResumeAsync(cancellationToken).ConfigureAwait(true);

        if (_pausedProcessing is { } processing)
        {
            Owner.SetProcess(processing.Process);
            Owner.SetPhysicsProcess(processing.Physics);
            Owner.SetProcessInput(processing.Input);
            _pausedProcessing = null;
        }

        _isActive = true;
    }

    /// <summary>
    ///     异步处理场景退出逻辑。
    ///     在场景即将被替换或关闭时调用，执行清理工作。
    /// </summary>
    /// <param name="cancellationToken">Cancellation of host preparation; cleanup receives an uncancelled token.</param>
    /// <returns>表示退出操作完成的ValueTask。</returns>
    public virtual async ValueTask OnExitAsync(CancellationToken cancellationToken = default)
    {
        _isTransitioning = true;

        if (_scene != null)
            await _scene.OnExitAsync(cancellationToken).ConfigureAwait(true);

        _isActive = false;
    }

    /// <summary>
    ///     异步卸载场景资源。
    ///     在场景完全退出后调用，释放占用的内存和资源。
    /// </summary>
    /// <param name="cancellationToken">Cancellation of host preparation; cleanup receives an uncancelled token.</param>
    /// <returns>表示卸载操作完成的ValueTask。</returns>
    public virtual async ValueTask OnUnloadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_scene != null)
                await _scene.OnUnloadAsync(cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            // A failed user unload cannot orphan a detached native node; stay on the host context.
            _isLoaded = false;
            _isActive = false;
            _isTransitioning = false;
            _pausedProcessing = null;
            Owner.QueueFreeX();
        }
    }

    #endregion
}
