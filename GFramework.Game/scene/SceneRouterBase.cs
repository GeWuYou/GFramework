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

using GFramework.Core.Abstractions.logging;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.system;
using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.scene;

namespace GFramework.Game.scene;

/// <summary>
/// 场景路由基类，提供场景切换和卸载的基础功能。
/// 实现了 <see cref="ISceneRouter"/> 接口，用于管理场景的加载、替换和卸载操作。
/// </summary>
public abstract class SceneRouterBase
    : AbstractSystem, ISceneRouter
{
    private static readonly ILogger Log =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(SceneRouterBase));

    private readonly List<ISceneRouteGuard> _guards = new();
    private readonly SceneTransitionPipeline _pipeline = new();

    private readonly Stack<ISceneBehavior> _stack = new();
    private readonly SemaphoreSlim _transitionLock = new(1, 1);
    private ISceneFactory _factory = null!;

    /// <summary>
    ///  场景根节点
    /// </summary>
    protected ISceneRoot? Root;

    /// <summary>
    /// 获取当前场景行为对象。
    /// </summary>
    public ISceneBehavior? Current => _stack.Count > 0 ? _stack.Peek() : null;

    /// <summary>
    /// 获取当前场景的键名。
    /// </summary>
    public string? CurrentKey => Current?.Key;

    /// <summary>
    /// 获取场景栈的只读视图，按压入顺序排列（从栈底到栈顶）。
    /// </summary>
    public IEnumerable<ISceneBehavior> Stack => _stack.Reverse();

    /// <summary>
    /// 获取是否正在进行场景转换。
    /// </summary>
    public bool IsTransitioning { get; private set; }

    /// <summary>
    /// 绑定场景根节点。
    /// </summary>
    /// <param name="root">场景根节点实例。</param>
    public void BindRoot(ISceneRoot root)
    {
        Root = root;
        Log.Debug("Bind Scene Root: {0}", root.GetType().Name);
    }

    #region Replace

    /// <summary>
    /// 替换当前场景为指定场景。
    /// </summary>
    /// <param name="sceneKey">目标场景键名。</param>
    /// <param name="param">场景进入参数。</param>
    /// <returns>异步任务。</returns>
    public async ValueTask ReplaceAsync(
        string sceneKey,
        ISceneEnterParam? param = null)
    {
        await _transitionLock.WaitAsync();
        try
        {
            IsTransitioning = true;

            var @event = CreateEvent(sceneKey, SceneTransitionType.Replace, param);

            await _pipeline.ExecuteAroundAsync(@event, async () =>
            {
                await BeforeChangeAsync(@event);
                await ClearInternalAsync();
                await PushInternalAsync(sceneKey, param);
                AfterChange(@event);
            });
        }
        finally
        {
            IsTransitioning = false;
            _transitionLock.Release();
        }
    }

    #endregion

    #region Query

    /// <summary>
    /// 检查指定场景是否在栈中。
    /// </summary>
    /// <param name="sceneKey">场景键名。</param>
    /// <returns>如果场景在栈中返回true，否则返回false。</returns>
    public bool Contains(string sceneKey)
    {
        return _stack.Any(s => s.Key == sceneKey);
    }

    #endregion

    /// <summary>
    /// 注册场景过渡处理器。
    /// </summary>
    /// <param name="handler">处理器实例。</param>
    /// <param name="options">执行选项。</param>
    public void RegisterHandler(ISceneTransitionHandler handler, SceneTransitionHandlerOptions? options = null)
    {
        _pipeline.RegisterHandler(handler, options);
    }

    /// <summary>
    /// 注销场景过渡处理器。
    /// </summary>
    /// <param name="handler">处理器实例。</param>
    public void UnregisterHandler(ISceneTransitionHandler handler)
    {
        _pipeline.UnregisterHandler(handler);
    }

    /// <summary>
    /// 添加场景路由守卫。
    /// </summary>
    /// <param name="guard">守卫实例。</param>
    public void AddGuard(ISceneRouteGuard guard)
    {
        ArgumentNullException.ThrowIfNull(guard);
        if (!_guards.Contains(guard))
        {
            _guards.Add(guard);
            _guards.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            Log.Debug("Guard added: {0}, Priority={1}", guard.GetType().Name, guard.Priority);
        }
    }

    /// <summary>
    /// 添加场景路由守卫（泛型版本）。
    /// </summary>
    /// <typeparam name="T">守卫类型。</typeparam>
    public void AddGuard<T>() where T : ISceneRouteGuard, new()
    {
        AddGuard(new T());
    }

    /// <summary>
    /// 移除场景路由守卫。
    /// </summary>
    /// <param name="guard">守卫实例。</param>
    public void RemoveGuard(ISceneRouteGuard guard)
    {
        if (_guards.Remove(guard))
        {
            Log.Debug("Guard removed: {0}", guard.GetType().Name);
        }
    }

    /// <summary>
    /// 注册场景过渡处理器的抽象方法，由子类实现。
    /// </summary>
    protected abstract void RegisterHandlers();

    /// <summary>
    /// 系统初始化方法，获取场景工厂并注册处理器。
    /// </summary>
    protected override void OnInit()
    {
        _factory = this.GetUtility<ISceneFactory>()!;
        Log.Debug("SceneRouterBase initialized. Factory={0}", _factory.GetType().Name);
        RegisterHandlers();
    }

    #region Push

    /// <summary>
    /// 将指定场景推入栈顶。
    /// </summary>
    /// <param name="sceneKey">目标场景键名。</param>
    /// <param name="param">场景进入参数。</param>
    /// <returns>异步任务。</returns>
    public async ValueTask PushAsync(
        string sceneKey,
        ISceneEnterParam? param = null)
    {
        await _transitionLock.WaitAsync();
        try
        {
            IsTransitioning = true;

            var @event = CreateEvent(sceneKey, SceneTransitionType.Push, param);

            await _pipeline.ExecuteAroundAsync(@event, async () =>
            {
                await BeforeChangeAsync(@event);
                await PushInternalAsync(sceneKey, param);
                AfterChange(@event);
            });
        }
        finally
        {
            IsTransitioning = false;
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// 内部推送场景实现方法。
    /// 执行守卫检查、场景创建、添加到场景树、加载资源、暂停当前场景、压入栈等操作。
    /// </summary>
    /// <param name="sceneKey">场景键名。</param>
    /// <param name="param">场景进入参数。</param>
    /// <returns>异步任务。</returns>
    private async ValueTask PushInternalAsync(
        string sceneKey,
        ISceneEnterParam? param)
    {
        if (Contains(sceneKey))
        {
            Log.Warn("Scene already in stack: {0}", sceneKey);
            return;
        }

        // 守卫检查
        if (!await ExecuteEnterGuardsAsync(sceneKey, param))
        {
            Log.Warn("Push blocked by guard: {0}", sceneKey);
            return;
        }

        // 通过 Factory 创建场景实例
        var scene = _factory.Create(sceneKey);

        // 添加到场景树
        Root!.AddScene(scene);

        // 加载资源
        await scene.OnLoadAsync(param);

        // 暂停当前场景
        if (_stack.Count > 0)
        {
            var current = _stack.Peek();
            await current.OnPauseAsync();
        }

        // 压入栈
        _stack.Push(scene);

        // 进入场景
        await scene.OnEnterAsync();

        Log.Debug("Push Scene: {0}, stackCount={1}",
            sceneKey, _stack.Count);
    }

    #endregion

    #region Pop

    /// <summary>
    /// 弹出栈顶场景。
    /// </summary>
    /// <returns>异步任务。</returns>
    public async ValueTask PopAsync()
    {
        await _transitionLock.WaitAsync();
        try
        {
            IsTransitioning = true;

            var @event = CreateEvent(null, SceneTransitionType.Pop);

            await _pipeline.ExecuteAroundAsync(@event, async () =>
            {
                await BeforeChangeAsync(@event);
                await PopInternalAsync();
                AfterChange(@event);
            });
        }
        finally
        {
            IsTransitioning = false;
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// 内部弹出场景实现方法。
    /// 执行守卫检查、退出场景、卸载资源、从场景树移除、恢复下一个场景等操作。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async ValueTask PopInternalAsync()
    {
        if (_stack.Count == 0)
            return;

        var top = _stack.Peek();

        // 守卫检查
        if (!await ExecuteLeaveGuardsAsync(top.Key))
        {
            Log.Warn("Pop blocked by guard: {0}", top.Key);
            return;
        }

        _stack.Pop();

        // 退出场景
        await top.OnExitAsync();

        // 卸载资源
        await top.OnUnloadAsync();

        // 从场景树移除
        Root!.RemoveScene(top);

        // 恢复下一个场景
        if (_stack.Count > 0)
        {
            var next = _stack.Peek();
            await next.OnResumeAsync();
        }

        Log.Debug("Pop Scene, stackCount={0}", _stack.Count);
    }

    #endregion

    #region Clear

    /// <summary>
    /// 清空所有场景栈。
    /// </summary>
    /// <returns>异步任务。</returns>
    public async ValueTask ClearAsync()
    {
        await _transitionLock.WaitAsync();
        try
        {
            IsTransitioning = true;

            var @event = CreateEvent(null, SceneTransitionType.Clear);

            await _pipeline.ExecuteAroundAsync(@event, async () =>
            {
                await BeforeChangeAsync(@event);
                await ClearInternalAsync();
                AfterChange(@event);
            });
        }
        finally
        {
            IsTransitioning = false;
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// 内部清空场景栈实现方法。
    /// 循环调用弹出操作直到栈为空。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async ValueTask ClearInternalAsync()
    {
        while (_stack.Count > 0)
        {
            await PopInternalAsync();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 创建场景转换事件对象。
    /// </summary>
    /// <param name="toSceneKey">目标场景键名。</param>
    /// <param name="type">转换类型。</param>
    /// <param name="param">进入参数。</param>
    /// <returns>场景转换事件实例。</returns>
    private SceneTransitionEvent CreateEvent(
        string? toSceneKey,
        SceneTransitionType type,
        ISceneEnterParam? param = null)
    {
        return new SceneTransitionEvent
        {
            FromSceneKey = CurrentKey,
            ToSceneKey = toSceneKey,
            TransitionType = type,
            EnterParam = param
        };
    }

    /// <summary>
    /// 执行转换前阶段的处理逻辑。
    /// </summary>
    /// <param name="event">场景转换事件。</param>
    /// <returns>异步任务。</returns>
    private async Task BeforeChangeAsync(SceneTransitionEvent @event)
    {
        Log.Debug("BeforeChange phases started: {0}", @event.TransitionType);
        await _pipeline.ExecuteAsync(@event, SceneTransitionPhases.BeforeChange);
        Log.Debug("BeforeChange phases completed: {0}", @event.TransitionType);
    }

    /// <summary>
    /// 执行转换后阶段的处理逻辑。
    /// 在后台线程中异步执行，避免阻塞主线程。
    /// </summary>
    /// <param name="event">场景转换事件。</param>
    private void AfterChange(SceneTransitionEvent @event)
    {
        Log.Debug("AfterChange phases started: {0}", @event.TransitionType);
        _ = Task.Run(async () =>
        {
            try
            {
                await _pipeline.ExecuteAsync(@event, SceneTransitionPhases.AfterChange);
                Log.Debug("AfterChange phases completed: {0}", @event.TransitionType);
            }
            catch (Exception ex)
            {
                Log.Error("AfterChange phases failed: {0}, Error: {1}",
                    @event.TransitionType, ex.Message);
            }
        });
    }

    /// <summary>
    /// 执行进入场景的守卫检查。
    /// 按优先级顺序执行所有守卫的CanEnterAsync方法。
    /// </summary>
    /// <param name="sceneKey">场景键名。</param>
    /// <param name="param">进入参数。</param>
    /// <returns>如果所有守卫都允许进入返回true，否则返回false。</returns>
    private async Task<bool> ExecuteEnterGuardsAsync(string sceneKey, ISceneEnterParam? param)
    {
        foreach (var guard in _guards)
        {
            try
            {
                Log.Debug("Executing enter guard: {0} for {1}", guard.GetType().Name, sceneKey);
                var canEnter = await guard.CanEnterAsync(sceneKey, param);

                if (!canEnter)
                {
                    Log.Debug("Enter guard blocked: {0}", guard.GetType().Name);
                    return false;
                }

                if (guard.CanInterrupt)
                {
                    Log.Debug("Enter guard {0} passed, can interrupt = true", guard.GetType().Name);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Enter guard {0} failed: {1}", guard.GetType().Name, ex.Message);
                if (guard.CanInterrupt)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 执行离开场景的守卫检查。
    /// 按优先级顺序执行所有守卫的CanLeaveAsync方法。
    /// </summary>
    /// <param name="sceneKey">场景键名。</param>
    /// <returns>如果所有守卫都允许离开返回true，否则返回false。</returns>
    private async Task<bool> ExecuteLeaveGuardsAsync(string sceneKey)
    {
        foreach (var guard in _guards)
        {
            try
            {
                Log.Debug("Executing leave guard: {0} for {1}", guard.GetType().Name, sceneKey);
                var canLeave = await guard.CanLeaveAsync(sceneKey);

                if (!canLeave)
                {
                    Log.Debug("Leave guard blocked: {0}", guard.GetType().Name);
                    return false;
                }

                if (guard.CanInterrupt)
                {
                    Log.Debug("Leave guard {0} passed, can interrupt = true", guard.GetType().Name);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Leave guard {0} failed: {1}", guard.GetType().Name, ex.Message);
                if (guard.CanInterrupt)
                    return false;
            }
        }

        return true;
    }

    #endregion
}