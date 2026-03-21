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
///     锁信息（用于调试）
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LockInfo
{
    /// <summary>
    ///     锁的键。
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    ///     当前引用计数。
    /// </summary>
    public int ReferenceCount { get; init; }

    /// <summary>
    ///     最后访问时间戳（Environment.TickCount64）。
    /// </summary>
    public long LastAccessTicks { get; init; }

    /// <summary>
    ///     等待队列长度（近似值）。
    ///     注意：这是一个基于 SemaphoreSlim.CurrentCount 的近似指示器，
    ///     当 CurrentCount == 0 时表示锁被持有且可能有等待者，返回 1；
    ///     否则返回 0。这不是精确的等待者数量，仅用于调试参考。
    /// </summary>
    public int WaitingCount { get; init; }
}