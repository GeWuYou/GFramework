// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Content;

/// <summary>Discovers local directory packages and fingerprints all their files before resolution.</summary>
public sealed class ContentPackDiscovery : IContentPackSource
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 32
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentPack>> DiscoverAsync(IEnumerable<string> roots,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roots);
        var packages = new List<ContentPack>();
        var visited = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var root in roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(root))
                throw new ArgumentException("Discovery roots cannot contain empty paths.", nameof(roots));
            var inspectedPath = root;
            try
            {
                if (!Directory.Exists(root))
                    continue;
                ContentPack.RejectLink(root);
                var directories = File.Exists(Path.Combine(root, "content-pack.json"))
                    ? new[] { root }
                    : Directory.GetDirectories(root).OrderBy(static path => path, StringComparer.Ordinal).ToArray();
                foreach (var directory in directories)
                {
                    inspectedPath = Path.GetFullPath(directory);
                    ContentPack.RejectLink(inspectedPath);
                    if (!visited.Add(inspectedPath) || !File.Exists(Path.Combine(inspectedPath, "content-pack.json")))
                        continue;
                    packages.Add(await ReadPackageAsync(inspectedPath, cancellationToken).ConfigureAwait(false));
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new ContentLoadException(new ContentLoadDiagnostic
                {
                    Code = "InvalidPackagePath", FilePath = inspectedPath, Detail = exception.Message
                }, exception);
            }
        }
        return packages.AsReadOnly();
    }

    private static async Task<ContentPack> ReadPackageAsync(string root, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(root, "content-pack.json");
        ContentPackManifest? manifest = null;
        try
        {
            ContentPack.RejectLink(manifestPath);
            if (new FileInfo(manifestPath).Length > 1024 * 1024)
                throw new IOException("A content manifest cannot exceed 1 MiB.");
            var text = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
            manifest = JsonSerializer.Deserialize<ContentPackManifest>(text, JsonOptions)
                ?? throw new InvalidOperationException("A content manifest cannot be null.");
            ValidateManifest(manifest);
            var fingerprint = await ComputeFingerprintAsync(root, cancellationToken).ConfigureAwait(false);
            var package = new ContentPack(root, manifest, fingerprint);
            foreach (var path in manifest.ContentFiles.Concat(manifest.ResourcePacks).Concat(manifest.Code is null ? Array.Empty<string>() : new[] { manifest.Code.AssemblyPath }))
            {
                if (!File.Exists(package.GetFilePath(path)))
                    throw new FileNotFoundException("A declared content file was not found.", path);
            }
            return package;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or
            ArgumentException or InvalidOperationException)
        {
            throw new ContentLoadException(new ContentLoadDiagnostic
            {
                Code = "InvalidManifest", PackId = manifest?.Id, PackVersion = manifest?.Version,
                FilePath = manifestPath, Detail = $"Cannot discover package: {exception.Message}"
            }, exception);
        }
    }

    /// <summary>Validates declarations both for disk discovery and explicit host-created descriptors.</summary>
    /// <param name="manifest">Manifest to check.</param>
    internal static void ValidateManifest(ContentPackManifest manifest)
    {
        if (manifest.FormatVersion != 2 || !ContentId.IsCanonicalSegment(manifest.Id) ||
            string.IsNullOrWhiteSpace(manifest.Name) || string.IsNullOrWhiteSpace(manifest.GameId))
            throw new ArgumentException("A package needs formatVersion 2, canonical id, name and gameId.", nameof(manifest));
        _ = ContentVersionRules.Parse(manifest.Version);
        ContentVersionRules.Validate(manifest.GameVersions);
        ContentVersionRules.Validate(manifest.ContentApiVersions);
        if (manifest.SaveCompatibleVersions is not null)
            ContentVersionRules.Validate(manifest.SaveCompatibleVersions);
        ArgumentNullException.ThrowIfNull(manifest.Dependencies, nameof(manifest));
        ArgumentNullException.ThrowIfNull(manifest.IncompatiblePackages, nameof(manifest));
        ArgumentNullException.ThrowIfNull(manifest.ContentFiles, nameof(manifest));
        ArgumentNullException.ThrowIfNull(manifest.ResourcePacks, nameof(manifest));
        if (manifest.Code is { } code)
        {
            ContentPack.ValidateRelativePath(code.AssemblyPath);
            if (!code.AssemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(code.EntryType) || code.EntryType.Length > 512 ||
                code.EntryType.Any(static character => !(char.IsLetterOrDigit(character) || character is '.' or '_' or '+')))
                throw new ArgumentException("Code requires a managed DLL and an exact entry type name.", nameof(manifest));
            ContentVersionRules.Validate(code.ApiVersions);
            ArgumentNullException.ThrowIfNull(code.RequiredCapabilities, nameof(manifest));
            if (code.RequiredCapabilities.Any(static name => !ContentId.IsCanonicalSegment(name)) ||
                code.RequiredCapabilities.Distinct(StringComparer.Ordinal).Count() != code.RequiredCapabilities.Count)
                throw new ArgumentException("Code capabilities must have unique canonical names.", nameof(manifest));
        }
        var dependencyIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dependency in manifest.Dependencies)
        {
            if (dependency is null || !ContentId.IsCanonicalSegment(dependency.Id) || !dependencyIds.Add(dependency.Id))
                throw new ArgumentException("Package dependencies must have unique canonical IDs.", nameof(manifest));
            ContentVersionRules.Validate(dependency.Versions);
        }
        foreach (var incompatible in manifest.IncompatiblePackages)
            if (!ContentId.IsCanonicalSegment(incompatible))
                throw new ArgumentException("Incompatible package IDs must be canonical.", nameof(manifest));
        foreach (var path in manifest.ContentFiles.Concat(manifest.ResourcePacks))
            ContentPack.ValidateRelativePath(path);
        if (manifest.ContentFiles.Distinct(StringComparer.Ordinal).Count() != manifest.ContentFiles.Count ||
            manifest.ResourcePacks.Distinct(StringComparer.Ordinal).Count() != manifest.ResourcePacks.Count)
            throw new ArgumentException("Content entrypoint paths cannot be duplicated.", nameof(manifest));
        if (manifest.ContentFiles.Any(static path => !path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)) ||
            manifest.ResourcePacks.Any(static path => !path.EndsWith(".pck", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Content files must be YAML and native resource packs must be PCK.", nameof(manifest));
    }

    /// <summary>Recomputes the installed file snapshot, including assets, before a content session is published.</summary>
    /// <param name="root">Package root.</param>
    /// <param name="cancellationToken">Cancels fingerprint IO.</param>
    /// <returns>Canonical SHA-256 fingerprint.</returns>
    internal static async Task<string> ComputeFingerprintAsync(string root, CancellationToken cancellationToken)
    {
        ContentPack.RejectLink(root);
        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        var entryCount = 0;
        while (pending.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                ContentPack.RejectLink(entry);
                if (Directory.Exists(entry)) pending.Push(entry);
                else files.Add(entry);
                if (++entryCount > 10000)
                    throw new IOException("A package cannot exceed 10000 files and directories.");
            }
        }
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[64 * 1024];
        foreach (var file in files.OrderBy(path => Path.GetRelativePath(root, path).Replace('\\', '/'), StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            hash.AppendData(Encoding.UTF8.GetBytes(relative + "\0"));
            var length = new FileInfo(file).Length;
            hash.AppendData(Encoding.UTF8.GetBytes(length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0"));
            var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, true);
            await using (stream.ConfigureAwait(false))
            {
                int count;
                while ((count = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                    hash.AppendData(buffer.AsSpan(0, count));
            }
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
}
