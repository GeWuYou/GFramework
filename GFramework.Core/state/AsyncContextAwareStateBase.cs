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

using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.Abstractions.state;
using IDisposable = GFramework.Core.Abstractions.lifecycle.IDisposable;

namespace GFramework.Core.state;

/// <summary>
///     上下文感知异步状态基类
///     提供基础的异步状态管理功能和架构上下文访问能力
///     实现了IAsyncState和IContextAware接口
/// </summary>
public class AsyncContextAwareStateBase : IAsyncState, IContextAware, IDisposable
{
    /// <summary>
    ///     架构上下文引用，用于访问架构相关的服务和数据
    /// </summary>
    private IArchitectureContext? _context;

    /// <summary>
    ///     异步进入状态时调用的方法
    ///     子类可重写此方法以实现特定的异步状态进入逻辑（如加载资源、初始化数据等）
    /// </summary>
    /// <param name="from">从哪个状态转换而来，可能为null表示初始状态</param>
    public virtual Task OnEnterAsync(IState? from)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     异步退出状态时调用的方法
    ///     子类可重写此方法以实现特定的异步状态退出逻辑（如保存数据、清理资源等）
    /// </summary>
    /// <param name="to">将要转换到的目标状态，可能为null表示结束状态</param>
    public virtual Task OnExitAsync(IState? to)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     异步判断当前状态是否可以转换到目标状态
    ///     子类可重写此方法以实现自定义的异步状态转换规则（如验证条件、检查权限等）
    /// </summary>
    /// <param name="target">希望转换到的目标状态对象</param>
    /// <returns>如果允许转换则返回true，否则返回false</returns>
    public virtual Task<bool> CanTransitionToAsync(IState target)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    ///     设置架构上下文
    /// </summary>
    /// <param name="context">架构上下文实例</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     获取架构上下文
    /// </summary>
    /// <returns>架构上下文实例</returns>
    public IArchitectureContext GetContext()
    {
        return _context ?? throw new InvalidOperationException(
            $"Architecture context has not been set. Call {nameof(SetContext)} before accessing the context.");
    }

    /// <summary>
    ///     销毁当前状态，释放相关资源
    ///     子类可重写此方法以执行特定的清理操作
    /// </summary>
    public virtual void Destroy()
    {
    }
}