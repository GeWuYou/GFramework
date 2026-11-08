// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using System.Text.Json;

namespace GFramework.Game.Modding;

/// <summary>Identifies genuine .NET runtime assemblies from runtime-pack metadata, never from application co-location.</summary>
/// <remarks>Self-contained hosts put framework and application DLLs together. TPA membership and physical directory
/// therefore cannot identify BCL ownership. Only managed assets declared by the actual NETCore runtime pack qualify.</remarks>
internal static class GameModPlatformAssemblies
{
    /// <summary>Reads the owning runtime metadata for both framework-dependent and self-contained desktop hosts.</summary>
    internal static IReadOnlySet<string> Discover()
    {
        var paths = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var directory = RuntimeEnvironment.GetRuntimeDirectory();
        return Read(paths, Directory.EnumerateFiles(directory, "*.deps.json", SearchOption.TopDirectoryOnly));
    }

    /// <summary>Intersects actual runtime-pack assets with TPA names; package and project assets never become platform.</summary>
    /// <param name="platformPaths">The host's trusted-platform assembly paths.</param>
    /// <param name="dependencyFiles">Host-owned deps files containing framework or runtime-pack library metadata.</param>
    /// <returns>A complete runtime-name set suitable only for fallback after private package resolution.</returns>
    internal static IReadOnlySet<string> Read(IEnumerable<string> platformPaths, IEnumerable<string> dependencyFiles)
    {
        var trusted = platformPaths.Select(static path => Path.GetFileNameWithoutExtension(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var runtimeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in dependencyFiles)
        {
            using var stream = File.OpenRead(file);
            using var document = JsonDocument.Parse(stream);
            ReadRuntimeAssets(document.RootElement, runtimeNames);
        }
        runtimeNames.IntersectWith(trusted);
        if (!runtimeNames.Contains("System.Private.CoreLib") || !runtimeNames.Contains("System.Runtime"))
            throw new InvalidOperationException("Cannot identify .NET runtime-pack assembly metadata for code mods. " +
                "Keep the framework Microsoft.NETCore.App.deps.json or the self-contained application .deps.json with runtime-pack assets beside the runtime.");
        return runtimeNames;
    }

    /// <summary>Visits every RID target because a neutral target may contain no runtime assets.</summary>
    private static void ReadRuntimeAssets(JsonElement root, HashSet<string> runtimeNames)
    {
        if (!root.TryGetProperty("libraries", out var libraries) || !root.TryGetProperty("targets", out var targets))
            return;
        var runtimeLibraries = new HashSet<string>(StringComparer.Ordinal);
        foreach (var library in libraries.EnumerateObject())
        {
            if (!library.Value.TryGetProperty("type", out var type))
                continue;
            var kind = type.GetString();
            if ((string.Equals(kind, "runtimepack", StringComparison.Ordinal) || string.Equals(kind, "package", StringComparison.Ordinal)) &&
                (library.Name.StartsWith("runtimepack.Microsoft.NETCore.App.Runtime.", StringComparison.Ordinal) ||
                 library.Name.StartsWith("Microsoft.NETCore.App.Runtime.", StringComparison.Ordinal)))
                runtimeLibraries.Add(library.Name);
        }
        foreach (var target in targets.EnumerateObject())
        {
            foreach (var library in target.Value.EnumerateObject())
            {
                if (!runtimeLibraries.Contains(library.Name) || !library.Value.TryGetProperty("runtime", out var assets))
                    continue;
                foreach (var asset in assets.EnumerateObject())
                    if (asset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        runtimeNames.Add(Path.GetFileNameWithoutExtension(asset.Name));
            }
        }
    }
}
