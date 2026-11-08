// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;

namespace DodgeTheCreeps.Advanced;

/// <summary>Immutable launch inputs determine data isolation before any feature service is constructed.</summary>
/// <param name="Arguments">Native user arguments.</param>
/// <param name="InstanceId">A fresh bounded identity for initial smoke/capture launches.</param>
/// <param name="DataRoot">The explicit Godot storage namespace.</param>
internal sealed record GameLaunchOptions(string[] Arguments, string InstanceId, string DataRoot)
{
    /// <summary>Resolves only validated GUID continuations; command-line input cannot select arbitrary player paths.</summary>
    /// <param name="arguments">Godot user arguments.</param>
    /// <returns>Inputs shared by composition and native acceptance.</returns>
    internal static GameLaunchOptions Parse(string[] arguments)
    {
        var id = Guid.NewGuid().ToString("N");
        var continuation = arguments.FirstOrDefault(static value => value.Contains("-smoke-continue=", StringComparison.Ordinal) ||
            value.StartsWith("--smoke-continue=", StringComparison.Ordinal));
        if (continuation is not null)
        {
            var value = continuation[(continuation.IndexOf('=', StringComparison.Ordinal) + 1)..].Split(':')[0];
            if (!Guid.TryParseExact(value, "N", out _))
                throw new ArgumentException("Acceptance identity must be a 32-digit GUID.", nameof(arguments));
            id = value;
        }
        var smoke = arguments.Any(static value => value.Contains("-smoke", StringComparison.Ordinal) ||
            string.Equals(value, "--smoke", StringComparison.Ordinal));
        var capture = arguments.Any(static value => value.StartsWith("--capture-", StringComparison.Ordinal) &&
            !value.StartsWith("--capture-language=", StringComparison.Ordinal));
        var root = smoke ? "user://dodge_advanced_smoke/" + id :
            capture ? "user://dodge_advanced_capture/" + id : "user://dodge_the_creeps_advanced";
        return new GameLaunchOptions(arguments, id, root);
    }
}
