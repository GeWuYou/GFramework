// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Game.Abstractions.Runtime;

namespace DodgeTheCreeps.Advanced.Composition;

/// <summary>Explicit lifecycle participant for content IO; ordinary services are not initialized by role or naming.</summary>
/// <param name="content">Content service whose resource ownership remains with DI.</param>
/// <param name="dispatcher">The owning native thread.</param>
internal sealed class ContentStartup(Content.DodgeContent content, IGameDispatcher dispatcher) : IAsyncInitializable, IAsyncDestroyable
{
    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        dispatcher.InvokeAsync(() => content.InitializeAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public ValueTask DestroyAsync() => new(dispatcher.InvokeAsync(content.StopModsAsync));
}
