// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Abstractions.Scene;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.Scene;

/// <summary>
/// Default constructor-injected scene navigator. Register optional guards and transitions at composition time,
/// navigate after the host is ready, and await disposal before releasing the root or dispatcher.
/// </summary>
public sealed class SceneRouter : SceneRouterBase
{
    /// <summary>Creates the navigator with optional transition extensions.</summary>
    /// <param name="factory">Scene construction adapter.</param>
    /// <param name="root">Host attachment adapter.</param>
    /// <param name="dispatcher">Host callback dispatcher.</param>
    public SceneRouter(ISceneFactory factory, ISceneRoot root, IGameDispatcher dispatcher)
        : base(factory, root, dispatcher)
    {
    }

    /// <summary>Creates the navigator with standard DI-compatible transition diagnostics.</summary>
    /// <param name="factory">Scene construction adapter.</param>
    /// <param name="root">Host attachment adapter.</param>
    /// <param name="dispatcher">Host callback dispatcher.</param>
    /// <param name="logger">Host-configured logger, forwarded to the private transition pipeline; null keeps logging disabled.</param>
    public SceneRouter(ISceneFactory factory, ISceneRoot root, IGameDispatcher dispatcher, ILogger<SceneRouter>? logger)
        : base(factory, root, dispatcher, logger)
    {
    }
}
