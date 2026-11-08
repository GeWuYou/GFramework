// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.Loader;
using GFramework.Game.Content;

namespace GFramework.Game.Modding;

/// <summary>Isolates private managed dependencies while preserving explicitly shared host contract identity.</summary>
/// <remarks>The context is intentionally non-collectible: trusted code and patching are process-lifetime.
/// Resolver paths remain package-scoped; this boundary is dependency hygiene, not a security sandbox.</remarks>
internal sealed class GameModLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly IReadOnlyDictionary<string, Assembly> _shared;
    private readonly IReadOnlySet<string> _platform;
    private readonly ContentPack _package;
    private readonly string _entryDirectory;

    /// <summary>Captures a validated entry path and a fixed host assembly map.</summary>
    internal GameModLoadContext(ContentPack package, string entryPath, IReadOnlyDictionary<string, Assembly> shared,
        IReadOnlySet<string>? platformAssemblies = null)
        : base("GameMod:" + package.Manifest.Id + ":" + Guid.NewGuid().ToString("N"), isCollectible: false)
    {
        _package = package;
        _entryDirectory = Path.GetDirectoryName(entryPath)!;
        _resolver = new AssemblyDependencyResolver(entryPath);
        _shared = shared;
        _platform = platformAssemblies ?? GameModPlatformAssemblies.Discover();
    }
    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? throw new FileLoadException("A mod dependency has no assembly name.");
        if (_shared.TryGetValue(name, out var shared))
        {
            var actual = shared.GetName();
            if (assemblyName.Version is not null && actual.Version is not null && actual.Version < assemblyName.Version)
                throw new FileLoadException("The shared host assembly is older than the mod requires: " + name);
            return shared;
        }
        var resolved = _resolver.ResolveAssemblyToPath(assemblyName);
        // Publish output normally carries deps.json. A plain DLL with sibling private DLLs is also supported.
        if (resolved is null)
        {
            ContentPack.ValidateRelativePath(name + ".dll");
            var sibling = Path.Combine(_entryDirectory, name + ".dll");
            if (File.Exists(sibling))
                resolved = sibling;
        }
        if (resolved is not null)
        {
            var relative = Path.GetRelativePath(_package.RootPath, resolved).Replace('\\', '/');
            return LoadFromAssemblyPath(_package.GetFilePath(relative));
        }
        // A NuGet dependency can legitimately supply a newer package version of a framework library.
        // Private resolution precedes genuine platform fallback, following the standard .NET plugin pattern.
        if (_platform.Contains(name))
            return null;
        throw new FileNotFoundException("Private mod dependency was not found inside its package, and is not explicitly shared: " + name);
    }
    /// <inheritdoc />
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var resolved = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (resolved is null)
            return nint.Zero;
        var relative = Path.GetRelativePath(_package.RootPath, resolved).Replace('\\', '/');
        return LoadUnmanagedDllFromPath(_package.GetFilePath(relative));
    }
}
