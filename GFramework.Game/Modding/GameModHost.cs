// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Modding;
using GFramework.Game.Content;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.Modding;

/// <summary>Prepares trusted DLLs once, then starts and stops game-owned extension contracts on the host thread.</summary>
/// <remarks>Prepare before native resource loading; start after the catalog and native resources validate.
/// Stop gameplay before stopping this host. Host service providers and dispatcher remain caller-owned.
/// A changed selection or failed startup requires a new process, not an attempted hot reload.</remarks>
public sealed class GameModHost : IAsyncDisposable
{
    private readonly GameModHostOptions _options;
    private readonly IReadOnlyList<IGameModExtension> _extensions;
    private readonly List<LoadedMod> _mods = [];
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private HostState _state = HostState.Prepared;

    /// <summary>Creates a private session that cannot be used until preparation completes.</summary>
    private GameModHost(ContentLoadPlan plan, GameModHostOptions options, IReadOnlyList<IGameModExtension> extensions)
    {
        _options = options;
        _extensions = extensions;
        Admission = new CodeModAdmission(plan, this);
    }
    /// <summary>Gets the proof required by ContentCatalogLoader when a plan contains code.</summary>
    public CodeModAdmission Admission
    {
        get;
    }
    /// <summary>Gets whether all selected mods reached startup.</summary>
    public bool IsStarted => _state == HostState.Started;
    /// <summary>Gets whether admission still belongs to a usable preparing or running session.</summary>
    internal bool IsUsable => _state is HostState.Prepared or HostState.Starting or HostState.Started;
    /// <summary>Gets approved code packages in dependency-resolved order.</summary>
    public IReadOnlyList<ContentPackStamp> Packages => Array.AsReadOnly(_mods.Select(static mod => mod.Stamp).ToArray());

