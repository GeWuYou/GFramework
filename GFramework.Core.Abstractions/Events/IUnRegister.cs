// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Events;

/// <summary>
///     提供注销功能的接口
/// </summary>
public interface IUnRegister
{
    /// <summary>
    ///     执行注销操作
    /// </summary>
    void UnRegister();
}