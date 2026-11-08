// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text.Json;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Content;

namespace GFramework.Game.Tests.Content;

/// <summary>Checks complete package discovery, dependency resolution, composition and diagnostic contracts.</summary>
[TestFixture]
public sealed class ContentRuntimeTests
{
    private static readonly ContentHostCompatibility Host = new() { GameId = "test", BasePackId = "base" };

    /// <summary>Canonical IDs remain independent of load order and reject ambiguous or path-like names.</summary>
    [TestCase("base:iron", true)]
    [TestCase("mod-one:item_2", true)]
    [TestCase("Base:iron", false)]
    [TestCase("base:../iron", false)]
    [TestCase("base:iron:extra", false)]
    [TestCase("base:", false)]
    public void ContentIds_AreCanonical(string value, bool valid)
    {
        if (!valid)
            Assert.Throws<ArgumentException>(() => new ContentId(value));
        else
        {
            var id = new ContentId(value);
            Assert.That(id.ToString(), Is.EqualTo(value));
            Assert.That(id, Is.EqualTo(new ContentId(value)));
        }
    }

    /// <summary>Bounds are explicit and numeric; no same-major save compatibility is guessed.</summary>
    [Test]
    public void Versions_ValidateNumericReleaseBounds()
    {
        var range = new ContentVersionRange { MinimumInclusive = "1.2.0", MaximumExclusive = "2.0.0" };
        Assert.Multiple(() =>
        {
            Assert.That(ContentVersionRules.Contains("1.2.0", range), Is.True);
            Assert.That(ContentVersionRules.Contains("1.10.0", range), Is.True);
            Assert.That(ContentVersionRules.Contains("2.0.0", range), Is.False);
            Assert.Throws<ArgumentException>(() => ContentVersionRules.Parse("1.2"));
            Assert.Throws<ArgumentException>(() => ContentVersionRules.Parse("1.2.0-beta"));
            Assert.Throws<ArgumentException>(() => ContentVersionRules.Parse("01.2.0"));
        });
    }

