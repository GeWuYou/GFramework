// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Modding;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Game.Modding;

/// <summary>Startup-owned mod configuration; configure once before preparing a session.</summary>
public sealed class GameModHostOptions
{
    /// <summary>Gets the host game identity.</summary>
    public required string GameId { get; init; }
    /// <summary>Gets the running game's explicitly published code API version.</summary>
    public required string CodeApiVersion { get; init; }
    /// <summary>Gets shared, caller-owned game services. The mod host never disposes this provider.</summary>
    public IServiceProvider Services { get; init; } = EmptyModServices.Instance;
    /// <summary>Gets an optional per-mod provider, resolved before shared services; its ownership stays with the game.</summary>
    public Func<IGameModContext, IServiceProvider>? CreateServices { get; init; }
    /// <summary>Gets the native host dispatcher; standalone .NET hosts can use InlineGameDispatcher.</summary>
    public IGameDispatcher Dispatcher { get; init; } = new InlineGameDispatcher();
    /// <summary>Gets the exact-stamp approval policy. A missing policy denies all code.</summary>
    public Func<ContentPackStamp, bool>? TrustPolicy { get; init; }
    /// <summary>Gets host-owned API assemblies shared with all mods; private dependencies remain isolated.</summary>
    public IReadOnlyList<Assembly> SharedAssemblies { get; init; } = Array.Empty<Assembly>();
    /// <summary>Gets optional adapters, such as HarmonyModExtension.</summary>
    public IReadOnlyList<IGameModExtension> Extensions { get; init; } = Array.Empty<IGameModExtension>();
    /// <summary>Gets a caller-owned logger factory.</summary>
    public ILoggerFactory LoggerFactory { get; init; } = NullLoggerFactory.Instance;
}
