// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text;
using System.Threading;
using GFramework.Core.Abstractions.Serializer;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Content;
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

namespace GFramework.Game.Tests.Data;

/// <summary>Exercises persisted content compatibility and lossless, player-approved recovery.</summary>
[TestFixture]
public sealed class ContentSaveTests
{
    private string _root = null!;
    private FileStorage _storage = null!;
    private static readonly ContentReference BaseArena = new("arenas", new ContentId("base:arena"));
    private static readonly ContentReference ModArena = new("arenas", new ContentId("extra:arena"));
    private static readonly SaveConfiguration Configuration = new() { SaveRoot = "saves", SaveFileName = "save" };

    /// <summary>Creates isolated physical files for byte-for-byte assertions.</summary>
    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "gframework-content-saves-" + Guid.NewGuid().ToString("N"));
        _storage = new FileStorage(_root, new JsonSerializer(), ".json");
    }

    /// <summary>Releases storage locks and removes only this test's temporary directory.</summary>
    [TearDown]
    public void TearDown()
    {
        _storage.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    /// <summary>Metadata survives serialization and a newly created repository without a sidecar file.</summary>
    [Test]
    public async Task SaveAndNewRepositoryLoad_PersistOneFileWithContentMetadata()
    {
        var catalog = CreateCatalog(false);
        await CreateRepository(catalog).SaveAsync(1, new ContentSave { Value = 7 });
        var loaded = await CreateRepository(catalog).LoadAsync(1);
        Assert.Multiple(() =>
        {
            Assert.That(loaded.Value, Is.EqualTo(7));
            Assert.That(loaded.Content!.Packs.Single().Id, Is.EqualTo("base"));
            Assert.That(Directory.GetFiles(SlotPath), Has.Length.EqualTo(1));
        });
    }

    /// <summary>Missing packages stop loading before a version migration can mutate or write the file.</summary>
    [Test]
    public async Task MissingPack_IsCheckedBeforeMigrationAndDoesNotWrite()
    {
        await CreateRepository(CreateCatalog(true)).SaveAsync(1,
            new ContentSave { ArenaId = "extra:arena", Version = 1 });
        var bytes = await File.ReadAllBytesAsync(SavePath);
        var migrations = 0;
        var repository = CreateRepository(CreateCatalog(false));
        repository.RegisterMigration(new SaveMigration(() => migrations++));
        var exception = await Assert.ThrowsAsync<ContentSaveCompatibilityException>(() => repository.LoadAsync(1));
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Report.Issues.Any(static issue => issue.Kind == ContentSaveIssueKind.MissingPack), Is.True);
            Assert.That(migrations, Is.Zero);
            Assert.That(File.ReadAllBytes(SavePath), Is.EqualTo(bytes));
        });
    }

    /// <summary>A successful repair backs up every original byte, migrates once, commits and remains reversible.</summary>
    [Test]
    public async Task RecoveryAndRestore_PreserveUnknownFieldsAndOriginalEncoding()
    {
        await CreateRepository(CreateCatalog(true)).SaveAsync(1,
            new ContentSave { ArenaId = "extra:arena", Version = 1, Value = 5 });
        var text = await File.ReadAllTextAsync(SavePath);
        text = text.Insert(text.LastIndexOf('}'), ",\n  \"UnknownFutureField\": {\"untouched\": true}\n");
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
        await File.WriteAllBytesAsync(SavePath, bytes);
        var hookCalls = 0;
        var repository = CreateRepository(CreateCatalog(false), recover: (data, _, _) =>
        {
            hookCalls++;
            Assert.That(Directory.GetFiles(Path.Combine(SlotPath, "backups")), Has.Length.EqualTo(1));
            data.ArenaId = "base:arena";
            return Task.FromResult(data);
        });
        repository.RegisterMigration(new SaveMigration(static () => { }));
        var report = await repository.InspectContentAsync(1);
        var result = await repository.RecoverContentAsync(1, report);
        var backupPath = Path.Combine(SlotPath, result.BackupKey + ".json");
        var recoveredBytes = await File.ReadAllBytesAsync(SavePath);
        Assert.Multiple(() =>
        {
            Assert.That(hookCalls, Is.EqualTo(1));
            Assert.That(result.Data.Version, Is.EqualTo(2));
            Assert.That(result.Data.ArenaId, Is.EqualTo("base:arena"));
            Assert.That(File.ReadAllBytes(backupPath), Is.EqualTo(bytes));
            Assert.That(result.Data.Content!.Packs.Select(static pack => pack.Id), Is.EqualTo(new[] { "base" }));
        });
        Assert.That((await CreateRepository(CreateCatalog(false)).LoadAsync(1)).ArenaId, Is.EqualTo("base:arena"));
        var preRestore = await repository.RestoreBackupAsync(1, result.BackupKey);
        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllBytes(SavePath), Is.EqualTo(bytes));
            Assert.That(File.ReadAllBytes(Path.Combine(SlotPath, preRestore + ".json")), Is.EqualTo(recoveredBytes));
        });
    }

    /// <summary>A stale confirmation cannot repair a file that changed after the dialog was shown.</summary>
    [Test]
    public async Task ChangedFile_InvalidatesApprovalBeforeBackupOrHook()
    {
        var oldCatalog = CreateCatalog(true);
        await CreateRepository(oldCatalog).SaveAsync(1, new ContentSave { ArenaId = "extra:arena" });
        var calls = 0;
        var repository = CreateRepository(CreateCatalog(false), recover: (data, _, _) =>
        {
            calls++;
            data.ArenaId = "base:arena";
            return Task.FromResult(data);
        });
        var report = await repository.InspectContentAsync(1);
        await CreateRepository(oldCatalog).SaveAsync(1, new ContentSave { ArenaId = "extra:arena", Value = 99 });
        var bytes = await File.ReadAllBytesAsync(SavePath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.RecoverContentAsync(1, report));
        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.Zero);
            Assert.That(File.ReadAllBytes(SavePath), Is.EqualTo(bytes));
            Assert.That(Directory.Exists(Path.Combine(SlotPath, "backups")), Is.False);
        });
    }

    /// <summary>A failed backup prevents the game's potentially destructive recovery code from running.</summary>
    [Test]
    public async Task BackupFailure_PreventsHookAndPreservesOriginal()
    {
        await CreateRepository(CreateCatalog(true)).SaveAsync(1, new ContentSave { ArenaId = "extra:arena" });
        await File.WriteAllTextAsync(Path.Combine(SlotPath, "backups"), "a file blocks the directory");
        var calls = 0;
        var repository = CreateRepository(CreateCatalog(false), recover: (data, _, _) =>
        {
            calls++;
            return Task.FromResult(data);
        });
        var bytes = await File.ReadAllBytesAsync(SavePath);
        var report = await repository.InspectContentAsync(1);
        await Assert.ThrowsAsync<IOException>(() => repository.RecoverContentAsync(1, report));
        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.Zero);
            Assert.That(File.ReadAllBytes(SavePath), Is.EqualTo(bytes));
        });
    }

    /// <summary>Failures and cancellation after backup keep both the original and a useful lossless backup.</summary>
    [TestCase("hook")]
    [TestCase("validation")]
    [TestCase("cancellation")]
    public async Task FailureAfterBackup_PreservesOriginal(string failure)
    {
        await CreateRepository(CreateCatalog(true)).SaveAsync(1, new ContentSave { ArenaId = "extra:arena" });
        using var cancellation = new CancellationTokenSource();
        var bytes = await File.ReadAllBytesAsync(SavePath);
        var repository = CreateRepository(CreateCatalog(false), recover: (data, _, _) =>
        {
            data.ArenaId = "base:arena";
            if (string.Equals(failure, "hook", StringComparison.Ordinal)) throw new InvalidOperationException("Game cannot repair this save");
            if (string.Equals(failure, "cancellation", StringComparison.Ordinal)) cancellation.Cancel();
            if (string.Equals(failure, "validation", StringComparison.Ordinal)) data.Value = -1;
            return Task.FromResult(data);
        });
        var report = await repository.InspectContentAsync(1);
        if (string.Equals(failure, "cancellation", StringComparison.Ordinal))
            await Assert.ThrowsAsync<OperationCanceledException>(() => repository.RecoverContentAsync(1, report, cancellation.Token));
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.RecoverContentAsync(1, report, cancellation.Token));
        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllBytes(SavePath), Is.EqualTo(bytes));
            var backup = Directory.GetFiles(Path.Combine(SlotPath, "backups")).Single();
            Assert.That(File.ReadAllBytes(backup), Is.EqualTo(bytes));
        });
    }

    /// <summary>Serialization failure at final commit preserves the original file and its completed backup.</summary>
    [Test]
    public async Task RecoveryWriteFailure_PreservesOriginalAndBackup()
    {
        await CreateRepository(CreateCatalog(true)).SaveAsync(1, new ContentSave { ArenaId = "extra:arena" });
        var bytes = await File.ReadAllBytesAsync(SavePath);
        var serializer = new ControlledSerializer();
        using var storage = new FileStorage(_root, serializer, ".json");
        var repository = CreateRepository(CreateCatalog(false), recover: (data, _, _) =>
        {
            data.ArenaId = "base:arena";
            serializer.FailWrite = true;
            return Task.FromResult(data);
        }, storage: storage);
        var report = await repository.InspectContentAsync(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.RecoverContentAsync(1, report));
        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllBytes(SavePath), Is.EqualTo(bytes));
            Assert.That(File.ReadAllBytes(Directory.GetFiles(Path.Combine(SlotPath, "backups")).Single()), Is.EqualTo(bytes));
        });
    }

    /// <summary>Known old formats require explicit import rather than automatic metadata fabrication during Load.</summary>
    [Test]
    public async Task LegacyImport_RequiresDeclarationAndBacksUpBeforeMigration()
    {
        await _storage.WriteAsync("saves/slot_1/save", new ContentSave { Version = 1 });
        var bytes = await File.ReadAllBytesAsync(SavePath);
        var catalog = CreateCatalog(false);
        var repository = CreateRepository(catalog, acceptLegacy: static data => data.Version == 1);
        repository.RegisterMigration(new SaveMigration(static () => { }));
        await Assert.ThrowsAsync<ContentSaveCompatibilityException>(() => repository.LoadAsync(1));
        var report = await repository.InspectContentAsync(1);
        var result = await repository.ImportLegacyAsync(1, report);
        Assert.Multiple(() =>
        {
            Assert.That(report.IsLegacy, Is.True);
            Assert.That(result.Data.Version, Is.EqualTo(2));
            Assert.That(result.Data.Content, Is.Not.Null);
            Assert.That(File.ReadAllBytes(Path.Combine(SlotPath, result.BackupKey + ".json")), Is.EqualTo(bytes));
        });
        Assert.That((await CreateRepository(catalog).LoadAsync(1)).Version, Is.EqualTo(2));
    }

    /// <summary>A removed row is detected even when the providing package itself is still enabled.</summary>
    [Test]
    public async Task MissingReference_IsBlockingEvenWithMatchingPackageStamps()
    {
        await CreateRepository(CreateCatalog(true)).SaveAsync(1, new ContentSave { ArenaId = "extra:arena" });
        var repository = CreateRepository(CreateCatalog(true, includeModReference: false));
        var report = await repository.InspectContentAsync(1);
        Assert.That(report.Issues.Single().Kind, Is.EqualTo(ContentSaveIssueKind.MissingContent));
        await Assert.ThrowsAsync<ContentSaveCompatibilityException>(() => repository.LoadAsync(1));
    }

    /// <summary>Adding content is advisory and ordinary loading does not rewrite metadata merely to hide differences.</summary>
    [Test]
    public async Task AdditionalPack_IsAdvisoryAndDoesNotRewriteFile()
    {
        await CreateRepository(CreateCatalog(false)).SaveAsync(1, new ContentSave());
        var bytes = await File.ReadAllBytesAsync(SavePath);
        var repository = CreateRepository(CreateCatalog(true));
        var report = await repository.InspectContentAsync(1);
        Assert.That(report.RequiresRecovery, Is.False);
        Assert.That(report.Issues.Single().Kind, Is.EqualTo(ContentSaveIssueKind.AdditionalPack));
        await repository.LoadAsync(1);
        Assert.That(File.ReadAllBytes(SavePath), Is.EqualTo(bytes));
    }

    /// <summary>Installed packages explicitly declare which prior save versions they accept.</summary>
    [TestCase("1.1.0", true)]
    [TestCase("0.9.0", false)]
    [TestCase("2.0.0", false)]
    public async Task DeclaredCompatibilityRange_AppliesInclusiveMinimumAndExclusiveMaximum(string savedVersion, bool compatible)
    {
        await CreateRepository(CreateCatalog(false, baseVersion: savedVersion)).SaveAsync(1, new ContentSave());
        var current = CreateCatalog(false, baseVersion: "3.0.0", baseFingerprint: "new-hash",
            compatibleVersions: new ContentVersionRange { MinimumInclusive = "1.0.0", MaximumExclusive = "2.0.0" });
        var report = await CreateRepository(current).InspectContentAsync(1);
        Assert.That(report.RequiresRecovery, Is.EqualTo(!compatible));
    }

    /// <summary>A broad version range does not hide changes to a local package that kept its version number.</summary>
    [Test]
    public async Task SameVersionEditedFingerprint_RemainsBlockingDespiteDeclaredRange()
    {
        await CreateRepository(CreateCatalog(false)).SaveAsync(1, new ContentSave());
        var current = CreateCatalog(false, baseFingerprint: "edited-hash", compatibleVersions: new ContentVersionRange());
        var report = await CreateRepository(current).InspectContentAsync(1);
        Assert.That(report.Issues.Single().Kind, Is.EqualTo(ContentSaveIssueKind.IncompatiblePack));
    }

    /// <summary>The slot gate prevents a concurrent Save from interleaving between backup and recovery commit.</summary>
    [Test]
    public async Task Recovery_SerializesConcurrentSaveForTheSameSlot()
    {
        await CreateRepository(CreateCatalog(true)).SaveAsync(1, new ContentSave { ArenaId = "extra:arena" });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = CreateRepository(CreateCatalog(false), recover: async (data, _, _) =>
        {
            entered.SetResult();
            await proceed.Task.ConfigureAwait(false);
            data.ArenaId = "base:arena";
            return data;
        });
        var report = await repository.InspectContentAsync(1);
        var recovery = repository.RecoverContentAsync(1, report);
        await entered.Task;
        var save = repository.SaveAsync(1, new ContentSave { Value = 99 });
        Assert.That(save.IsCompleted, Is.False);
        proceed.SetResult();
        await recovery;
        await save;
        Assert.That((await repository.LoadAsync(1)).Value, Is.EqualTo(99));
    }

    /// <summary>Restore cannot select another slot or an arbitrary physical file through a crafted backup key.</summary>
    [TestCase("../slot_2/save")]
    [TestCase("backups/../../save")]
    [TestCase("/absolute/save")]
    public async Task Restore_RejectsKeysOutsideTheSlotBackupDirectory(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository(CreateCatalog(false)).RestoreBackupAsync(1, key));
    }

    /// <summary>Gets the only file managed by slot 1.</summary>
    private string SavePath => Path.Combine(SlotPath, "save.json");
    /// <summary>Gets this fixture's isolated slot directory.</summary>
    private string SlotPath => Path.Combine(_root, "saves", "slot_1");

    /// <summary>Creates a repository with game-owned repair and reference semantics.</summary>
    private SaveRepository<ContentSave> CreateRepository(ContentCatalog catalog,
        Func<ContentSave, ContentSaveReport, CancellationToken, Task<ContentSave>>? recover = null,
        Func<ContentSave, bool>? acceptLegacy = null, FileStorage? storage = null)
    {
        return new SaveRepository<ContentSave>(storage ?? _storage, Configuration, new ContentSaveOptions<ContentSave>
        {
            CurrentCatalog = () => catalog,
            EnumerateReferences = static data => [new ContentReference("arenas", new ContentId(data.ArenaId))],
            RecoverAsync = recover,
            AcceptLegacy = acceptLegacy,
            Validate = static data =>
            {
                if (data.Value < 0) throw new InvalidOperationException("Negative business value is invalid");
            }
        });
    }

    /// <summary>Publishes a minimal catalog using the same immutable internal state as the real loader.</summary>
    private ContentCatalog CreateCatalog(bool includeMod, bool includeModReference = true,
        string baseVersion = "1.0.0", string baseFingerprint = "base-hash", ContentVersionRange? compatibleVersions = null)
    {
        var packages = new List<ContentPack>
        {
            new(_root, new ContentPackManifest
            {
                Id = "base", GameId = "test", Name = "Base", Version = baseVersion,
                SaveCompatibleVersions = compatibleVersions
            }, baseFingerprint)
        };
        var references = new HashSet<ContentReference> { BaseArena };
        if (includeMod)
        {
            packages.Add(new ContentPack(_root, new ContentPackManifest { Id = "extra", GameId = "test", Name = "Extra" }, "extra-hash"));
            if (includeModReference) references.Add(ModArena);
        }
        return new ContentCatalog(new ContentLoadPlan(packages), new Dictionary<string, IConfigTable>(StringComparer.Ordinal), references,
            new Dictionary<ContentReference, IReadOnlyList<ContentSource>>(),
            new Dictionary<(ContentReference Reference, string Field), ContentSource>());
    }

    /// <summary>Portable test DTO with opt-in content and a game-owned arena reference.</summary>
    public sealed class ContentSave : IVersionedData, IContentAwareSaveData
    {
        /// <inheritdoc />
        public int Version { get; set; } = 2;
        /// <inheritdoc />
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        /// <inheritdoc />
        public ContentSnapshot? Content { get; set; }
        /// <summary>Gets or sets the selected arena's stable identity.</summary>
        public string ArenaId { get; set; } = "base:arena";
        /// <summary>Gets or sets a business value validated after recovery.</summary>
        public int Value { get; set; }
    }

    /// <summary>Allows only the final DTO serialization to fail while lossless backup operations continue to work.</summary>
    private sealed class ControlledSerializer : ISerializer
    {
        private readonly JsonSerializer _inner = new();
        /// <summary>Gets or sets whether subsequent DTO writes should fail.</summary>
        internal bool FailWrite { get; set; }
        /// <inheritdoc />
        public string Serialize<T>(T value) => FailWrite
            ? throw new InvalidOperationException("Cannot serialize repaired data")
            : _inner.Serialize(value);
        /// <inheritdoc />
        public T Deserialize<T>(string data) => _inner.Deserialize<T>(data);
    }

    /// <summary>Verifies content checking occurs before this otherwise valid schema migration.</summary>
    private sealed class SaveMigration(Action onMigrate) : ISaveMigration<ContentSave>
    {
        /// <inheritdoc />
        public int FromVersion => 1;
        /// <inheritdoc />
        public int ToVersion => 2;
        /// <inheritdoc />
        public ContentSave Migrate(ContentSave oldData)
        {
            onMigrate();
            oldData.Version = 2;
            return oldData;
        }
    }
}