    /// <summary>Discovery does not activate optional packages; dependencies constrain player priority.</summary>
    [Test]
    public async Task Resolver_PreservesPlayerOrderWithinDependencyConstraints()
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        workspace.Add("library", "[]");
        workspace.Add("dependent", "[]", [new ContentPackDependency { Id = "library" }]);
        workspace.Add("other", "[]");
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        var basic = ContentPackResolver.Resolve(packages, new ContentPackProfile(), Host);
        Assert.That(basic.Packages.Select(static pack => pack.Manifest.Id), Is.EqualTo(["base"]));
        var plan = ContentPackResolver.Resolve(packages, new ContentPackProfile
        {
            EnabledPackages = ["dependent", "library", "other"],
            LoadOrder = ["dependent", "other", "library", "base"],
        }, Host);
        Assert.That(plan.Packages.Select(static pack => pack.Manifest.Id),
            Is.EqualTo(["base", "other", "library", "dependent"]));
    }

    /// <summary>Unmet dependencies and explicit incompatibilities fail with package-local diagnostics.</summary>
    [Test]
    public async Task Resolver_RejectsMissingVersionAndIncompatibilityFailures()
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        workspace.Add("needs", "[]", [new ContentPackDependency { Id = "library" }]);
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        AssertFailure("MissingDependency", () => ContentPackResolver.Resolve(packages,
            new ContentPackProfile { EnabledPackages = ["needs"] }, Host));
        AssertFailure("MissingPackage", () => ContentPackResolver.Resolve(packages,
            new ContentPackProfile { EnabledPackages = ["gone"] }, Host));
        workspace.Add("library", "[]");
        workspace.Add("needs", "[]", [ new ContentPackDependency
        {
            Id = "library", Versions = new ContentVersionRange { MinimumInclusive = "2.0.0" },
        } ]);
        packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        AssertFailure("DependencyVersionMismatch", () => ContentPackResolver.Resolve(packages,
            new ContentPackProfile { EnabledPackages = ["needs", "library"] }, Host));
        workspace.Add("needs", "[]", incompatible: ["library"]);
        packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        AssertFailure("IncompatiblePackages", () => ContentPackResolver.Resolve(packages,
            new ContentPackProfile { EnabledPackages = ["needs", "library"] }, Host));
    }

    /// <summary>Optional dependencies are optional only while absent; their enabled versions and cycles still matter.</summary>
    [Test]
    public async Task Resolver_OptionalDependenciesAndCyclesHaveExplicitSemantics()
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        workspace.Add("first", "[]", [new ContentPackDependency { Id = "second", Optional = true }]);
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        Assert.That(ContentPackResolver.Resolve(packages,
            new ContentPackProfile { EnabledPackages = ["first"] }, Host).Packages.Count, Is.EqualTo(2));
        workspace.Add("second", "[]", [new ContentPackDependency { Id = "first" }]);
        packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        AssertFailure("DependencyCycle", () => ContentPackResolver.Resolve(packages,
            new ContentPackProfile { EnabledPackages = ["first", "second"] }, Host));
    }

    /// <summary>Multiple installed versions never cause an implicit, potentially save-breaking selection.</summary>
    [Test]
    public async Task Resolver_RejectsDuplicatePackageIds()
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        AssertFailure("DuplicatePackage", () => ContentPackResolver.Resolve(packages.Concat(packages), new(), Host));
    }

    /// <summary>Field patches compose, array patches replace, and winner provenance follows player order.</summary>
    [Test]
    public async Task Loader_ComposesFieldsArraysAndPlayerOrder()
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        workspace.Add("fast", """
            - op: patch
              table: enemies
              id: base:walker
              value:
                speed: 180
                stats:
                  color: red
                weights: [9]
            """);
        workspace.Add("slow", """
            - op: patch
              table: enemies
              id: base:walker
              value:
                speed: 80
                stats:
                  armor: 5
                note: null
            """);
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        var catalog = await workspace.LoadAsync(packages, "fast", "slow").ConfigureAwait(false);
        var reference = new ContentReference("enemies", new ContentId("base:walker"));
        var row = catalog.GetTable<Enemy>("enemies").Get(reference.Id);
        Assert.Multiple(() =>
        {
            Assert.That(row.Speed, Is.EqualTo(80));
            Assert.That(row.Stats.Color, Is.EqualTo("red"));
            Assert.That(row.Stats.Armor, Is.EqualTo(5));
            Assert.That(row.Weights, Is.EqualTo([9]));
            Assert.That(row.Note, Is.Null);
            Assert.That(catalog.GetSources(reference).Select(static source => source.PackId),
                Is.EqualTo(["base", "fast", "slow"]));
            Assert.That(catalog.GetFieldSource(reference, "stats.color")?.PackId, Is.EqualTo("fast"));
            Assert.That(catalog.GetFieldSource(reference, "speed")?.PackId, Is.EqualTo("slow"));
            Assert.That(catalog.GetFieldSource(reference, "weights[0]")?.PackId, Is.EqualTo("fast"));
        });
        var reversed = await workspace.LoadAsync(packages, "slow", "fast").ConfigureAwait(false);
        Assert.That(reversed.GetTable<Enemy>("enemies").Get(reference.Id).Speed, Is.EqualTo(180));
        Assert.That(catalog.GetTable<Enemy>("enemies").Get(reference.Id).Speed, Is.EqualTo(80));
    }

    /// <summary>A whole-record replacement discards prior field provenance and old field values.</summary>
    [Test]
    public async Task Loader_ReplacementResetsNestedProvenance()
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        workspace.Add("replace", """
            - op: replace
              table: enemies
              id: base:walker
              value:
                id: base:walker
                speed: 70
                stats: {color: blue, armor: 7}
                weights: []
            """);
        var catalog = await workspace.LoadAsync(await workspace.DiscoverAsync().ConfigureAwait(false), "replace")
            .ConfigureAwait(false);
        var reference = new ContentReference("enemies", new ContentId("base:walker"));
        Assert.That(catalog.GetFieldSource(reference, "stats.color")?.PackId, Is.EqualTo("replace"));
        Assert.That(catalog.GetFieldSource(reference, "note"), Is.Null);
    }

    /// <summary>Creating or replacing an object records its container source without overwriting existing-object provenance.</summary>
    [TestCase("appearance: blue", "{}", "patch", null)]
    [TestCase("", "{right: blue}", "patch", "right")]
    [TestCase("appearance: {left: gray}", "{right: blue}", "base", "right")]
    public async Task Loader_ObjectCreationTracksContainerProvenance(string original, string patch,
        string containerOwner, string? patchedField)
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", "- op: add\n  table: appearance\n  id: base:row\n  value:\n    id: base:row\n" +
            (original.Length == 0 ? string.Empty : "    " + original + "\n"));
        workspace.Add("patch", "- op: patch\n  table: appearance\n  id: base:row\n  value:\n    appearance: " + patch);
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        var plan = ContentPackResolver.Resolve(packages,
            new ContentPackProfile { EnabledPackages = ["patch"] }, Host);
        var catalog = await new ContentCatalogLoader().RegisterTable<AppearanceRow>("appearance", schemaPath: null,
            static row => row.Id).LoadAsync(plan).ConfigureAwait(false);
        var reference = new ContentReference("appearance", new ContentId("base:row"));
        Assert.That(catalog.GetFieldSource(reference, "appearance")?.PackId, Is.EqualTo(containerOwner));
        if (patchedField is not null)
            Assert.That(catalog.GetFieldSource(reference, "appearance." + patchedField)?.PackId, Is.EqualTo("patch"));
        if (string.Equals(containerOwner, "base", StringComparison.Ordinal))
            Assert.That(catalog.GetFieldSource(reference, "appearance.left")?.PackId, Is.EqualTo("base"));
    }

    /// <summary>Invalid operations and identifiers fail instead of silently replacing unrelated data.</summary>
    [TestCase("add", "base:walker", "{id: 'base:walker', speed: 1}", "DuplicateOrForeignContent")]
    [TestCase("patch", "bad:missing", "{speed: 1}", "MissingContentTarget")]
    [TestCase("replace", "bad:missing", "{id: 'bad:missing'}", "MissingContentTarget")]
    [TestCase("remove", "bad:missing", "", "MissingContentTarget")]
    [TestCase("patch", "base:walker", "{id: 'bad:new'}", "ContentIdChanged")]
    public async Task Loader_RejectsInvalidOperations(string operation, string id, string value, string code)
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        workspace.Add("bad", $"- op: {operation}\n  table: enemies\n  id: {id}\n" +
            (value.Length == 0 ? string.Empty : $"  value: {value}\n"));
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        var exception = await Assert.ThrowsAsync<ContentLoadException>(() => workspace.LoadAsync(packages, "bad"))
            .ConfigureAwait(false);
        Assert.That(exception!.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(exception.Diagnostic.PackId, Is.EqualTo("bad"));
    }

    /// <summary>Schema validation and references run against final rows, and failures retain the offending patch source.</summary>
    [Test]
    public async Task Loader_ValidatesFinalSchemaAndReferences()
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData + """

            - op: add
              table: enemies
              id: base:follower
              value:
                id: base:follower
                speed: 80
                stats: {color: green, armor: 0}
                weights: []
                target: base:walker
            """);
        workspace.Add("bad", """
            - op: remove
              table: enemies
              id: base:walker
            """);
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        var exception = await Assert.ThrowsAsync<ContentLoadException>(() => workspace.LoadAsync(packages, "bad"))
            .ConfigureAwait(false);
        Assert.That(exception!.Diagnostic.Code, Is.EqualTo("MissingContentReference"));
        Assert.That(exception.Diagnostic.RelatedPackId, Is.EqualTo("bad"));
        workspace.Add("bad", """
            - op: patch
              table: enemies
              id: base:walker
              value: {speed: null}
            """);
        packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        exception = await Assert.ThrowsAsync<ContentLoadException>(() => workspace.LoadAsync(packages, "bad"))
            .ConfigureAwait(false);
        Assert.That(exception!.Diagnostic.Code, Is.EqualTo("SchemaValidationFailed"));
        Assert.That(exception.Diagnostic.PackId, Is.EqualTo("bad"));
    }

    /// <summary>A modified package must be rediscovered before its fingerprint can enter a save.</summary>
    [Test]
    public async Task Loader_RejectsChangedPackageSnapshot()
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        await File.AppendAllTextAsync(Path.Combine(workspace.Root, "base", "content.yaml"), "\n# package edited after discovery\n").ConfigureAwait(false);
        var exception = await Assert.ThrowsAsync<ContentLoadException>(() => workspace.LoadAsync(packages))
            .ConfigureAwait(false);
        Assert.That(exception!.Diagnostic.Code, Is.EqualTo("PackageChanged"));
    }

    /// <summary>Package paths and filesystem links cannot redirect content access outside the installation.</summary>
    [Test]
    public async Task Discovery_RejectsTraversalAndLinkedFiles()
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        Assert.Throws<ArgumentException>(() => packages[0].GetFilePath("../secret.yaml"));
        Assert.Throws<ArgumentException>(() => packages[0].GetFilePath("C:/secret.yaml"));
        Assert.Throws<ArgumentException>(() => packages[0].GetFilePath("res://secret.yaml"));
        if (OperatingSystem.IsWindows())
            return; // Windows link creation may require an unavailable developer privilege.
        var external = Path.Combine(workspace.Root, "external.yaml");
        await File.WriteAllTextAsync(external, "[]").ConfigureAwait(false);
        File.CreateSymbolicLink(Path.Combine(workspace.Root, "base", "redirect.yaml"), external);
        await Assert.ThrowsAsync<ContentLoadException>(() => workspace.DiscoverAsync()).ConfigureAwait(false);
    }

    /// <summary>An installed package-root link is a structured inventory failure that management UIs can present.</summary>
    [Test]
    public async Task Discovery_LinkedPackageRootHasStructuredDiagnostic()
    {
        if (OperatingSystem.IsWindows())
            return;
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        var link = Path.Combine(workspace.Root, "linked-package");
        Directory.CreateSymbolicLink(link, Path.Combine(workspace.Root, "base"));
        var exception = await Assert.ThrowsAsync<ContentLoadException>(() =>
            new ContentPackDiscovery().DiscoverAsync([link])).ConfigureAwait(false);
        Assert.That(exception!.Diagnostic.Code, Is.EqualTo("InvalidPackagePath"));
        Assert.That(exception.Diagnostic.FilePath, Is.EqualTo(link));
    }

    /// <summary>Aliases and custom tags never become data or runtime type activations.</summary>
    [TestCase("- &anchor {op: add, table: enemies, id: bad:item, value: {id: 'bad:item'}}")]
    [TestCase("- !Dangerous {op: add, table: enemies, id: bad:item, value: {id: 'bad:item'}}")]
    [TestCase("- !!object {op: add, table: enemies, id: bad:item, value: {id: 'bad:item'}}")]
    public async Task Loader_RejectsYamlAnchorsAndTags(string data)
    {
        using var workspace = new PackageWorkspace();
        workspace.Add("base", BaseData);
        workspace.Add("bad", data);
        var packages = await workspace.DiscoverAsync().ConfigureAwait(false);
        var exception = await Assert.ThrowsAsync<ContentLoadException>(() => workspace.LoadAsync(packages, "bad"))
            .ConfigureAwait(false);
        Assert.That(exception!.Diagnostic.Code, Is.EqualTo("InvalidContentFile"));
    }

    private static void AssertFailure(string code, Action action)
        => Assert.That(Assert.Throws<ContentLoadException>(action)!.Diagnostic.Code, Is.EqualTo(code));

    private const string BaseData = """
        - op: add
          table: enemies
          id: base:walker
          value:
            id: base:walker
            speed: 125
            stats: {color: gray, armor: 1}
            weights: [1, 2]
            note: initial
        """;

    private sealed class PackageWorkspace : IDisposable
    {
        private static readonly JsonSerializerOptions _manifestOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };


        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "gframework-content", Guid.NewGuid().ToString("N"));
        private readonly string _schemaPath;

        internal PackageWorkspace()
        {
            Directory.CreateDirectory(Root);
            _schemaPath = Path.Combine(Root, "enemy.schema.json");
            File.WriteAllText(_schemaPath, """
                {
                  "type":"object", "additionalProperties":false,
                  "required":["id","speed","stats","weights"],
                  "properties": {
                    "id":{"type":"string"}, "speed":{"type":"integer","minimum":1},
                    "note":{"type":"string"},
                    "stats":{"type":"object","additionalProperties":false,"required":["color","armor"],
                      "properties":{"color":{"type":"string"},"armor":{"type":"integer"}}},
                    "weights":{"type":"array","items":{"type":"integer"}},
                    "target":{"type":"string","x-gframework-ref-table":"enemies"}
                  }
                }
                """);
        }

        internal void Add(string id, string data, IReadOnlyList<ContentPackDependency>? dependencies = null,
            IReadOnlyList<string>? incompatible = null)
        {
            var directory = Path.Combine(Root, id);
            Directory.CreateDirectory(directory);
            var manifest = new ContentPackManifest
            {
                Id = id,
                Name = id,
                GameId = "test",
                ContentFiles = ["content.yaml"],
                Dependencies = dependencies ?? [],
                IncompatiblePackages = incompatible ?? [],
            };
            File.WriteAllText(Path.Combine(directory, "content-pack.json"), JsonSerializer.Serialize(manifest,
                _manifestOptions));
            File.WriteAllText(Path.Combine(directory, "content.yaml"), data);
        }

        internal Task<IReadOnlyList<ContentPack>> DiscoverAsync()
            => new ContentPackDiscovery().DiscoverAsync([Root]);

        internal Task<ContentCatalog> LoadAsync(IEnumerable<ContentPack> packages, params string[] enabled)
        {
            var plan = ContentPackResolver.Resolve(packages, new ContentPackProfile
            {
                EnabledPackages = enabled,
                LoadOrder = enabled,
            }, Host);
            return new ContentCatalogLoader().RegisterTable<Enemy>("enemies", _schemaPath, static row => row.Id).LoadAsync(plan);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    /// <summary>Game-owned sample DTO whose shape is fixed before loading external content.</summary>
    public sealed class Enemy
    {
        /// <summary>Gets or sets the stable namespaced ID.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Gets or sets game-validated speed.</summary>
        public int Speed
        {
            get; set;
        }
        /// <summary>Gets or sets nested content fields.</summary>
        public EnemyStats Stats { get; set; } = new();
        /// <summary>Gets or sets an array replaced as a whole by patches.</summary>
        public int[] Weights { get; set; } = [];
        /// <summary>Gets or sets an optional field removed by a null patch.</summary>
        public string? Note
        {
            get; set;
        }
        /// <summary>Gets or sets a declared cross-table reference.</summary>
        public string? Target
        {
            get; set;
        }
    }

    /// <summary>Nested fields used to verify recursive merge and field provenance.</summary>
    public sealed class EnemyStats
    {
        /// <summary>Gets or sets an appearance field.</summary>
        public string Color { get; set; } = string.Empty;
        /// <summary>Gets or sets an independent numeric field.</summary>
        public int Armor
        {
            get; set;
        }
    }

    /// <summary>Permits scalar-to-object patch tests independently of a game-specific schema shape.</summary>
    public sealed class AppearanceRow
    {
        /// <summary>Gets or sets the stable content ID.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Gets or sets the deliberately flexible field under test.</summary>
        public object? Appearance
        {
            get; set;
        }
    }
}
