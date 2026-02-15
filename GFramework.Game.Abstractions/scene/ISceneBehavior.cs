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

namespace GFramework.Game.Abstractions.scene;

/// <summary>
/// 场景行为接口，定义了场景生命周期管理的标准方法。
/// 实现此接口的类需要处理场景的加载、激活、暂停、恢复和卸载等核心操作。
/// </summary>
public interface ISceneBehavior
{
    /// <summary>
    /// 获取场景的唯一标识符。
    /// 用于区分不同的场景实例。
    /// </summary>
    string Key { get; }

    /// <summary>
    /// 获取场景是否已加载完成的状态。
    /// true表示场景资源已加载，false表示未加载或正在加载中。
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// 获取场景是否处于活跃运行状态。
    /// true表示场景正在前台运行，false表示场景已暂停或未激活。
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 获取场景是否正在进行切换操作。
    /// true表示场景正在加载、卸载或过渡中，false表示场景状态稳定。
    /// </summary>
    bool IsTransitioning { get; }

    /// <summary>
    /// 异步加载场景所需资源。
    /// 在场景正式进入前调用，负责预加载场景所需的各类资源。
    /// </summary>
    /// <param name="param">场景进入参数，可能包含初始化数据或上下文信息。</param>
    /// <returns>表示加载操作完成的ValueTask。</returns>
    ValueTask OnLoadAsync(ISceneEnterParam? param);

    /// <summary>
    /// 异步处理场景正式进入逻辑。
    /// 在资源加载完成后调用，启动场景的主要运行逻辑。
    /// </summary>
    /// <returns>表示进入操作完成的ValueTask。</returns>
    ValueTask OnEnterAsync();

    /// <summary>
    /// 异步处理场景暂停逻辑。
    /// 当场景被其他场景覆盖或失去焦点时调用。
    /// </summary>
    /// <returns>表示暂停操作完成的ValueTask。</returns>
    ValueTask OnPauseAsync();

    /// <summary>
    /// 异步处理场景恢复逻辑。
    /// 当场景重新获得焦点或从暂停状态恢复时调用。
    /// </summary>
    /// <returns>表示恢复操作完成的ValueTask。</returns>
    ValueTask OnResumeAsync();

    /// <summary>
    /// 异步处理场景退出逻辑。
    /// 在场景即将被替换或关闭时调用，执行清理工作。
    /// </summary>
    /// <returns>表示退出操作完成的ValueTask。</returns>
    ValueTask OnExitAsync();

    /// <summary>
    /// 异步卸载场景资源。
    /// 在场景完全退出后调用，释放占用的内存和资源。
    /// </summary>
    /// <returns>表示卸载操作完成的ValueTask。</returns>
    ValueTask OnUnloadAsync();
}