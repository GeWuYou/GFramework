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

using System.Runtime.InteropServices;

namespace GFramework.Core.Abstractions.Concurrency;

/// <summary>
///     锁统计信息
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LockStatistics
{
    /// <summary>
    ///     当前活跃的锁数量
    /// </summary>
    public int ActiveLockCount
    {
        get; init;
    }

    /// <summary>
    ///     累计获取锁的次数
    /// </summary>
    public int TotalAcquired
    {
        get; init;
    }

    /// <summary>
    ///     累计释放锁的次数
    /// </summary>
    public int TotalReleased
    {
        get; init;
    }

    /// <summary>
    ///     累计清理的锁数量
    /// </summary>
    public int TotalCleaned
    {
        get; init;
    }
}
