// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace CqrsPipelineDemo;

/// <summary>A reusable open generic behavior that deliberately depends on scoped state.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="scope">State belonging to the caller's explicit scope.</param>
public sealed class ScopeBehavior<TRequest, TResponse>(RequestScope scope) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        scope.Calls++;
        return next(message, cancellationToken);
    }
}
