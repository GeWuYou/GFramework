// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Modding;

/// <summary>A trusted process-lifetime game extension, initialized and stopped on the host dispatcher.</summary>
/// <remarks>Instances are created once after approval. Initialize and Start must await their work; Stop must
/// release business activity even when Initialize failed partway. Changing the mod set requires restart.</remarks>
public interface IGameMod
{
    /// <summary>Registers game extensions without entering gameplay.</summary>
    /// <param name="context">Per-mod services and tracked resource ownership.</param>
    /// <param name="cancellationToken">Cancels cooperative initialization.</param>
    Task InitializeAsync(IGameModContext context, CancellationToken cancellationToken);
    /// <summary>Starts an initialized mod after native resources and the content catalog are ready.</summary>
    /// <param name="cancellationToken">Cancels cooperative startup.</param>
    Task StartAsync(CancellationToken cancellationToken);
    /// <summary>Stops activity before tracked resources are released; called even after partial initialization.</summary>
    /// <param name="cancellationToken">Shutdown token; the host uses a non-cancelled token for mandatory cleanup.</param>
    Task StopAsync(CancellationToken cancellationToken);
}
