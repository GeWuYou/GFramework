// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Runtime.Loader;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Content;
using GFramework.Game.Modding;

namespace GFramework.Game.Tests.Modding;

/// <summary>Guards self-contained applications where BCL and application dependencies occupy the same directory.</summary>
[TestFixture]
[NonParallelizable]
public sealed class GameModPlatformAssemblyTests
{
    /// <summary>App/package entries are not platform assets even when they are in TPA beside the runtime DLLs.</summary>
    [Test]
    public void ColocatedDependencies_AreExcludedAndPrivateVersionsWin()
    {
        using var workspace = new ColocatedWorkspace();
        var platform = workspace.ReadPlatform();
        Assert.That(platform, Does.Contain("System.Runtime"));
        Assert.That(platform, Does.Not.Contain("FixturePrivate"));
        Assert.That(platform, Does.Not.Contain("Newtonsoft.Json"));
        // Host version 1 is genuinely loaded in the default context. The private mod still needs version 2.
        var hostPrivate = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(static assembly =>
            string.Equals(assembly.GetName().Name, "FixturePrivate", StringComparison.Ordinal));
        if (hostPrivate is null)
        {
            using var stream = File.OpenRead(Path.Combine(workspace.RuntimeDirectory, "FixturePrivate.dll"));
            hostPrivate = AssemblyLoadContext.Default.LoadFromStream(stream);
        }
        Assert.That(hostPrivate.GetName().Version!.Major, Is.EqualTo(1));
        var context = workspace.CreateContext(platform);
        var isolated = context.LoadFromAssemblyName(new AssemblyName("FixturePrivate"));
        Assert.That(isolated.GetName().Version!.Major, Is.EqualTo(2));
        Assert.That(AssemblyLoadContext.GetLoadContext(isolated), Is.SameAs(context));
        Assert.That(isolated.GetType("FixturePrivate.VersionInfo")!.GetProperty("Value")!.GetValue(null), Is.EqualTo(2));

        // Without a private copy, a host application dependency must not silently satisfy the request.
        File.Delete(Path.Combine(workspace.ModDirectory, "FixturePrivate.dll"));
        var missing = workspace.CreateContext(platform);
        Assert.Throws<FileNotFoundException>(() => missing.LoadFromAssemblyName(new AssemblyName("FixturePrivate")));
    }

    /// <summary>Unidentified runtime metadata fails with an actionable error instead of sharing all app DLLs.</summary>
    [Test]
    public void MissingRuntimeMetadata_FailsClosed()
    {
        var error = Assert.Throws<InvalidOperationException>(() => GameModPlatformAssemblies.Read(
            ["/host/Newtonsoft.Json.dll"], []));
        Assert.That(error!.Message, Does.Contain("runtime-pack"));
    }

    /// <summary>Creates runtime-pack metadata with an empty neutral target and populated RID target.</summary>
    private sealed class ColocatedWorkspace : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "gframework-colocated-" + Guid.NewGuid().ToString("N"));
        /// <summary>Creates fake co-location metadata around real compiled private dependency artifacts.</summary>
        public ColocatedWorkspace()
        {
            Directory.CreateDirectory(RuntimeDirectory);
            Directory.CreateDirectory(ModDirectory);
            var fixtures = Path.Combine(TestContext.CurrentContext.TestDirectory, "ModFixtures");
            File.Copy(Path.Combine(fixtures, "one", "FixturePrivate.dll"), Path.Combine(RuntimeDirectory, "FixturePrivate.dll"));
            foreach (var file in Directory.GetFiles(Path.Combine(fixtures, "two")))
                File.Copy(file, Path.Combine(ModDirectory, Path.GetFileName(file)));
            foreach (var name in new[] { "System.Runtime", "System.Private.CoreLib", "Newtonsoft.Json" })
                File.WriteAllText(Path.Combine(RuntimeDirectory, name + ".dll"), string.Empty);
            File.WriteAllText(Path.Combine(RuntimeDirectory, "Game.deps.json"), """
                {
                  "libraries": {
                    "runtimepack.Microsoft.NETCore.App.Runtime.linux-x64/8.0.33": { "type": "runtimepack" },
                    "Game/1.0.0": { "type": "project" },
                    "FixturePrivate/1.0.0": { "type": "package" },
                    "Newtonsoft.Json/13.0.4": { "type": "package" }
                  },
                  "targets": {
                    ".NETCoreApp,Version=v8.0": {},
                    ".NETCoreApp,Version=v8.0/linux-x64": {
                      "runtimepack.Microsoft.NETCore.App.Runtime.linux-x64/8.0.33": {
                        "runtime": { "System.Runtime.dll": {}, "System.Private.CoreLib.dll": {} }
                      },
                      "FixturePrivate/1.0.0": { "runtime": { "FixturePrivate.dll": {} } },
                      "Newtonsoft.Json/13.0.4": { "runtime": { "Newtonsoft.Json.dll": {} } }
                    }
                  }
                }
                """);
        }
        /// <summary>Gets the self-contained host's shared physical directory.</summary>
        public string RuntimeDirectory => Path.Combine(_root, "runtime");
        /// <summary>Gets the independent mod directory with its own version of the host's dependency.</summary>
        public string ModDirectory => Path.Combine(_root, "mod");
        /// <summary>Reads platform ownership from metadata instead of inferring it from directory placement.</summary>
        public IReadOnlySet<string> ReadPlatform() => GameModPlatformAssemblies.Read(
            Directory.GetFiles(RuntimeDirectory, "*.dll"), Directory.GetFiles(RuntimeDirectory, "*.deps.json"));
        /// <summary>Creates a real dependency resolver with the tested platform classification.</summary>
        public GameModLoadContext CreateContext(IReadOnlySet<string> platform) => new(
            new ContentPack(ModDirectory, new ContentPackManifest { Id = "mod", Name = "Mod", GameId = "test" }, "snapshot"),
            Path.Combine(ModDirectory, "FixtureMod.dll"), new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase), platform);
        /// <inheritdoc />
        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
