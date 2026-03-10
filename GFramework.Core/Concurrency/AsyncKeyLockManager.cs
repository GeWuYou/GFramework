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

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Concurrency;

namespace GFramework.Core.Concurrency;

/// <summary>
///     工业级异步键锁管理器，支持自动清理、统计和调试
/// </summary>
public sealed class AsyncKeyLockManager : IAsyncKeyLockManager
{
    private readonly Timer _cleanupTimer;
    private readonly ConcurrentDictionary<string, LockEntry> _locks = new();
    private readonly long _lockTimeoutTicks;
    private volatile bool _disposed;

    // 统计计数器
    private int _totalAcquired;
    private int _totalCleaned;
    private int _totalReleased;

    /// <summary>
    ///     初始化锁管理器
    /// </summary>
    /// <param name="cleanupInterval">清理间隔，默认 60 秒</param>
    /// <param name="lockTimeout">锁超时时间，默认 300 秒</param>
    public AsyncKeyLockManager(TimeSpan? cleanupInterval = null, TimeSpan? lockTimeout = null)
    {
        var cleanupIntervalValue = cleanupInterval ?? TimeSpan.FromSeconds(60);
        var lockTimeoutValue = lockTimeout ?? TimeSpan.FromSeconds(300);
        _lockTimeoutTicks = (long)(lockTimeoutValue.TotalMilliseconds * TimeSpan.TicksPerMillisecond / 10000);

        _cleanupTimer = new Timer(CleanupUnusedLocks, null, cleanupIntervalValue, cleanupIntervalValue);
    }

    /// <summary>
    ///     异步获取指定键的锁
    /// </summary>
    public async ValueTask<IAsyncLockHandle> AcquireLockAsync(string key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entry = _locks.GetOrAdd(key, _ => new LockEntry());
        Interlocked.Increment(ref entry.ReferenceCount);
        entry.LastAccessTicks = System.Environment.TickCount64;

        // 再次检查 disposed（防止在 GetOrAdd 后 Dispose）
        if (_disposed)
        {
            Interlocked.Decrement(ref entry.ReferenceCount);
            throw new ObjectDisposedException(nameof(AsyncKeyLockManager));
        }

        await entry.Semaphore.WaitAsync(cancellationToken);

        Interlocked.Increment(ref _totalAcquired);

        return new AsyncLockHandle(this, key, entry, System.Environment.TickCount64);
    }

    /// <summary>
    ///     同步获取指定键的锁
    /// </summary>
    public IAsyncLockHandle AcquireLock(string key)
    {
        return AcquireLockAsync(key).AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    ///     获取统计信息
    /// </summary>
    public LockStatistics GetStatistics()
    {
        return new LockStatistics
        {
            ActiveLockCount = _locks.Count,
            TotalAcquired = _totalAcquired,
            TotalReleased = _totalReleased,
            TotalCleaned = _totalCleaned
        };
    }

    /// <summary>
    ///     获取活跃锁信息
    /// </summary>
    public IReadOnlyDictionary<string, LockInfo> GetActiveLocks()
    {
        return _locks.ToDictionary(
            kvp => kvp.Key,
            kvp => new LockInfo
            {
                Key = kvp.Key,
                ReferenceCount = kvp.Value.ReferenceCount,
                LastAccessTicks = kvp.Value.LastAccessTicks,
                WaitingCount = kvp.Value.Semaphore.CurrentCount == 0 ? 1 : 0
            });
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cleanupTimer.Dispose();

        // 统一释放所有 semaphore
        foreach (var entry in _locks.Values)
        {
            entry.Dispose();
        }

        _locks.Clear();
    }

    /// <summary>
    ///     释放指定键的锁
    /// </summary>
    internal void ReleaseLock(string key, LockEntry entry)
    {
        entry.Semaphore.Release();
        Interlocked.Decrement(ref entry.ReferenceCount);
        Interlocked.Increment(ref _totalReleased);
        entry.LastAccessTicks = System.Environment.TickCount64;
    }

    /// <summary>
    ///     清理未使用的锁（不 Dispose semaphore，避免 race condition）
    /// </summary>
    private void CleanupUnusedLocks(object? state)
    {
        if (_disposed) return;

        var now = System.Environment.TickCount64;

        foreach (var (key, entry) in _locks)
        {
            // 只检查引用计数和超时，不 Dispose
            if (entry.ReferenceCount == 0 &&
                now - entry.LastAccessTicks > _lockTimeoutTicks &&
                _locks.TryRemove(key, out _))
            {
                Interlocked.Increment(ref _totalCleaned);
            }
        }
    }

    /// <summary>
    ///     锁条目，包含信号量和引用计数
    /// </summary>
    internal sealed class LockEntry : IDisposable
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public long LastAccessTicks = System.Environment.TickCount64;
        public int ReferenceCount;

        public void Dispose()
        {
            Semaphore.Dispose();
        }
    }
}