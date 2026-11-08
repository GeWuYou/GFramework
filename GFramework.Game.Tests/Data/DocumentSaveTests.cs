// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text;
using System.Threading;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Storage;
using GFramework.Game.Data;
using GFramework.Game.Storage;
using Newtonsoft.Json.Linq;
using RuntimeJsonSerializer = GFramework.Game.Serializer.JsonSerializer;

namespace GFramework.Game.Tests.Data;

/// <summary>Exercises document-first schema evolution, immutable approval and atomic lossless persistence.</summary>
[TestFixture]
public sealed class DocumentSaveTests
{
    private string _root = null!;
    private FileStorage _storage = null!;
    private readonly JsonSaveDocumentCodec _codec = new();
    private static readonly SaveConfiguration Configuration = new() { SaveRoot = "saves", SaveFileName = "save" };
    private const string SaveKey = "saves/slot_1/save";

    /// <summary>Creates a private physical storage root.</summary>
    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "gframework-documents-" + Guid.NewGuid().ToString("N"));
        _storage = new FileStorage(_root, new RuntimeJsonSerializer(), ".json");
    }

    /// <summary>Releases only this fixture's private files.</summary>
    [TearDown]
    public void TearDown()
    {
        _storage.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    /// <summary>The old payload cannot become a current DTO, but a document conversion can rename/retype it first.</summary>
    [Test]
    public async Task Prepare_MigratesIncompatibleShapeBeforeDto_AndPreservesUnknownFields()
    {
        var source = Document(1, """{"Position":"12,34","Unknown":{"keep":true}}""");
        await WriteAsync(source);
        Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => _codec.DeserializeData<CurrentSave>(source.Game.Data));
        var original = await SnapshotAsync();
        var repository = Repository(2, new SaveMigrationGraph([PositionUpgrade()]));
        var prepared = await repository.PrepareLoadAsync(1);
        Assert.Multiple(() =>
        {
            Assert.That(prepared.Data.Position.X, Is.EqualTo(12));
            Assert.That(prepared.Document.Game.Data["Unknown"]!["keep"]!.Value<bool>(), Is.True);
            Assert.That(prepared.RequiresCommit, Is.True);
            Assert.That(prepared.RequiresConfirmation, Is.False);
            Assert.That(File.ReadAllBytes(SavePath), Is.EqualTo(original.ToArray()));
            Assert.That(Directory.Exists(Path.Combine(SlotPath, "backups")), Is.False);
        });
        var committed = await repository.CommitPreparedAsync(1, prepared);
        Assert.That((await repository.LoadAsync(1)).Position.Y, Is.EqualTo(34));
        Assert.That((await _storage.ReadSnapshotAsync("saves/slot_1/" + committed.BackupKey)).ToArray(), Is.EqualTo(original.ToArray()));
        Assert.That(_codec.Decode(await SnapshotAsync()).Game.Data["Unknown"]!["keep"]!.Value<bool>(), Is.True);
    }

    /// <summary>Ordinary load must not silently persist a conversion.</summary>
    [Test]
    public async Task Load_RequiresExplicitCommit_AndNeverWrites()
    {
        await WriteAsync(Document(1, """{"Position":"12,34"}"""));
        var original = await SnapshotAsync();
        var repository = Repository(2, new SaveMigrationGraph([PositionUpgrade()]));
        var exception = await Assert.ThrowsAsync<SavePreparationRequiredException<CurrentSave>>(() => repository.LoadAsync(1));
        Assert.That(exception!.Prepared.Data.Position.X, Is.EqualTo(12));
        Assert.That((await SnapshotAsync()).Fingerprint, Is.EqualTo(original.Fingerprint));
    }

    /// <summary>Downgrades are explicit directed transformations and are always marked for player confirmation.</summary>
    [Test]
    public async Task Prepare_UsesExplicitDowngrade_AndMarksConfirmation()
    {
        await WriteAsync(Document(3, """{"Position":{"X":8,"Y":9},"FutureMechanic":123}"""));
        var step = new SaveMigrationStep(3, 2, "Remove future mechanic", (block, _) =>
        {
            block.Data.Remove("FutureMechanic");
            block.Version = 2;
            return Task.FromResult(block);
        });
        var repository = Repository(2, new SaveMigrationGraph([step]));
        var prepared = await repository.PrepareLoadAsync(1);
        Assert.That(prepared.RequiresConfirmation, Is.True);
        Assert.That(prepared.MigrationPath.Single().FromVersion, Is.EqualTo(3));
        Assert.That(prepared.Document.Game.Data.ContainsKey("FutureMechanic"), Is.False);
    }

    /// <summary>A graph never invents a reverse conversion from an upward edge.</summary>
    [Test]
    public async Task Prepare_RejectsMissingReversePath_WithoutChangingOriginal()
    {
        await WriteAsync(Document(3, """{}"""));
        var original = await SnapshotAsync();
        var repository = Repository(2, new SaveMigrationGraph([Edge(2, 3)]));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.PrepareLoadAsync(1));
        Assert.That((await SnapshotAsync()).Fingerprint, Is.EqualTo(original.Fingerprint));
    }

    /// <summary>Equal shortest routes require a caller decision through the actual repository entrypoint.</summary>
    [Test]
    public async Task Prepare_AmbiguousRoutesRequireExplicitPath()
    {
        await WriteAsync(Document(1, """{}"""));
        var graph = new SaveMigrationGraph([Edge(1, 2), Edge(2, 4), Edge(1, 3), Edge(3, 4)]);
        var repository = Repository(4, graph);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.PrepareLoadAsync(1));
        var prepared = await repository.PrepareLoadAsync(1, versionPath: new[] { 1, 3, 4 });
        Assert.That(prepared.MigrationPath.Select(static step => step.ToVersion), Is.EqualTo(new[] { 3, 4 }));
    }

    /// <summary>Duplicate edges, self edges and route reversals fail deterministically at policy boundaries.</summary>
    [Test]
    public void Graph_RejectsInvalidRegistrationAndExplicitPaths()
    {
        Assert.Throws<ArgumentException>(() => new SaveMigrationStep(1, 1, "invalid", static (block, _) => Task.FromResult(block)));
        Assert.Throws<ArgumentException>(() => new SaveMigrationGraph([Edge(1, 2), Edge(1, 2)]));
        var graph = new SaveMigrationGraph([Edge(1, 3), Edge(3, 2), Edge(2, 4)]);
        Assert.Throws<ArgumentException>(() => graph.Plan(1, 4, new[] { 1, 3, 2, 4 }));
        Assert.Throws<ArgumentException>(() => graph.Plan(1, 4, new[] { 2, 4 }));
    }

    /// <summary>Wrong output versions and callback failures preserve original bytes.</summary>
    [TestCase("wrong-version")]
    [TestCase("throw")]
    [TestCase("null")]
    public async Task Prepare_FailedConversionCannotPublish(string failure)
    {
        await WriteAsync(Document(1, """{}"""));
        var original = await SnapshotAsync();
        var step = new SaveMigrationStep(1, 2, "broken", (block, _) => failure switch
        {
            "throw" => throw new InvalidOperationException("broken conversion"),
            "null" => Task.FromResult<SaveDataBlock>(null!),
            _ => Task.FromResult(block)
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => Repository(2, new SaveMigrationGraph([step])).PrepareLoadAsync(1));
        Assert.That((await SnapshotAsync()).Fingerprint, Is.EqualTo(original.Fingerprint));
    }

    /// <summary>Callers cannot mutate an approved candidate through its public DTO, document or mod draft.</summary>
    [Test]
    public async Task Commit_UsesImmutablePrivateCandidate()
    {
        await WriteAsync(Document(1, """{"Position":"12,34"}"""));
        var repository = Repository(2, new SaveMigrationGraph([PositionUpgrade()]), [new ModSavePolicy("extra", 1)]);
        var prepared = await repository.PrepareLoadAsync(1);
        prepared.Data.Position.X = 999;
        prepared.Document.Game.Data["Position"]!["X"] = 888;
        prepared.ModState.ForMod("extra").Write(new ModCounter { Count = 777 });
        var result = await repository.CommitPreparedAsync(1, prepared);
        Assert.That(result.Data.Position.X, Is.EqualTo(12));
        Assert.That(_codec.Decode(await SnapshotAsync()).Mods, Is.Empty);
    }

    /// <summary>A source changed after confirmation cannot be overwritten, even when it is valid JSON.</summary>
    [Test]
    public async Task Commit_RejectsStaleFileAndForeignRepository()
    {
        await WriteAsync(Document(1, """{"Position":"12,34"}"""));
        var repository = Repository(2, new SaveMigrationGraph([PositionUpgrade()]));
        var prepared = await repository.PrepareLoadAsync(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Repository(2).CommitPreparedAsync(1, prepared));
        await WriteAsync(Document(2, """{"Position":{"X":99}}"""));
        var changed = await SnapshotAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CommitPreparedAsync(1, prepared));
        Assert.That((await SnapshotAsync()).Fingerprint, Is.EqualTo(changed.Fingerprint));
        Assert.That(Directory.Exists(Path.Combine(SlotPath, "backups")), Is.False);
    }

    /// <summary>Backup failure aborts before replacement; replacement failure retains both original and completed backup.</summary>
    [TestCase(true)]
    [TestCase(false)]
    public async Task Commit_WriteFailuresPreserveOriginal(bool failBackup)
    {
        await WriteAsync(Document(1, """{"Position":"12,34"}"""));
        var original = await SnapshotAsync();
        var proxy = StorageProxy(key =>
        {
            if (key.Contains("backups/", StringComparison.Ordinal) == failBackup) throw new IOException("Injected write failure");
            return Task.CompletedTask;
        });
        var repository = Repository(2, new SaveMigrationGraph([PositionUpgrade()]), storage: proxy);
        var prepared = await repository.PrepareLoadAsync(1);
        await Assert.ThrowsAsync<IOException>(() => repository.CommitPreparedAsync(1, prepared));
        Assert.That((await SnapshotAsync()).Fingerprint, Is.EqualTo(original.Fingerprint));
        if (!failBackup) Assert.That(Directory.GetFiles(Path.Combine(SlotPath, "backups")), Has.Length.EqualTo(1));
    }

    /// <summary>External writes during backup invalidate approval before the final atomic replacement.</summary>
    [Test]
    public async Task Commit_RechecksAfterBackup()
    {
        await WriteAsync(Document(1, """{"Position":"12,34"}"""));
        var proxy = StorageProxy(async key =>
        {
            if (key.Contains("backups/", StringComparison.Ordinal))
                await WriteAsync(Document(2, """{"Position":{"X":99}}""")).ConfigureAwait(false);
        });
        var repository = Repository(2, new SaveMigrationGraph([PositionUpgrade()]), storage: proxy);
        var prepared = await repository.PrepareLoadAsync(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CommitPreparedAsync(1, prepared));
        Assert.That(_codec.Decode(await SnapshotAsync()).Game.Data["Position"]!["X"]!.Value<int>(), Is.EqualTo(99));
    }

    /// <summary>Raw backups preserve BOM, whitespace and unknown fields, and restoration never interprets them.</summary>
    [Test]
    public async Task CommitAndRestore_PreserveExactOriginalBytes()
    {
        var encoded = _codec.Encode(Document(1, """{"Position":"12,34","Unknown":[1,2]}"""));
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(encoded.ReadText())).ToArray();
        await _storage.WriteSnapshotAsync(SaveKey, new StorageSnapshot(bytes));
        var repository = Repository(2, new SaveMigrationGraph([PositionUpgrade()]));
        var result = await repository.CommitPreparedAsync(1, await repository.PrepareLoadAsync(1));
        var converted = await SnapshotAsync();
        var displaced = await repository.RestoreBackupAsync(1, result.BackupKey);
        Assert.That((await SnapshotAsync()).ToArray(), Is.EqualTo(bytes));
        Assert.That((await _storage.ReadSnapshotAsync("saves/slot_1/" + displaced)).ToArray(), Is.EqualTo(converted.ToArray()));
    }

    /// <summary>Cancellation during pure preparation does not create backups or write the slot.</summary>
    [Test]
    public async Task Prepare_CancellationPreservesOriginal()
    {
        await WriteAsync(Document(1, """{}"""));
        var original = await SnapshotAsync();
        using var cancellation = new CancellationTokenSource();
        var step = new SaveMigrationStep(1, 2, "cancel", (block, _) =>
        {
            cancellation.Cancel();
            block.Version = 2;
            return Task.FromResult(block);
        });
        await Assert.CatchAsync<OperationCanceledException>(() => Repository(2, new SaveMigrationGraph([step])).PrepareLoadAsync(1, cancellation.Token));
        Assert.That((await SnapshotAsync()).Fingerprint, Is.EqualTo(original.Fingerprint));
    }

    /// <summary>Inactive mod data survives game saves and becomes readable again when that mod is enabled.</summary>
    [Test]
    public async Task ModData_PreservesMissingOwner_AndScopesActiveWrites()
    {
        var document = Document(2, """{}""");
        document.Mods["missing"] = new SaveDataBlock { Version = 7, Data = JObject.Parse("""{"SecretProgress":123}""") };
        await WriteAsync(document);
        var repository = Repository(2, mods: [new ModSavePolicy("extra", 1)]);
        var prepared = await repository.PrepareLoadAsync(1);
        var state = prepared.ModState;
        Assert.Throws<InvalidOperationException>(() => state.ForMod("missing"));
        state.ForMod("extra").Write(new ModCounter { Count = 9 });
        Assert.That(state.ForMod("extra").Read<ModCounter>()!.Count, Is.EqualTo(9));
        await repository.SaveDocumentAsync(1, prepared.Data, state);
        var saved = _codec.Decode(await SnapshotAsync());
        Assert.That(saved.Mods["missing"].Data["SecretProgress"]!.Value<int>(), Is.EqualTo(123));
        var enabled = Repository(2, mods: [new ModSavePolicy("missing", 7), new ModSavePolicy("extra", 1)]);
        Assert.That((await enabled.PrepareLoadAsync(1)).ModState.ForMod("extra").Read<ModCounter>()!.Count, Is.EqualTo(9));
        await enabled.SaveAsync(1, new CurrentSave());
        Assert.That(_codec.Decode(await SnapshotAsync()).Mods.Keys, Is.EquivalentTo(new[] { "missing", "extra" }));
    }

    /// <summary>Composition captures caller-owned policy routes and collections instead of observing later mutations.</summary>
    [Test]
    public async Task ModData_CapturesPolicyCollectionAndExplicitPath()
    {
        var document = Document(2, """{}""");
        document.Mods["extra"] = new SaveDataBlock { Version = 1, Data = new JObject() };
        await WriteAsync(document);
        var path = new[] { 1, 3, 4 };
        var policies = new List<ModSavePolicy>
        {
            new("extra", 4, new SaveMigrationGraph([Edge(1, 2), Edge(2, 4), Edge(1, 3), Edge(3, 4)])) { VersionPath = path }
        };
        var repository = Repository(2, mods: policies);
        path[1] = 2;
        policies.Clear();
        var prepared = await repository.PrepareLoadAsync(1);
        Assert.That(prepared.MigrationPath.Select(static step => step.ToVersion), Is.EqualTo(new[] { 3, 4 }));
    }

    /// <summary>Active namespaces and versions are explicit validated policy, independent of any DTO interface.</summary>
    [Test]
    public void ModData_RejectsInvalidPolicy()
    {
        Assert.Throws<ArgumentException>(() => new ModSavePolicy("UpperCase", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ModSavePolicy("extra", 0));
        Assert.Throws<ArgumentException>(() => Repository(2, mods: [new ModSavePolicy("extra", 1), new ModSavePolicy("extra", 1)]));
    }

    /// <summary>Information-discarding upward transformations also require confirmation.</summary>
    [Test]
    public async Task Prepare_LossyUpgradeRequiresConfirmation()
    {
        await WriteAsync(Document(1, """{}"""));
        var step = new SaveMigrationStep(1, 2, "Drop obsolete data", static (block, _) =>
        {
            block.Version = 2;
            return Task.FromResult(block);
        }, isLossy: true);
        Assert.That((await Repository(2, new SaveMigrationGraph([step])).PrepareLoadAsync(1)).RequiresConfirmation, Is.True);
    }

    /// <summary>Each active mod migrates its own schema, with explicit routes independent of game versions.</summary>
    [Test]
    public async Task ModData_UsesOwnMigrationGraphAndExplicitPath()
    {
        var document = Document(2, """{}""");
        document.Mods["extra"] = new SaveDataBlock { Version = 1, Data = JObject.Parse("""{"Count":4}""") };
        await WriteAsync(document);
        var policy = new ModSavePolicy("extra", 4, new SaveMigrationGraph([Edge(1, 2), Edge(2, 4), Edge(1, 3), Edge(3, 4)]))
        { VersionPath = new[] { 1, 3, 4 } };
        var repository = Repository(2, mods: [policy]);
        var prepared = await repository.PrepareLoadAsync(1);
        Assert.That(prepared.Document.Mods["extra"].Version, Is.EqualTo(4));
        Assert.That(prepared.ModState.ForMod("extra").Read<ModCounter>()!.Count, Is.EqualTo(4));
        Assert.That(prepared.RequiresCommit, Is.True);
    }

    /// <summary>A fresh run owns an empty snapshot: no active or unavailable progress belongs to it yet.</summary>
    [Test]
    public async Task ModData_FreshRunDropsAllPreviousRunBlocks()
    {
        var document = Document(2, """{}""");
        document.Mods["extra"] = new SaveDataBlock { Version = 1, Data = JObject.Parse("""{"Count":99}""") };
        document.Mods["missing"] = new SaveDataBlock { Version = 3, Data = JObject.Parse("""{"Count":77}""") };
        await WriteAsync(document);
        var repository = Repository(2, mods: [new ModSavePolicy("extra", 1)]);
        await repository.SaveDocumentAsync(1, new CurrentSave(), repository.CreateModState());
        var saved = _codec.Decode(await SnapshotAsync());
        Assert.That(saved.Mods, Is.Empty);
    }

    /// <summary>A loaded session is the complete run snapshot, not a partial overlay on the destination slot.</summary>
    [Test]
    public async Task ModData_ExplicitSessionDoesNotResurrectAbsentDestinationBlocks()
    {
        var source = Document(2, """{}""");
        source.Mods["missing"] = new SaveDataBlock { Version = 7, Data = JObject.Parse("""{"Count":123}""") };
        await WriteAsync(source);
        var repository = Repository(2, mods: [new ModSavePolicy("extra", 1)]);
        var loaded = await repository.PrepareLoadAsync(1);
        var state = loaded.ModState;
        var destination = Document(2, """{}""");
        destination.Mods["other-run"] = new SaveDataBlock { Version = 9, Data = JObject.Parse("""{"Count":999}""") };
        await _storage.WriteSnapshotAsync("saves/slot_2/save", _codec.Encode(destination));
        await repository.SaveDocumentAsync(2, loaded.Data, state);
        var saved = _codec.Decode(await _storage.ReadSnapshotAsync("saves/slot_2/save"));
        Assert.That(saved.Mods.Keys, Is.EqualTo(new[] { "missing" }));
        Assert.That(saved.Mods["missing"].Data["Count"]!.Value<int>(), Is.EqualTo(123));
        await repository.SaveDocumentAsync(2, loaded.Data, state);
        Assert.That(_codec.Decode(await _storage.ReadSnapshotAsync("saves/slot_2/save")).Mods.ContainsKey("other-run"), Is.False);
    }

    /// <summary>Intentional new-run saves replace unsupported previous formats without invoking a compatibility parser.</summary>
    [Test]
    public async Task ModData_FreshRunCanReplaceUnsupportedSlotWithoutReadingIt()
    {
        await _storage.WriteSnapshotAsync(SaveKey, new StorageSnapshot(Encoding.UTF8.GetBytes("unsupported previous run")));
        var repository = Repository(2, mods: [new ModSavePolicy("extra", 1)]);
        var state = repository.CreateModState();
        state.ForMod("extra").Write(new ModCounter { Count = 1 });
        await repository.SaveDocumentAsync(1, new CurrentSave(), state);
        var saved = _codec.Decode(await SnapshotAsync());
        Assert.That(saved.Mods.Keys, Is.EqualTo(new[] { "extra" }));
        Assert.That(saved.Mods["extra"].Data["Count"]!.Value<int>(), Is.EqualTo(1));
    }

    /// <summary>Planning a highly branching graph with no route visits schemas instead of enumerating paths.</summary>
    [Test]
    public void Graph_BranchingMissingTargetIsBounded()
    {
        var edges = new List<SaveMigrationStep>();
        for (var version = 1; version < 100; version++)
        {
            edges.Add(Edge(version, version + 1));
            if (version < 99) edges.Add(Edge(version, version + 2));
        }
        var graph = new SaveMigrationGraph(edges);
        Assert.Throws<InvalidOperationException>(() => graph.Plan(1, 1000));
    }

    /// <summary>Partial/unknown save envelopes must not be guessed from property initializer defaults.</summary>
    [TestCase("""{"GameId":"test"}""")]
    [TestCase("""{"FormatVersion":1,"GameId":"test","Game":{"Data":{}},"Content":null,"Mods":{}}""")]
    [TestCase("""{"FormatVersion":1,"GameId":"test","Game":{"Version":1,"Data":{}},"Mods":{}}""")]
    [TestCase("""{"FormatVersion":1,"GameId":"test","Game":{"Version":1,"Data":{}},"Content":null,"Mods":{"extra":{"Data":{}}}}""")]
    public void Codec_RejectsMissingExplicitMetadata(string json) =>
        Assert.Throws<InvalidOperationException>(() => _codec.Decode(new StorageSnapshot(Encoding.UTF8.GetBytes(json))));

    /// <summary>Foreign drafts cannot replace another game's mod namespaces.</summary>
    [Test]
    public async Task ModData_RejectsForeignRepositoryDraft()
    {
        var first = Repository(2);
        var second = Repository(2);
        await Assert.ThrowsAsync<ArgumentException>(() => second.SaveDocumentAsync(1, new CurrentSave(), first.CreateModState()));
    }

    /// <summary>Malformed envelope identity is rejected before any migration is called.</summary>
    [Test]
    public async Task Envelope_RejectsForeignGameAndUnknownFormat()
    {
        var document = Document(1, """{}""");
        document.GameId = "another-game";
        await WriteAsync(document);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Repository(1).PrepareLoadAsync(1));
        Assert.Throws<InvalidOperationException>(() => _codec.Decode(new StorageSnapshot(Encoding.UTF8.GetBytes("""{"FormatVersion":9,"GameId":"test","Game":{"Version":1,"Data":{}}}"""))));
    }

    /// <summary>Restoration cannot escape the selected slot's backup namespace.</summary>
    [TestCase("../slot_2/save")]
    [TestCase("backups/../../save")]
    [TestCase("/absolute/save")]
    public async Task Restore_RejectsEscapingKeys(string key) =>
        await Assert.ThrowsAsync<ArgumentException>(() => Repository(2).RestoreBackupAsync(1, key));

    /// <summary>Builds a repository using explicitly owned schema policy.</summary>
    private SaveRepository<CurrentSave> Repository(int version, SaveMigrationGraph? graph = null,
        IReadOnlyList<ModSavePolicy>? mods = null, IStorage? storage = null) => new(storage ?? _storage, Configuration,
        new SaveRepositoryOptions<CurrentSave>
        {
            GameId = "test", CurrentVersion = version, Migrations = graph ?? new SaveMigrationGraph(),
            ModPolicies = mods ?? [], Validate = static data =>
            {
                if (data.Position.X < 0) throw new InvalidOperationException("Negative position is invalid.");
            }
        });

    /// <summary>Creates a deterministic no-op structure conversion with an explicit version update.</summary>
    private static SaveMigrationStep Edge(int from, int to) => new(from, to, $"""{from} to {to}""", (block, _) =>
    {
        block.Version = to;
        return Task.FromResult(block);
    });

    /// <summary>Renames/retypes the historical scalar while retaining unrelated document fields.</summary>
    private static SaveMigrationStep PositionUpgrade() => new(1, 2, "Convert scalar position", (block, _) =>
    {
        var parts = block.Data["Position"]!.Value<string>()!.Split(',');
        block.Data["Position"] = new JObject { ["X"] = int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), ["Y"] = int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) };
        block.Version = 2;
        return Task.FromResult(block);
    });

    /// <summary>Creates test envelope data without constructing any current DTO.</summary>
    private static SaveDocument Document(int version, string payload) => new()
    { GameId = "test", Game = new SaveDataBlock { Version = version, Data = JObject.Parse(payload) } };

    /// <summary>Writes an entire raw document through the production snapshot boundary.</summary>
    private Task WriteAsync(SaveDocument document) => _storage.WriteSnapshotAsync(SaveKey, _codec.Encode(document));
    /// <summary>Reads original bytes without DTO conversion.</summary>
    private Task<StorageSnapshot> SnapshotAsync() => _storage.ReadSnapshotAsync(SaveKey);
    /// <summary>Gets the physical file used for byte-level evidence.</summary>
    private string SavePath => Path.Combine(SlotPath, "save.json");
    /// <summary>Gets the isolated first slot directory.</summary>
    private string SlotPath => Path.Combine(_root, "saves", "slot_1");

    /// <summary>Injects failures at exact snapshot-write boundaries while retaining production IO behavior.</summary>
    private IStorage StorageProxy(Func<string, Task> beforeWrite)
    {
        var proxy = new Mock<IStorage>(MockBehavior.Strict);
        proxy.Setup(value => value.ExistsAsync(It.IsAny<string>())).Returns((string key) => _storage.ExistsAsync(key));
        var raw = proxy.As<IStorageBackup>();
        raw.Setup(value => value.ReadSnapshotAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string key, CancellationToken token) => _storage.ReadSnapshotAsync(key, token));
        raw.Setup(value => value.WriteSnapshotAsync(It.IsAny<string>(), It.IsAny<StorageSnapshot>(), It.IsAny<CancellationToken>()))
            .Returns(async (string key, StorageSnapshot snapshot, CancellationToken token) =>
            {
                await beforeWrite(key).ConfigureAwait(false);
                await _storage.WriteSnapshotAsync(key, snapshot, token).ConfigureAwait(false);
            });
        return proxy.Object;
    }

    /// <summary>Current DTO whose object position cannot consume historical scalar position.</summary>
    public sealed class CurrentSave : IData
    {
        /// <summary>Gets or sets current structured coordinates.</summary>
        public Coordinates Position { get; set; } = new();
    }
    /// <summary>Plain structured coordinates.</summary>
    public sealed class Coordinates
    {
        /// <summary>Gets or sets horizontal position.</summary>
        public int X { get; set; }
        /// <summary>Gets or sets vertical position.</summary>
        public int Y { get; set; }
    }
    /// <summary>Portable mod-specific progress.</summary>
    public sealed class ModCounter
    {
        /// <summary>Gets or sets accumulated progress.</summary>
        public int Count { get; set; }
    }
}
