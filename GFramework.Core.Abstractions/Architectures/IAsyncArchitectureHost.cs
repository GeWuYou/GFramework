// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>Declares that a composition root requires asynchronous host attachment.</summary>
/// <remarks>
/// Implement explicitly on asynchronous hosts. Architecture invokes this capability once, after components start;
/// callers must use InitializeAsync. Thread-affine hosts dispatch their own native operations.
/// Ordinary business services do not implement this interface.
/// </remarks>
public interface IAsyncArchitectureHost
{
    /// <summary>Attaches the host after component startup and before readiness is published.</summary>
    /// <param name="cancellationToken">The architecture startup lifetime.</param>
    /// <returns>Completion of required host setup; failures trigger normal startup rollback.</returns>
    ValueTask StartHostAsync(CancellationToken cancellationToken);
}
