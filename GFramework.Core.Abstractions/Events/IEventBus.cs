// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Events;

/// <summary>Instance-local synchronous notifications delivered on the publishing thread.</summary>
/// <remarks>Each publication snapshots registrations. Callback exceptions stop delivery and propagate.</remarks>
public interface IEventBus
{
    /// <summary>Publish to ordinary and priority subscribers in one channel.</summary>
    /// <typeparam name="T">Notification type.</typeparam>
    /// <param name="value">Notification value.</param>
    /// <param name="propagation">Delivery mode, scoped to this publication.</param>
    void Publish<T>(T value, EventPropagation propagation = EventPropagation.All);

    /// <summary>Subscribe until the token is disposed; higher priorities run first, ties retain insertion order.</summary>
    /// <typeparam name="T">Notification type.</typeparam>
    /// <param name="handler">Synchronous callback.</param>
    /// <param name="priority">Larger values execute first.</param>
    /// <returns>An independently owned idempotent token.</returns>
    IDisposable Subscribe<T>(Action<T> handler, int priority = 0);

    /// <summary>Subscribe with access to this publication's handled flag.</summary>
    /// <typeparam name="T">Notification type.</typeparam>
    /// <param name="handler">Synchronous callback receiving a publication-local context.</param>
    /// <param name="priority">Larger values execute first.</param>
    /// <returns>An independently owned idempotent token.</returns>
    IDisposable SubscribeWithContext<T>(Action<EventContext<T>> handler, int priority = 0);
}
