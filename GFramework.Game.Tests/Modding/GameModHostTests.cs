// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text.Json;
using System.Threading;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using GFramework.Game.Abstractions.Modding;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Content;
using GFramework.Game.Modding;

namespace GFramework.Game.Tests.Modding;

/// <summary>Exercises compiled external DLLs, real private dependency identity, approval and lifecycle rollback.</summary>
[TestFixture]
public sealed class GameModHostTests
{
    /// <summary>Different mod contexts load different versions of the same private dependency while sharing contracts.</summary>
    [Test]
    public async Task Startup_IsolatesPrivateDependenciesAndCleansInReverseOrder()
    {
        using var workspace = new ModWorkspace();
        workspace.AddMod("alpha", "one");
        workspace.AddMod("beta", "two");
        var plan = await workspace.ResolveAsync("alpha", "beta").ConfigureAwait(false);
        var observed = new List<string>();
        var host = await GameModHost.PrepareAsync(plan, Options(observed)).ConfigureAwait(false);
        await using var cleanup = host.ConfigureAwait(false);
        Assert.That(observed, Is.Empty, "Preparing code must not invoke lifecycle callbacks.");
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ContentCatalogLoader().LoadAsync(plan)).ConfigureAwait(false);
        var catalog = await new ContentCatalogLoader().LoadAsync(plan, host.Admission).ConfigureAwait(false);
        Assert.That(catalog.Snapshot.Packs.Select(static stamp => stamp.Id), Is.EqualTo(new[] { "base", "alpha", "beta" }));
        await host.StartAsync().ConfigureAwait(false);
        await host.StartAsync().ConfigureAwait(false);
        await host.StopAsync().ConfigureAwait(false);
        await host.StopAsync().ConfigureAwait(false);
        Assert.That(observed, Is.EqualTo(new[] { "alpha:init:1", "alpha:start", "beta:init:2", "beta:start",
            "beta:stop", "beta:dispose", "alpha:stop", "alpha:dispose" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ContentCatalogLoader().LoadAsync(plan, host.Admission)).ConfigureAwait(false);
    }

    /// <summary>Self-contained engine hosts may own a separate IServiceProvider assembly; external mod calls must share it.</summary>
    [Test]
    public async Task CustomHostContext_PreservesServiceProviderContractIdentity()
    {
        using var workspace = new ModWorkspace();
        workspace.AddMod("alpha", "one");
        var custom = new EngineHostContext();
        var game = custom.LoadFromAssemblyPath(typeof(GameModHost).Assembly.Location);
        var contracts = custom.LoadFromAssemblyPath(typeof(IGameMod).Assembly.Location);
        var contextType = contracts.GetType(typeof(IGameModContext).FullName!, throwOnError: true)!;
        Assert.That(contextType.GetProperty("Services")!.PropertyType, Is.Not.SameAs(typeof(IServiceProvider)),
            "The fixture must reproduce the engine's separate System.ComponentModel identity.");

        var discoveryType = game.GetType(typeof(ContentPackDiscovery).FullName!, throwOnError: true)!;
        var discovery = Activator.CreateInstance(discoveryType)!;
        var packages = await InvokeResultAsync(discoveryType.GetMethod("DiscoverAsync")!.Invoke(discovery,
            new object[] { new[] { workspace.Root }, CancellationToken.None })!).ConfigureAwait(false);
        var profile = Activator.CreateInstance(contracts.GetType(typeof(ContentPackProfile).FullName!, true)!)!;
        profile.GetType().GetProperty("EnabledPackages")!.SetValue(profile, new[] { "alpha" });
        var compatibility = Activator.CreateInstance(contracts.GetType(typeof(ContentHostCompatibility).FullName!, true)!)!;
        compatibility.GetType().GetProperty("GameId")!.SetValue(compatibility, "test");
        compatibility.GetType().GetProperty("CodeApiVersion")!.SetValue(compatibility, "1.0.0");
        var plan = game.GetType(typeof(ContentPackResolver).FullName!, true)!.GetMethod("Resolve")!.Invoke(null,
            new[] { packages, profile, compatibility })!;
        var options = Activator.CreateInstance(game.GetType(typeof(GameModHostOptions).FullName!, true)!)!;
        options.GetType().GetProperty("GameId")!.SetValue(options, "test");
        options.GetType().GetProperty("CodeApiVersion")!.SetValue(options, "1.0.0");
        var trust = options.GetType().GetProperty("TrustPolicy")!;
        var stamp = Expression.Parameter(trust.PropertyType.GetGenericArguments()[0], "stamp");
        trust.SetValue(options, Expression.Lambda(trust.PropertyType, Expression.Constant(true), stamp).Compile());
        var hostType = game.GetType(typeof(GameModHost).FullName!, true)!;
        var host = await InvokeResultAsync(hostType.GetMethod("PrepareAsync")!.Invoke(null,
            new[] { plan, options, (object)CancellationToken.None })!).ConfigureAwait(false);
        try
        {
            // BasicMod actually calls context.Services.GetService, so this catches a return-type ABI mismatch.
            await ((Task)hostType.GetMethod("StartAsync")!.Invoke(host, new object[] { CancellationToken.None })!).ConfigureAwait(false);
            Assert.That(hostType.GetProperty("IsStarted")!.GetValue(host), Is.True);
        }
        finally
        {
            await ((Task)hostType.GetMethod("StopAsync")!.Invoke(host, new object[] { CancellationToken.None })!).ConfigureAwait(false);
        }
    }

    /// <summary>Awaits a dynamically loaded host's generic task without resolving its game types in the default ALC.</summary>
    private static async Task<object> InvokeResultAsync(object value)
    {
        await ((Task)value).ConfigureAwait(false);
        return value.GetType().GetProperty("Result")!.GetValue(value)!;
    }

    /// <summary>Reproduces an engine ALC that owns contracts and colocated System.ComponentModel/netstandard facades.</summary>
    private sealed class EngineHostContext : AssemblyLoadContext
    {
        private readonly Dictionary<string, string> _assemblies = new(StringComparer.OrdinalIgnoreCase)
        {
            ["GFramework.Game"] = typeof(GameModHost).Assembly.Location,
            ["GFramework.Game.Abstractions"] = typeof(IGameMod).Assembly.Location,
            ["GFramework.Core.Abstractions"] = typeof(GFramework.Core.Abstractions.Events.IUnRegister).Assembly.Location,
            ["System.ComponentModel"] = typeof(IServiceProvider).Assembly.Location,
            ["netstandard"] = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "netstandard.dll")
        };
        /// <summary>Creates a distinct process-lifetime engine contract context.</summary>
        internal EngineHostContext() : base("EngineHost:" + Guid.NewGuid().ToString("N"), isCollectible: false) { }
        /// <inheritdoc />
        protected override Assembly? Load(AssemblyName assemblyName) =>
            _assemblies.TryGetValue(assemblyName.Name!, out var path) ? LoadFromAssemblyPath(path) : null;
    }

