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

using GFramework.Core.Abstractions.Concurrency;

namespace GFramework.Core.Concurrency;

/// <summary>
///     异步锁句柄实现
/// </summary>
internal sealed class AsyncLockHandle(AsyncKeyLockManager manager, string key, AsyncKeyLockManager.LockEntry entry,
    long acquiredTicks) : IAsyncLockHandle
{
    private readonly AsyncKeyLockManager.LockEntry _entry = entry;
    private readonly string _key = key;
    private readonly AsyncKeyLockManager _manager = manager;
    private int _disposed;

    public string Key { get; } = key;
    public long AcquiredTicks { get; } = acquiredTicks;

    public ValueTask DisposeAsync()
    {
        Release();
        return ValueTask.CompletedTask;
    }

    public void Dispose() => Release();

    // Both disposal contracts perform the same immediate, idempotent release without async self-dispatch.
    private void Release()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _manager.ReleaseLock(_key, _entry);
        }
    }
}
