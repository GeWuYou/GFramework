// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Tests.Common;

/// <summary>Thin test invocation fixture forwarding to a production runtime resolved from standard DI.</summary>
/// <param name="services">The built fixture provider.</param>
public sealed class CqrsTestHost(IServiceProvider services)
{
    /// <summary>Resolves a fixture service for cache identity assertions.</summary>
    public T GetService<T>() where T : class => services.GetRequiredService<T>();
    /// <summary>Invokes the production request pipeline.</summary>
    public ValueTask<T> SendRequestAsync<T>(IRequest<T> request, CancellationToken cancellationToken = default) => GetService<ICqrsRuntime>().SendAsync(request, cancellationToken);
    /// <summary>Invokes the production request pipeline.</summary>
    public ValueTask<T> SendAsync<T>(IRequest<T> request, CancellationToken cancellationToken = default) => SendRequestAsync(request, cancellationToken);
    /// <summary>Invokes the production notification publisher.</summary>
    public ValueTask PublishAsync<T>(T notification, CancellationToken cancellationToken = default) where T : INotification => GetService<ICqrsRuntime>().PublishAsync(notification, cancellationToken);
    /// <summary>Invokes the production stream pipeline.</summary>
    public IAsyncEnumerable<T> CreateStream<T>(IStreamRequest<T> request, CancellationToken cancellationToken = default) => GetService<ICqrsRuntime>().CreateStream(request, cancellationToken);
}
