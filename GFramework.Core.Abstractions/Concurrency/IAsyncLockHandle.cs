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

namespace GFramework.Core.Abstractions.Concurrency;

/// <summary>
///     异步锁句柄接口，支持 await using 语法
/// </summary>
public interface IAsyncLockHandle : IAsyncDisposable, IDisposable
{
    /// <summary>
    ///     锁的键
    /// </summary>
    string Key { get; }

    /// <summary>
    ///     锁获取时的时间戳（Environment.TickCount64）
    /// </summary>
    long AcquiredTicks { get; }
}