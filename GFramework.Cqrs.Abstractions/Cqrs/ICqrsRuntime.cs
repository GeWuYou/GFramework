// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Abstractions.Cqrs;

/// <summary>Dispatches requests, notifications and streams using explicitly registered standard DI handlers.</summary>
/// <remarks>Handlers and behaviors receive dependencies through constructors, never through per-dispatch context mutation.</remarks>
public interface ICqrsRuntime
{
    /// <summary>Resolves and invokes one request handler through the configured pipeline.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The operation lifetime.</param>
    /// <returns>The handler response.</returns>
    ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>Publishes to the configured notification strategy; zero handlers is valid.</summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The operation lifetime.</param>
    /// <returns>Completion of the configured strategy.</returns>
    ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>Creates a response stream whose caller-owned DI scope must remain alive throughout enumeration.</summary>
    /// <typeparam name="TResponse">The element type.</typeparam>
    /// <param name="request">The stream request.</param>
    /// <param name="cancellationToken">The enumeration lifetime.</param>
    /// <returns>The response sequence.</returns>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);
}
