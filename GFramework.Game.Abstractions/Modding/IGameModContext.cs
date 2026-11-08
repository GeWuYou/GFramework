// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Runtime;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.Abstractions.Modding;

/// <summary>Per-mod host capabilities and reverse-order resource ownership; this is not a security sandbox.</summary>
public interface IGameModContext
{
    /// <summary>Gets the game identity used to namespace integration state.</summary>
    string GameId
    {
        get;
    }
    /// <summary>Gets the exact approved package snapshot.</summary>
    ContentPackStamp Package
    {
        get;
    }
    /// <summary>Gets host-selected services, with per-mod services preceding shared services.</summary>
    IServiceProvider Services
    {
        get;
    }
    /// <summary>Gets the dispatcher for operations that touch the native host.</summary>
    IGameDispatcher Dispatcher
    {
        get;
    }
    /// <summary>Gets a logger categorized with the mod identity.</summary>
    ILogger Logger
    {
        get;
    }
    /// <summary>Transfers a disposable resource to this mod scope for reverse-order cleanup.</summary>
    /// <param name="resource">Resource owned by this mod, not a shared host singleton.</param>
    void Own(IDisposable resource);
    /// <summary>Transfers an asynchronously disposable resource to the mod scope.</summary>
    /// <param name="resource">Resource whose cleanup is awaited by host shutdown.</param>
    void OwnAsync(IAsyncDisposable resource);
    /// <summary>Tracks an existing framework event subscription without creating a second event mechanism.</summary>
    /// <param name="registration">Subscription to unregister during scope cleanup.</param>
    void OwnRegistration(IUnRegister registration);
}
