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
using GFramework.Core.logging;
using GFramework.Core.system;
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

    private readonly Stack<ISceneBehavior> _stack = new();
    private readonly SemaphoreSlim _transitionLock = new(1, 1);

    protected ISceneRoot? Root;

    public ISceneBehavior? Current => _stack.Count > 0 ? _stack.Peek() : null;

    public string? CurrentKey => Current?.Key;

    public IReadOnlyList<ISceneBehavior> Stack =>
        _stack.Reverse().ToList();

    public bool IsTransitioning { get; private set; }

    public void BindRoot(ISceneRoot root)
    {
        Root = root;
        Log.Debug("Bind Scene Root: {0}", root.GetType().Name);
    }

    #region Replace

    public async ValueTask ReplaceAsync(
        string sceneKey,
        ISceneEnterParam? param = null)
    {
        await _transitionLock.WaitAsync();
        try
        {
            IsTransitioning = true;

            Log.Debug("Replace Scene: {0}", sceneKey);

            await ClearInternalAsync();
            await PushInternalAsync(sceneKey, param);
        }
        finally
        {
            IsTransitioning = false;
            _transitionLock.Release();
        }
    }

    #endregion

    #region Query

    public bool Contains(string sceneKey)
    {
        return _stack.Any(s => s.Key == sceneKey);
    }

    #endregion

    #region Push

    public async ValueTask PushAsync(
        string sceneKey,
        ISceneEnterParam? param = null)
    {
        await _transitionLock.WaitAsync();
        try
        {
            IsTransitioning = true;
            await PushInternalAsync(sceneKey, param);
        }
        finally
        {
            IsTransitioning = false;
            _transitionLock.Release();
        }
    }

    private async ValueTask PushInternalAsync(
        string sceneKey,
        ISceneEnterParam? param)
    {
        if (Contains(sceneKey))
        {
            Log.Warn("Scene already in stack: {0}", sceneKey);
            return;
        }

        var scene = await Root!.LoadAsync(sceneKey);

        if (_stack.Count > 0)
        {
            var current = _stack.Peek();
            await current.OnPauseAsync();
        }

        _stack.Push(scene);

        await scene.OnEnterAsync(param);
        await scene.OnShowAsync();

        Log.Debug("Push Scene: {0}, stackCount={1}",
            sceneKey, _stack.Count);
    }

    #endregion

    #region Pop

    public async ValueTask PopAsync()
    {
        await _transitionLock.WaitAsync();
        try
        {
            IsTransitioning = true;
            await PopInternalAsync();
        }
        finally
        {
            IsTransitioning = false;
            _transitionLock.Release();
        }
    }

    private async ValueTask PopInternalAsync()
    {
        if (_stack.Count == 0)
            return;

        var top = _stack.Pop();

        await top.OnExitAsync();
        await Root!.UnloadAsync(top);

        if (_stack.Count > 0)
        {
            var next = _stack.Peek();
            await next.OnResumeAsync();
            await next.OnShowAsync();
        }

        Log.Debug("Pop Scene, stackCount={0}", _stack.Count);
    }

    #endregion

    #region Clear

    public async ValueTask ClearAsync()
    {
        await _transitionLock.WaitAsync();
        try
        {
            IsTransitioning = true;
            await ClearInternalAsync();
        }
        finally
        {
            IsTransitioning = false;
            _transitionLock.Release();
        }
    }

    private async ValueTask ClearInternalAsync()
    {
        while (_stack.Count > 0)
        {
            await PopInternalAsync();
        }
    }

    #endregion
}