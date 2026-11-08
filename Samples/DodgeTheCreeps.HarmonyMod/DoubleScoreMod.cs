// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Modding;
using GFramework.Game.Harmony;
using HarmonyLib;

namespace DodgeTheCreeps.HarmonyMod;

/// <summary>Demonstrates an owner-scoped patch of a game method outside its published mod API.</summary>
public sealed class DoubleScoreMod : IGameMod
{
    /// <inheritdoc />
    public Task InitializeAsync(IGameModContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var patches = context.Services.GetService(typeof(IModPatchService)) as IModPatchService
            ?? throw new InvalidOperationException("This mod requires the host Harmony extension.");
        patches.PatchAll(typeof(DoubleScoreMod).Assembly);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Targets the named internal implementation; a renamed method fails startup instead of silently doing nothing.</summary>
    [HarmonyPatch]
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
    internal static class ScorePatch
    {
        /// <summary>Locates one exact game-owned method without requiring the game implementation as a build dependency.</summary>
        private static MethodInfo TargetMethod() => AccessTools.Method("DodgeTheCreeps.Modding.DodgeScoreRules:Calculate")
            ?? throw new MissingMethodException("The game no longer contains DodgeScoreRules.Calculate.");

        /// <summary>Doubles the method's result after the ordinary game's calculation.</summary>
        private static void Postfix(ref int __result) => __result = checked(__result * 2);
    }
}
