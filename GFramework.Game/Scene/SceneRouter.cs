// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Abstractions.Scene;

namespace GFramework.Game.Scene;

/// <summary>
/// Default constructor-injected scene navigator. Register optional guards and transitions at composition time,
/// navigate after the host is ready, and await disposal before releasing the root or dispatcher.
/// </summary>
public sealed class SceneRouter : SceneRouterBase
{
    /// <summary>Creates the default scene navigator without an Architecture dependency.</summary>
    /// <param name="factory">Scene construction adapter.</param>
    /// <param name="root">Host attachment adapter.</param>
    /// <param name="dispatcher">Host callback dispatcher.</param>
    public SceneRouter(ISceneFactory factory, ISceneRoot root, IGameDispatcher dispatcher)
        : base(factory, root, dispatcher)
    {
    }
}
