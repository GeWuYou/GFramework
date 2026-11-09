// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;

namespace DodgeTheCreeps.Advanced.Composition;

/// <summary>Owns one application's cancellation source; the provider disposes it after actions stop.</summary>
internal sealed class GameLifetime : IDisposable
{
    private readonly CancellationTokenSource _source = new();

    /// <summary>Cancellation shared by host operations, never by unrelated sample instances.</summary>
    internal CancellationToken Token => _source.Token;

    /// <summary>Stops admission and requests cancellation without abandoning cleanup.</summary>
    internal void Cancel() => _source.Cancel();

    /// <summary>Releases cancellation registrations after all application operations have completed.</summary>
    public void Dispose() => _source.Dispose();
}
