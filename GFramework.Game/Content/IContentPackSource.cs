// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;

namespace GFramework.Game.Content;

/// <summary>Discovers package descriptors without enabling them or loading native resources.</summary>
public interface IContentPackSource
{
    /// <summary>Discovers packages from configured roots; malformed packages remain explicit failures.</summary>
    /// <param name="roots">Package roots or directories containing package roots.</param>
    /// <param name="cancellationToken">Cancels discovery and fingerprinting.</param>
    /// <returns>Discovered packages; newly discovered optional packages are not implicitly enabled.</returns>
    Task<IReadOnlyList<ContentPack>> DiscoverAsync(IEnumerable<string> roots, CancellationToken cancellationToken = default);
}
