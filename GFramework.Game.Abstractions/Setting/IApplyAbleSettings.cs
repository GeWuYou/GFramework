// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Setting;

/// <summary>
///     定义可应用设置的接口，继承自ISettingsSection
/// </summary>
public interface IApplyAbleSettings : ISettingsSection
{
    /// <summary>
    ///     异步应用当前设置到目标系统中。
    /// </summary>
    /// <returns>
    ///     表示应用流程完成的任务。
    ///     对于仅执行同步引擎调用的实现，可以返回已完成任务。
    /// </returns>
    Task ApplyAsync();
}
