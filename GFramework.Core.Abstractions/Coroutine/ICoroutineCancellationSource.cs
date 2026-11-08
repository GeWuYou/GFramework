// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     为协程枚举器提供额外的生命周期取消来源，供任意宿主通过统一调度入口绑定。
/// </summary>
/// <remarks>
///     调度器在接收枚举器或工厂结果后、执行第一步前读取令牌并拥有其取消注册。
///     取消来源仍由实现方管理；取消回调只排队终止请求，不在来源线程操作宿主对象。
/// </remarks>
public interface ICoroutineCancellationSource
{
    /// <summary>
    ///     获取该枚举器额外绑定的生命周期令牌。返回值应稳定，读取不得启动业务操作。
    /// </summary>
    CancellationToken CancellationToken { get; }
}
