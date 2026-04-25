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

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;
using GFramework.Core.Systems;
using GFramework.Game.Abstractions.Routing;

namespace GFramework.Game.Routing;

/// <summary>
/// 路由器基类,提供通用的路由管理功能
/// </summary>
/// <typeparam name="TRoute">路由项类型,必须实现 IRoute 接口</typeparam>
/// <typeparam name="TContext">路由上下文类型,必须实现 IRouteContext 接口</typeparam>
/// <remarks>
/// 此基类提供了以下通用功能:
/// - 路由守卫管理 (AddGuard/RemoveGuard)
/// - 守卫执行逻辑 (ExecuteEnterGuardsAsync/ExecuteLeaveGuardsAsync)
/// - 路由栈管理 (Stack/Current/CurrentKey)
/// - 栈操作方法 (Contains/PeekKey/IsTop)
/// </remarks>
public abstract class RouterBase<TRoute, TContext> : AbstractSystem
    where TRoute : IRoute
    where TContext : IRouteContext
{
    private static readonly ILogger Log =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(RouterBase<TRoute, TContext>));

    /// <summary>
    /// 路由守卫列表,按优先级排序
    /// </summary>
    private readonly List<IRouteGuard<TRoute>> _guards = new();

    /// <summary>
    /// 路由栈,用于管理路由的显示顺序和导航历史
    /// </summary>
    protected readonly Stack<TRoute> Stack = new();

    /// <summary>
    /// 获取当前路由 (栈顶元素)
    /// </summary>
    public TRoute? Current => Stack.Count > 0 ? Stack.Peek() : default;

    /// <summary>
    /// 获取当前路由的键值
    /// </summary>
    public string? CurrentKey => Current?.Key;

    /// <summary>
    /// 获取栈深度
    /// </summary>
    public int Count => Stack.Count;

    #region Abstract Methods

    /// <summary>
    /// 注册过渡处理器 (由子类实现)
    /// </summary>
    /// <remarks>
    /// 子类应该在此方法中注册所有需要的过渡处理器。
    /// 此方法在 OnInit 中被调用。
    /// </remarks>
    protected abstract void RegisterHandlers();

    #endregion

    #region Guard Management

    /// <summary>
    /// 添加路由守卫
    /// </summary>
    /// <param name="guard">路由守卫实例</param>
    /// <exception cref="ArgumentNullException">当守卫实例为 null 时抛出</exception>
    public void AddGuard(IRouteGuard<TRoute> guard)
    {
        ArgumentNullException.ThrowIfNull(guard);

        if (_guards.Contains(guard))
        {
            Log.Debug("Guard already registered: {0}", guard.GetType().Name);
            return;
        }

        _guards.Add(guard);
        _guards.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        Log.Debug("Guard registered: {0}, Priority={1}", guard.GetType().Name, guard.Priority);
    }

    /// <summary>
    /// 添加路由守卫 (泛型版本)
    /// </summary>
    /// <typeparam name="T">守卫类型,必须实现 IRouteGuard 接口且有无参构造函数</typeparam>
    public void AddGuard<T>() where T : IRouteGuard<TRoute>, new()
    {
        AddGuard(new T());
    }

    /// <summary>
    /// 移除路由守卫
    /// </summary>
    /// <param name="guard">要移除的路由守卫实例</param>
    /// <exception cref="ArgumentNullException">当守卫实例为 null 时抛出</exception>
    public void RemoveGuard(IRouteGuard<TRoute> guard)
    {
        ArgumentNullException.ThrowIfNull(guard);
        if (_guards.Remove(guard))
            Log.Debug("Guard removed: {0}", guard.GetType().Name);
    }

    #endregion

    #region Guard Execution

    /// <summary>
    /// 执行进入守卫检查
    /// </summary>
    /// <param name="routeKey">路由键值</param>
    /// <param name="context">路由上下文</param>
    /// <returns>如果所有守卫都允许进入返回 true,否则返回 false</returns>
    /// <remarks>
    /// 守卫按优先级从小到大依次执行。
    /// 如果某个守卫返回 false 且 CanInterrupt 为 true,则中断后续守卫的执行。
    /// 如果某个守卫抛出异常且 CanInterrupt 为 true,则中断后续守卫的执行。
    /// </remarks>
    protected async Task<bool> ExecuteEnterGuardsAsync(string routeKey, TContext? context)
    {
        foreach (var guard in _guards)
        {
            try
            {
                Log.Debug("Executing enter guard: {0} for {1}", guard.GetType().Name, routeKey);
                var canEnter = await guard.CanEnterAsync(routeKey, context).ConfigureAwait(false);

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
    /// 执行离开守卫检查
    /// </summary>
    /// <param name="routeKey">路由键值</param>
    /// <returns>如果所有守卫都允许离开返回 true,否则返回 false</returns>
    /// <remarks>
    /// 守卫按优先级从小到大依次执行。
    /// 如果某个守卫返回 false 且 CanInterrupt 为 true,则中断后续守卫的执行。
    /// 如果某个守卫抛出异常且 CanInterrupt 为 true,则中断后续守卫的执行。
    /// </remarks>
    protected async Task<bool> ExecuteLeaveGuardsAsync(string routeKey)
    {
        foreach (var guard in _guards)
        {
            try
            {
                Log.Debug("Executing leave guard: {0} for {1}", guard.GetType().Name, routeKey);
                var canLeave = await guard.CanLeaveAsync(routeKey).ConfigureAwait(false);

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

    #region Stack Operations

    /// <summary>
    /// 检查栈中是否包含指定路由
    /// </summary>
    /// <param name="routeKey">路由键值</param>
    /// <returns>如果栈中包含指定路由返回 true,否则返回 false</returns>
    public bool Contains(string routeKey)
    {
        return Stack.Any(r => r.Key == routeKey);
    }

    /// <summary>
    /// 获取栈顶路由的键值
    /// </summary>
    /// <returns>栈顶路由的键值,如果栈为空则返回空字符串</returns>
    public string PeekKey()
    {
        return Stack.Count == 0 ? string.Empty : Stack.Peek().Key;
    }

    /// <summary>
    /// 判断栈顶是否为指定路由
    /// </summary>
    /// <param name="routeKey">路由键值</param>
    /// <returns>如果栈顶是指定路由返回 true,否则返回 false</returns>
    public bool IsTop(string routeKey)
    {
        return Stack.Count != 0 && Stack.Peek().Key.Equals(routeKey);
    }

    #endregion
}