    /// <summary>Approves the whole selection before loading any DLL; no mod lifecycle callbacks run here.</summary>
    /// <param name="plan">One complete dependency-resolved content plan.</param>
    /// <param name="options">Game-owned code policy, shared APIs and optional adapters.</param>
    /// <param name="cancellationToken">Cancels admission and fingerprint IO.</param>
    /// <returns>A prepared host; call StartAsync only after native resources are prepared.</returns>
    public static async Task<GameModHost> PrepareAsync(ContentLoadPlan plan, GameModHostOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.GameId, nameof(options));
        _ = ContentVersionRules.Parse(options.CodeApiVersion);
        ArgumentNullException.ThrowIfNull(options.Services, nameof(options));
        ArgumentNullException.ThrowIfNull(options.Dispatcher, nameof(options));
        ArgumentNullException.ThrowIfNull(options.LoggerFactory, nameof(options));
        ArgumentNullException.ThrowIfNull(options.Extensions, nameof(options));
        ArgumentNullException.ThrowIfNull(options.SharedAssemblies, nameof(options));
        var extensions = options.Extensions.ToArray();
        if (extensions.Any(static extension => extension is null || !ContentId.IsCanonicalSegment(extension.Capability)) ||
            extensions.Select(static extension => extension.Capability).Distinct(StringComparer.Ordinal).Take(extensions.Length + 1).Count() != extensions.Length)
            throw new ArgumentException("Mod extension capabilities must be unique canonical names.", nameof(options));
        var capabilities = extensions.Select(static extension => extension.Capability).ToHashSet(StringComparer.Ordinal);
        var selected = plan.Packages.Where(static pack => pack.Manifest.Code is not null).ToArray();
        foreach (var pack in selected)
            await ApprovePackageAsync(pack, options, capabilities, cancellationToken).ConfigureAwait(false);
        var shared = BuildSharedAssemblies(options.SharedAssemblies, extensions);
        var host = new GameModHost(plan, options, Array.AsReadOnly(extensions));
        foreach (var pack in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var code = pack.Manifest.Code!;
                var path = pack.GetFilePath(code.AssemblyPath);
                var context = new GameModLoadContext(pack, path, shared);
                var assembly = context.LoadFromAssemblyPath(path);
                var type = assembly.GetType(code.EntryType, throwOnError: true, ignoreCase: false)!;
                if (!type.IsPublic || type.IsAbstract || type.ContainsGenericParameters ||
                    !typeof(IGameMod).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) is null)
                    throw new InvalidOperationException("The exact entry type must be public, concrete and implement IGameMod with a public parameterless constructor.");
                host._mods.Add(new LoadedMod(pack, type));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                host._state = HostState.Failed;
                throw Failure("CodeLoadFailed", pack, exception.Message, exception);
            }
        }
        return host;
    }

    /// <summary>Initializes and starts selected mods in effective order; partial failure cleans every entered scope.</summary>
    /// <param name="cancellationToken">Cancels admission and cooperative mod startup.</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_state == HostState.Started)
                return;
            if (_state != HostState.Prepared)
                throw new InvalidOperationException("This mod session cannot start; restart the process.");
            _state = HostState.Starting;
            try
            {
                await _options.Dispatcher.InvokeAsync(async () =>
                {
                    foreach (var mod in _mods)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        mod.Context = new GameModContext(mod.Stamp, _options);
                        try
                        {
                            mod.Context.Configure(_options, _extensions);
                            mod.Instance = (IGameMod)Activator.CreateInstance(mod.EntryType)!;
                            await mod.Instance.InitializeAsync(mod.Context, cancellationToken).ConfigureAwait(true);
                            await mod.Instance.StartAsync(cancellationToken).ConfigureAwait(true);
                        }
                        catch (Exception exception) { throw new GameModException(mod.Stamp.Id, "startup", exception); }
                    }
                }, cancellationToken).ConfigureAwait(false);
                _state = HostState.Started;
            }
            catch (Exception startupFailure)
            {
                _state = HostState.Failed;
                try
                {
                    await CleanupOnDispatcherAsync().ConfigureAwait(false);
                }
                catch (Exception cleanupFailure) { throw new AggregateException("Mod startup and cleanup failed.", startupFailure, cleanupFailure); }
                throw;
            }
        }
        finally { _lifecycle.Release(); }
    }

    /// <summary>Stops each entered mod and scope in reverse order, continuing after cleanup failures.</summary>
    /// <param name="cancellationToken">Cancels admission only; accepted cleanup always finishes.</param>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_state == HostState.Stopped)
                return;
            _state = HostState.Stopped;
            await CleanupOnDispatcherAsync().ConfigureAwait(false);
        }
        finally { _lifecycle.Release(); }
    }
    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(StopAsync());

    /// <summary>Joins cleanup on the native host thread without abandoning callbacks after cancellation.</summary>
    private Task CleanupOnDispatcherAsync() => _options.Dispatcher.InvokeAsync(async () =>
    {
        var errors = new List<Exception>();
        for (var index = _mods.Count - 1; index >= 0; index--)
        {
            var mod = _mods[index];
            var instance = mod.Instance;
            var context = mod.Context;
            mod.Instance = null;
            mod.Context = null;
            if (instance is not null)
            {
                try
                {
                    await instance.StopAsync(CancellationToken.None).ConfigureAwait(true);
                }
                catch (Exception exception) { errors.Add(new GameModException(mod.Stamp.Id, "stop", exception)); }
            }
            if (context is not null)
            {
                try
                {
                    await context.DisposeAsync().ConfigureAwait(true);
                }
                catch (Exception exception) { errors.Add(new GameModException(mod.Stamp.Id, "resource cleanup", exception)); }
            }
        }
        if (errors.Count != 0)
            throw new AggregateException("Mod shutdown failed.", errors);
    });

    /// <summary>Checks capabilities, trust and actual bytes before any selected mod assembly is loaded.</summary>
    private static async Task ApprovePackageAsync(ContentPack pack, GameModHostOptions options,
        HashSet<string> capabilities, CancellationToken cancellationToken)
    {
        var code = pack.Manifest.Code!;
        if (!string.Equals(pack.Manifest.GameId, options.GameId, StringComparison.Ordinal) ||
            !ContentVersionRules.Contains(options.CodeApiVersion, code.ApiVersions))
            throw Failure("CodeApiIncompatible", pack, "The code API or game identity is incompatible.");
        foreach (var required in code.RequiredCapabilities)
            if (!capabilities.Contains(required))
                throw Failure("MissingCodeCapability", pack, "Missing required capability: " + required);
        var stamp = new ContentPackStamp(pack.Manifest.Id, pack.Manifest.Version, pack.Fingerprint);
        if (options.TrustPolicy is null || !options.TrustPolicy(stamp))
            throw Failure("CodeNotApproved", pack, "This exact code package version and fingerprint are not approved.");
        var fingerprint = await ContentPackDiscovery.ComputeFingerprintAsync(pack.RootPath, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(pack.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw Failure("PackageChanged", pack, "Package bytes changed after discovery; discover and approve again.");
        _ = pack.GetFilePath(code.AssemblyPath);
    }

    /// <summary>Fixes shared contract identity, refusing conflicting copies before user code can execute.</summary>
    private static Dictionary<string, Assembly> BuildSharedAssemblies(IReadOnlyList<Assembly> assemblies,
        IReadOnlyList<IGameModExtension> extensions)
    {
        var shared = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        // IServiceProvider lives in System.ComponentModel. Self-contained engine hosts can load that
        // facade into their own ALC, so its defining assembly is part of this public ABI, just like ILogger.
        // Falling back to the default-context copy would give get_Services a different return type identity.
        var required = new[] { typeof(IGameMod).Assembly, typeof(GameModHost).Assembly, typeof(ILogger).Assembly,
            typeof(IServiceProvider).Assembly, typeof(GFramework.Core.Abstractions.Events.IUnRegister).Assembly, };
        foreach (var assembly in required.Concat(assemblies).Concat(extensions.SelectMany(static extension => extension.SharedAssemblies)))
        {
            ArgumentNullException.ThrowIfNull(assembly, nameof(assemblies));
            var name = assembly.GetName().Name ?? throw new ArgumentException("Shared assembly has no name.", nameof(assemblies));
            if (shared.TryGetValue(name, out var existing) && !ReferenceEquals(existing, assembly))
                throw new ArgumentException("Shared API contains conflicting assembly identities: " + name, nameof(assemblies));
            shared[name] = assembly;
        }
        return shared;
    }
    /// <summary>Builds structured package-local admission diagnostics.</summary>
    private static ContentLoadException Failure(string code, ContentPack pack, string detail, Exception? inner = null)
        => new(new ContentLoadDiagnostic { Code = code, PackId = pack.Manifest.Id, PackVersion = pack.Manifest.Version, Detail = detail }, inner);

    /// <summary>Records the process-lifetime entry type and at most one entered lifecycle scope.</summary>
    private sealed class LoadedMod
    {
        /// <summary>Captures immutable public package identity and the validated entry type.</summary>
        internal LoadedMod(ContentPack pack, Type entryType)
        {
            Stamp = new ContentPackStamp(pack.Manifest.Id, pack.Manifest.Version, pack.Fingerprint);
            EntryType = entryType;
        }
        /// <summary>Gets the approved package identity.</summary>
        internal ContentPackStamp Stamp
        {
            get;
        }
        /// <summary>Gets the exact validated mod type.</summary>
        internal Type EntryType
        {
            get;
        }
        /// <summary>Gets or sets the instance after constructor admission.</summary>
        internal IGameMod? Instance
        {
            get; set;
        }
        /// <summary>Gets or sets the scope as soon as initialization enters, including partial failure.</summary>
        internal GameModContext? Context
        {
            get; set;
        }
    }
    /// <summary>Encodes the single-start process-lifetime state machine.</summary>
    private enum HostState
    {
        Prepared, Starting, Started, Failed, Stopped,
    }
}
