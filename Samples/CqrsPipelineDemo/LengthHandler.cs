// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace CqrsPipelineDemo;

/// <summary>An ordinary constructor-injected handler.</summary>
/// <param name="scope">The caller's scoped state.</param>
public sealed class LengthHandler(RequestScope scope) : IRequestHandler<GetLength, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(GetLength request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(scope.Disposed, nameof(RequestScope));
        return ValueTask.FromResult(request.Value.Length);
    }
}
