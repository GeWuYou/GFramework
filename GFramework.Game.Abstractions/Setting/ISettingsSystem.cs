// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0



namespace GFramework.Game.Abstractions.Setting;

/// <summary>
///     定义设置系统的接口，提供应用各种设置的方法
/// </summary>
public interface ISettingsSystem
{
    /// <summary>
    ///     应用所有可应用的设置
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    Task ApplyAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     应用指定类型的设置（泛型版本）
    /// </summary>
    /// <typeparam name="T">设置类型，必须是class且实现IResetApplyAbleSettings接口</typeparam>
    /// <returns>表示异步操作的任务</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    Task ApplyAsync<T>(CancellationToken cancellationToken = default) where T : class, IResetApplyAbleSettings;

    /// <summary>
    ///     保存所有设置
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    Task SaveAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     重置指定类型的设置
    /// </summary>
    /// <typeparam name="T">设置类型，必须继承自class并实现IPersistentApplyAbleSettings接口</typeparam>
    /// <returns>表示异步操作的任务</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    Task ResetAsync<T>(CancellationToken cancellationToken = default) where T : class, ISettingsData, IResetApplyAbleSettings, new();

    /// <summary>
    ///     重置所有设置
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    Task ResetAllAsync(CancellationToken cancellationToken = default);
}