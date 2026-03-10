// Copyright (c) 2025 GeWuYou
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

using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Abstractions.Concurrency;

/// <summary>
///     异步键锁管理器接口，提供基于键的细粒度锁机制
/// </summary>
public interface IAsyncKeyLockManager : IUtility, IDisposable
{
    /// <summary>
    ///     异步获取指定键的锁（推荐使用）
    /// </summary>
    /// <param name="key">锁键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁句柄，使用 await using 自动释放</returns>
    ValueTask<IAsyncLockHandle> AcquireLockAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     同步获取指定键的锁（兼容性方法）
    /// </summary>
    /// <param name="key">锁键</param>
    /// <returns>锁句柄，使用 using 自动释放</returns>
    IAsyncLockHandle AcquireLock(string key);

    /// <summary>
    ///     获取锁管理器的统计信息
    /// </summary>
    /// <returns>统计信息快照</returns>
    LockStatistics GetStatistics();

    /// <summary>
    ///     获取当前活跃的锁信息（用于调试）
    /// </summary>
    /// <returns>键到锁信息的只读字典</returns>
    IReadOnlyDictionary<string, LockInfo> GetActiveLocks();
}