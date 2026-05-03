// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Events;

/// <summary>
///     提供统一注销功能的接口，用于管理需要注销的对象列表
/// </summary>
public interface IUnRegisterList
{
    /// <summary>
    ///     获取需要注销的对象列表
    /// </summary>
    IList<IUnRegister> UnregisterList { get; }
}