    /// <summary>A failing Initialize is stopped and its resources are cleaned before earlier mods.</summary>
    [Test]
    public async Task PartialInitializationFailure_CleansFailingAndEarlierMods()
    {
        using var workspace = new ModWorkspace();
        workspace.AddMod("alpha", "one");
        workspace.AddMod("failing", "two");
        var plan = await workspace.ResolveAsync("alpha", "failing").ConfigureAwait(false);
        var observed = new List<string>();
        var host = await GameModHost.PrepareAsync(plan, Options(observed)).ConfigureAwait(false);
        await using var cleanup = host.ConfigureAwait(false);
        var error = await Assert.ThrowsAsync<GameModException>(() => host.StartAsync()).ConfigureAwait(false);
        Assert.That(error!.PackId, Is.EqualTo("failing"));
        Assert.That(observed, Is.EqualTo(new[] { "alpha:init:1", "alpha:start", "failing:init:2",
            "failing:stop", "failing:dispose", "alpha:stop", "alpha:dispose" }));
        Assert.That(host.IsStarted, Is.False);
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync()).ConfigureAwait(false);
    }

    /// <summary>Cleanup failure from one participant does not skip later resources or earlier mod scopes.</summary>
    [TestCase("stop-failing")]
    [TestCase("dispose-failing")]
    public async Task CleanupFailure_ContinuesAndIsNotRepeated(string failingId)
    {
        using var workspace = new ModWorkspace();
        workspace.AddMod("alpha", "one");
        workspace.AddMod(failingId, "two");
        var plan = await workspace.ResolveAsync("alpha", failingId).ConfigureAwait(false);
        var observed = new List<string>();
        var host = await GameModHost.PrepareAsync(plan, Options(observed)).ConfigureAwait(false);
        await using var cleanup = host.ConfigureAwait(false);
        await host.StartAsync().ConfigureAwait(false);
        await Assert.ThrowsAsync<AggregateException>(() => host.StopAsync()).ConfigureAwait(false);
        Assert.That(observed.TakeLast(4), Is.EqualTo(new[] { failingId + ":stop", failingId + ":dispose", "alpha:stop", "alpha:dispose" }));
        var count = observed.Count;
        await host.StopAsync().ConfigureAwait(false);
        Assert.That(observed, Has.Count.EqualTo(count));
    }

    /// <summary>All approvals are checked before trying to load any selected entrypoint, including invalid images.</summary>
    [Test]
    public async Task UnapprovedSelection_DoesNotLoadEarlierCode()
    {
        using var workspace = new ModWorkspace();
        workspace.AddMod("alpha", "one");
        workspace.AddMod("beta", "two");
        File.WriteAllText(Path.Combine(workspace.Root, "alpha", "FixtureMod.dll"), "invalid image");
        var plan = await workspace.ResolveAsync("alpha", "beta").ConfigureAwait(false);
        var error = await Assert.ThrowsAsync<ContentLoadException>(() => GameModHost.PrepareAsync(plan, new GameModHostOptions
        {
            GameId = "test", CodeApiVersion = "1.0.0", TrustPolicy = static stamp => string.Equals(stamp.Id, "alpha", StringComparison.Ordinal)
        })).ConfigureAwait(false);
        Assert.That(error!.Diagnostic.Code, Is.EqualTo("CodeNotApproved"));
        Assert.That(error.Diagnostic.PackId, Is.EqualTo("beta"));
    }

    /// <summary>Mod bytes edited after discovery cannot reuse a previously approved stamp.</summary>
    [Test]
    public async Task ChangedBytes_AreRejectedBeforeCodeLoads()
    {
        using var workspace = new ModWorkspace();
        workspace.AddMod("alpha", "one");
        var plan = await workspace.ResolveAsync("alpha").ConfigureAwait(false);
        File.AppendAllText(Path.Combine(workspace.Root, "alpha", "FixtureMod.dll"), "edit");
        var error = await Assert.ThrowsAsync<ContentLoadException>(() => GameModHost.PrepareAsync(plan, Options(new List<string>()))).ConfigureAwait(false);
        Assert.That(error!.Diagnostic.Code, Is.EqualTo("PackageChanged"));
    }

    /// <summary>Required adapter capabilities and exact public entrypoint types fail explicitly.</summary>
    [Test]
    public async Task MissingCapabilityAndEntrypoint_ArePackageLocalFailures()
    {
        using var workspace = new ModWorkspace();
        workspace.AddMod("alpha", "one", capabilities: new[] { "harmony" });
        var plan = await workspace.ResolveAsync("alpha").ConfigureAwait(false);
        var error = await Assert.ThrowsAsync<ContentLoadException>(() => GameModHost.PrepareAsync(plan, Options(new List<string>()))).ConfigureAwait(false);
        Assert.That(error!.Diagnostic.Code, Is.EqualTo("MissingCodeCapability"));
        workspace.AddMod("alpha", "one", entryType: "FixtureMod.NotThere");
        plan = await workspace.ResolveAsync("alpha").ConfigureAwait(false);
        error = await Assert.ThrowsAsync<ContentLoadException>(() => GameModHost.PrepareAsync(plan, Options(new List<string>()))).ConfigureAwait(false);
        Assert.That(error!.Diagnostic.Code, Is.EqualTo("CodeLoadFailed"));
    }

    /// <summary>Admission cannot be borrowed by a distinct plan, even if package snapshots are equal.</summary>
    [Test]
    public async Task Admission_IsBoundToExactPlan()
    {
        using var workspace = new ModWorkspace();
        workspace.AddMod("alpha", "one");
        var first = await workspace.ResolveAsync("alpha").ConfigureAwait(false);
        var second = await workspace.ResolveAsync("alpha").ConfigureAwait(false);
        var host = await GameModHost.PrepareAsync(first, Options(new List<string>())).ConfigureAwait(false);
        await using var cleanup = host.ConfigureAwait(false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ContentCatalogLoader().LoadAsync(second, host.Admission)).ConfigureAwait(false);
    }

    /// <summary>Resolver requires an explicit compatible code API instead of accepting code as ordinary data.</summary>
    [Test]
    public async Task CodeApi_IsExplicitAndVersioned()
    {
        using var workspace = new ModWorkspace();
        workspace.AddMod("alpha", "one");
        var packages = await new ContentPackDiscovery().DiscoverAsync(new[] { workspace.Root }).ConfigureAwait(false);
        var profile = new ContentPackProfile { EnabledPackages = new[] { "alpha" } };
        Assert.That(Assert.Throws<ContentLoadException>(() => ContentPackResolver.Resolve(packages, profile,
            new ContentHostCompatibility { GameId = "test" }))!.Diagnostic.Code, Is.EqualTo("CodeApiIncompatible"));
        Assert.That(Assert.Throws<ContentLoadException>(() => ContentPackResolver.Resolve(packages, profile,
            new ContentHostCompatibility { GameId = "test", CodeApiVersion = "2.0.0" }))!.Diagnostic.Code,
            Is.EqualTo("CodeApiIncompatible"));
    }

    /// <summary>Supplies a host observer through standard services while approving exact discovered stamps.</summary>
    private static GameModHostOptions Options(List<string> observed) => new()
    {
        GameId = "test", CodeApiVersion = "1.0.0", TrustPolicy = static _ => true,
        Services = new ObserverServices(observed.Add)
    };

    /// <summary>Shares only a BCL delegate so fixture-private types never leak into the host contract.</summary>
    private sealed class ObserverServices(Action<string> observer) : IServiceProvider
    {
        /// <inheritdoc />
        public object? GetService(Type serviceType) => serviceType == typeof(Action<string>) ? observer : null;
    }

    /// <summary>Owns an isolated installation tree containing actual compiled mod artifacts.</summary>
    private sealed class ModWorkspace : IDisposable
    {
        /// <summary>Creates one mandatory data-only base package.</summary>
        public ModWorkspace()
        {
            Directory.CreateDirectory(Root);
            WriteManifest("base", null);
        }
        /// <summary>Gets this test installation root, never a real player profile.</summary>
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "gframework-mod-tests-" + Guid.NewGuid().ToString("N"));
        /// <summary>Installs a fixture and its dependency closure as an ordinary discovered package.</summary>
        public void AddMod(string id, string fixture, string entryType = "FixtureMod.BasicMod", string[]? capabilities = null)
        {
            var directory = Path.Combine(Root, id);
            Directory.CreateDirectory(directory);
            foreach (var file in Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "ModFixtures", fixture)))
                File.Copy(file, Path.Combine(directory, Path.GetFileName(file)), overwrite: true);
            WriteManifest(id, new ContentCodeEntrypoint
            {
                AssemblyPath = "FixtureMod.dll", EntryType = entryType,
                ApiVersions = new ContentVersionRange { MinimumInclusive = "1.0.0", MaximumExclusive = "2.0.0" },
                RequiredCapabilities = capabilities ?? Array.Empty<string>()
            });
        }
        /// <summary>Discovers fresh bytes and applies the requested player order with explicit code support.</summary>
        public async Task<ContentLoadPlan> ResolveAsync(params string[] enabled)
        {
            var packages = await new ContentPackDiscovery().DiscoverAsync(new[] { Root }).ConfigureAwait(false);
            return ContentPackResolver.Resolve(packages,
                new ContentPackProfile { EnabledPackages = enabled, LoadOrder = enabled },
                new ContentHostCompatibility { GameId = "test", CodeApiVersion = "1.0.0" });
        }
        private void WriteManifest(string id, ContentCodeEntrypoint? code)
        {
            var directory = Path.Combine(Root, id);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "content-pack.json"), JsonSerializer.Serialize(new ContentPackManifest
            {
                Id = id, Name = id, GameId = "test", Code = code
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
        /// <inheritdoc />
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
