// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Lifecycle;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>Optional owner of one service provider and an explicitly ordered startup/stop operation.</summary>
/// <remarks>Business objects receive their dependencies directly; only composition roots should resolve through Services.</remarks>
public interface IArchitecture : IInitializable, IAsyncInitializable, IAsyncDestroyable, IAsyncDisposable
{
    /// <summary>Root service access, including standard keyed resolution; rejected before build and during external stop.</summary>
    /// <remarks>Already-created scopes, cached scope factories and injected standard providers remain caller-owned.
    /// Stop application work and dispose its scopes before disposing this architecture.</remarks>
    IServiceProvider Services
    {
        get;
    }

    /// <summary>Waits for startup, including failure cleanup; cancellation and failure remain observable.</summary>
    /// <returns>The unique readiness result.</returns>
    Task WaitUntilReadyAsync();
}
