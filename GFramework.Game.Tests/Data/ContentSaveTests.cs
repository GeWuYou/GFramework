// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Content;
using GFramework.Game.Data;
using GFramework.Game.Storage;
using Newtonsoft.Json.Linq;
using RuntimeJsonSerializer = GFramework.Game.Serializer.JsonSerializer;

namespace GFramework.Game.Tests.Data;

/// <summary>Checks document-level content recovery before DTO construction and exact session-bound commit.</summary>
[TestFixture]
public sealed class ContentSaveTests
{
    private string _root = null!;
    private FileStorage _storage = null!;
    private readonly JsonSaveDocumentCodec _codec = new();
    private static readonly ContentReference BaseArena = new("arenas", new ContentId("base:arena"));
    private static readonly ContentReference ModArena = new("arenas", new ContentId("extra:arena"));
    private static readonly SaveConfiguration Configuration = new() { SaveRoot = "saves", SaveFileName = "save" };

    /// <summary>Creates a private persistence root.</summary>
    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "gframework-content-documents-" + Guid.NewGuid().ToString("N"));
        _storage = new FileStorage(_root, new RuntimeJsonSerializer(), ".json");
    }

    /// <summary>Releases only this fixture's temporary files.</summary>
    [TearDown]
    public void TearDown()
    {
        _storage.Dispose();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    /// <summary>Content metadata lives in the envelope, without imposing metadata interfaces on game DTOs.</summary>
    [Test]
    public async Task SaveAndLoad_UseEnvelopeMetadata()
    {
        var catalog = CreateCatalog(includeMod: false);
        var repository = CreateRepository(() => catalog);
        await repository.SaveAsync(1, new ContentSave { Value = 7 });
        var loaded = await CreateRepository(() => catalog).LoadAsync(1);
        var document = _codec.Decode(await SnapshotAsync());
        Assert.Multiple(() =>
        {
            Assert.That(loaded.Value, Is.EqualTo(7));
            Assert.That(document.Content!.Packs.Single().Id, Is.EqualTo("base"));
            Assert.That(document.Game.Data.ContainsKey("Content"), Is.False);
        });
    }

    /// <summary>Missing outer content metadata is a policy decision, without reviving an old-format import path.</summary>
    [Test]
    public async Task MissingMetadata_RequiresExplicitDocumentRecovery()
    {
        var document = new SaveDocument
        {
            GameId = "test",
            Game = new SaveDataBlock { Version = 2, Data = JObject.Parse("""{"ArenaId":"base:arena","Value":7}""") },
        };
        await _storage.WriteSnapshotAsync("saves/slot_1/save", _codec.Encode(document));
        var repository = CreateRepository(() => CreateCatalog(includeMod: false), static (candidate, report, _) =>
        {
            Assert.That(report.Issues.Single().Kind, Is.EqualTo(ContentSaveIssueKind.MissingMetadata));
            return Task.FromResult(candidate);
        });
        var prepared = await repository.PrepareLoadAsync(1);
        Assert.That(prepared.RequiresConfirmation, Is.True);
        Assert.That(prepared.SavedContent, Is.Null);
        Assert.That(prepared.Document.Content!.Packs.Single().Id, Is.EqualTo("base"));
    }

    /// <summary>Missing packages and references become explicit candidate diagnostics, with raw recovery before DTO validation.</summary>
    [Test]
    public async Task Prepare_RepairsMissingContentBeforeCurrentDtoValidation()
    {
        var originalCatalog = CreateCatalog(includeMod: true);
        await CreateRepository(() => originalCatalog).SaveAsync(1, new ContentSave { ArenaId = "extra:arena", Value = 5 });
        var original = await SnapshotAsync();
        var baseCatalog = CreateCatalog(includeMod: false);
        var calls = 0;
        var repository = CreateRepository(() => baseCatalog, (data, report, _) =>
        {
            calls++;
            Assert.That(report.Issues.Select(static issue => issue.Kind), Does.Contain(ContentSaveIssueKind.MissingPack));
            Assert.That(Directory.Exists(Path.Combine(_root, "saves", "slot_1", "backups")), Is.False,
                "Pure preparation must not persist a backup before an approval exists.");
            data.Game.Data["ArenaId"] = "base:arena";
            return Task.FromResult(data);
        });
        var prepared = await repository.PrepareLoadAsync(1);
        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(prepared.RequiresConfirmation, Is.True);
            Assert.That(prepared.Data.ArenaId, Is.EqualTo("base:arena"));
            Assert.That(File.ReadAllBytes(SavePath), Is.EqualTo(original.ToArray()));
        });
        var committed = await repository.CommitPreparedAsync(1, prepared);
        Assert.That(calls, Is.EqualTo(1), "Commit must use immutable prepared bytes instead of rerunning recovery.");
        Assert.That((await repository.LoadAsync(1)).Value, Is.EqualTo(5));
        Assert.That((await _storage.ReadSnapshotAsync("saves/slot_1/" + committed.BackupKey)).ToArray(), Is.EqualTo(original.ToArray()));
    }

    /// <summary>Absent recovery policy stops before publishing any prepared current data.</summary>
    [Test]
    public async Task MissingContentWithoutPolicy_PreservesOriginal()
    {
        await CreateRepository(() => CreateCatalog(includeMod: true)).SaveAsync(1, new ContentSave { ArenaId = "extra:arena" });
        var original = await SnapshotAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateRepository(() => CreateCatalog(includeMod: false)).PrepareLoadAsync(1));
        Assert.That((await SnapshotAsync()).Fingerprint, Is.EqualTo(original.Fingerprint));
    }

    /// <summary>Recovery that leaves missing references or invalid business data cannot be committed.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task InvalidRecovery_PreservesOriginal(bool invalidBusinessValue)
    {
        await CreateRepository(() => CreateCatalog(includeMod: true)).SaveAsync(1, new ContentSave { ArenaId = "extra:arena" });
        var original = await SnapshotAsync();
        var repository = CreateRepository(() => CreateCatalog(includeMod: false), (data, _, _) =>
        {
            if (invalidBusinessValue)
            {
                data.Game.Data["ArenaId"] = "base:arena";
                data.Game.Data["Value"] = -1;
            }
            return Task.FromResult(data);
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.PrepareLoadAsync(1));
        Assert.That((await SnapshotAsync()).Fingerprint, Is.EqualTo(original.Fingerprint));
    }

    /// <summary>Source schema conversion precedes raw-reference enumeration and content repair.</summary>
    [Test]
    public async Task SchemaConversionPrecedesContentRepair_WithoutCurrentDtoDependency()
    {
        var catalog = CreateCatalog(includeMod: false);
        var source = new SaveDocument
        {
            GameId = "test",
            Content = CreateCatalog(includeMod: true).Snapshot,
            Game = new SaveDataBlock { Version = 1, Data = JObject.Parse("""{"OldArena":"extra:arena","Value":4}""") },
        };
        await _storage.WriteSnapshotAsync("saves/slot_1/save", _codec.Encode(source));
        var step = new SaveMigrationStep(1, 2, "Rename arena", (block, _) =>
        {
            block.Data["ArenaId"] = block.Data["OldArena"];
            block.Data.Remove("OldArena");
            block.Version = 2;
            return Task.FromResult(block);
        });
        var repository = CreateRepository(() => catalog, (data, _, _) =>
        {
            Assert.That(data.Game.Data.ContainsKey("OldArena"), Is.False);
            data.Game.Data["ArenaId"] = "base:arena";
            return Task.FromResult(data);
        }, migrations: new SaveMigrationGraph([step]));
        var prepared = await repository.PrepareLoadAsync(1);
        Assert.That(prepared.Data.ArenaId, Is.EqualTo("base:arena"));
        Assert.That(prepared.MigrationPath.Single().ToVersion, Is.EqualTo(2));
    }

    /// <summary>Whole-document recovery lets the game explicitly remove an unavailable owner's opaque state.</summary>
    [Test]
    public async Task Recovery_CanExplicitlyRemoveUnavailableModData()
    {
        var document = new SaveDocument
        {
            GameId = "test",
            Content = CreateCatalog(includeMod: true).Snapshot,
            Game = new SaveDataBlock { Version = 2, Data = JObject.Parse("""{"ArenaId":"extra:arena","Value":7}""") },
        };
        document.Mods["extra"] = new SaveDataBlock { Version = 9, Data = JObject.Parse("""{"UnknownProgress":123}""") };
        await _storage.WriteSnapshotAsync("saves/slot_1/save", _codec.Encode(document));
        var repository = CreateRepository(() => CreateCatalog(includeMod: false), static (candidate, _, _) =>
        {
            candidate.Game.Data["ArenaId"] = "base:arena";
            candidate.Mods.Remove("extra");
            return Task.FromResult(candidate);
        });
        var prepared = await repository.PrepareLoadAsync(1);
        Assert.That(prepared.RequiresConfirmation, Is.True);
        Assert.That(prepared.Document.Mods, Is.Empty);
        Assert.That(_codec.Decode(await SnapshotAsync()).Mods.ContainsKey("extra"), Is.True);
    }

    /// <summary>Recovery cannot forge ownership or pretend a different schema is current.</summary>
    [TestCase(true)]
    [TestCase(false)]
    public async Task Recovery_RejectsChangedIdentityOrCurrentVersion(bool changeIdentity)
    {
        await CreateRepository(() => CreateCatalog(includeMod: true)).SaveAsync(1, new ContentSave { ArenaId = "extra:arena" });
        var repository = CreateRepository(() => CreateCatalog(includeMod: false), (candidate, _, _) =>
        {
            candidate.Game.Data["ArenaId"] = "base:arena";
            if (changeIdentity)
                candidate.GameId = "forged";
            else
                candidate.Game.Version = 999;
            return Task.FromResult(candidate);
        });
        var original = await SnapshotAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.PrepareLoadAsync(1));
        Assert.That((await SnapshotAsync()).Fingerprint, Is.EqualTo(original.Fingerprint));
    }

    /// <summary>Changing the startup content session invalidates a prior approval even if the source bytes did not change.</summary>
    [Test]
    public async Task Commit_RejectsChangedTargetSession()
    {
        var current = CreateCatalog(includeMod: true);
        await CreateRepository(() => current).SaveAsync(1, new ContentSave { ArenaId = "extra:arena" });
        current = CreateCatalog(includeMod: false);
        var repository = CreateRepository(() => current, static (data, _, _) =>
        {
            data.Game.Data["ArenaId"] = "base:arena";
            return Task.FromResult(data);
        });
        var prepared = await repository.PrepareLoadAsync(1);
        current = CreateCatalog(includeMod: false, baseFingerprint: "changed");
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CommitPreparedAsync(1, prepared));
    }

    /// <summary>Version-range compatibility is explicit and does not silently accept same-version file edits.</summary>
    [TestCase("1.0.0", "2.0.0", true)]
    [TestCase("1.0.0", "1.0.0", false)]
    [TestCase("3.0.0", "2.0.0", false)]
    public async Task PackageCompatibility_UsesDeclaredRangeButRejectsSameVersionEdits(string oldVersion, string currentVersion, bool compatible)
    {
        await CreateRepository(() => CreateCatalog(includeMod: false, baseVersion: oldVersion)).SaveAsync(1, new ContentSave());
        var current = CreateCatalog(includeMod: false, baseVersion: currentVersion, baseFingerprint: "updated",
            compatibleVersions: new ContentVersionRange { MinimumInclusive = "1.0.0", MaximumExclusive = "3.0.0" });
        var repository = CreateRepository(() => current);
        if (compatible)
        {
            var prepared = await repository.PrepareLoadAsync(1);
            Assert.That(prepared.RequiresConfirmation, Is.False);
            Assert.That(prepared.RequiresCommit, Is.True);
        }
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.PrepareLoadAsync(1));
    }

    /// <summary>Newly enabled packages are advisory; persisting the new session remains an explicit commit.</summary>
    [Test]
    public async Task AdditionalPackage_IsAdvisoryAndNoWrite()
    {
        await CreateRepository(() => CreateCatalog(includeMod: false)).SaveAsync(1, new ContentSave());
        var original = await SnapshotAsync();
        var prepared = await CreateRepository(() => CreateCatalog(includeMod: true)).PrepareLoadAsync(1);
        Assert.That(prepared.RequiresConfirmation, Is.False);
        Assert.That(prepared.Issues.Single().Kind, Is.EqualTo(ContentSaveIssueKind.AdditionalPack));
        Assert.That((await SnapshotAsync()).Fingerprint, Is.EqualTo(original.Fingerprint));
    }

    /// <summary>Creates explicit current-schema policy with raw references and optional pure recovery.</summary>
    private SaveRepository<ContentSave> CreateRepository(Func<ContentCatalog> catalog,
        Func<SaveDocument, ContentSaveReport, CancellationToken, Task<SaveDocument>>? recover = null, SaveMigrationGraph? migrations = null) =>
        new(_storage, Configuration, new SaveRepositoryOptions<ContentSave>
        {
            GameId = "test",
            CurrentVersion = 2,
            Migrations = migrations ?? new SaveMigrationGraph(),
            Content = new ContentSaveOptions
            {
                CurrentCatalog = catalog,
                EnumerateReferences = static data => [new ContentReference("arenas", new ContentId(data.Game.Data["ArenaId"]!.Value<string>()!))],
                RecoverAsync = recover,
            },
            Validate = static data => { if (data.Value < 0) throw new InvalidOperationException("Invalid negative business value."); },
        });

    /// <summary>Creates immutable catalog state through the production catalog representation.</summary>
    private ContentCatalog CreateCatalog(bool includeMod, string baseVersion = "1.0.0", string baseFingerprint = "base-hash",
        ContentVersionRange? compatibleVersions = null)
    {
        var packages = new List<ContentPack>
        {
            new(_root, new ContentPackManifest { Id = "base", Name = "Base", GameId = "test", Version = baseVersion,
                SaveCompatibleVersions = compatibleVersions, }, baseFingerprint),
        };
        var references = new HashSet<ContentReference> { BaseArena };
        if (includeMod)
        {
            packages.Add(new ContentPack(_root, new ContentPackManifest { Id = "extra", Name = "Extra", GameId = "test" }, "extra-hash"));
            references.Add(ModArena);
        }
        return new ContentCatalog(new ContentLoadPlan(packages), new Dictionary<string, IConfigTable>(StringComparer.Ordinal), references,
            new Dictionary<ContentReference, IReadOnlyList<ContentSource>>(), new Dictionary<(ContentReference Reference, string Field), ContentSource>());
    }

    /// <summary>Reads immutable source bytes.</summary>
    private Task<GFramework.Game.Abstractions.Storage.StorageSnapshot> SnapshotAsync() => _storage.ReadSnapshotAsync("saves/slot_1/save");
    /// <summary>Gets the physical slot file for byte preservation assertions.</summary>
    private string SavePath => Path.Combine(_root, "saves", "slot_1", "save.json");

    /// <summary>Pure game DTO with no required version/content interfaces.</summary>
    public sealed class ContentSave : IData
    {
        /// <summary>Gets or sets the stable arena reference.</summary>
        public string ArenaId { get; set; } = "base:arena";
        /// <summary>Gets or sets a validated business value.</summary>
        public int Value
        {
            get; set;
        }
    }
}
