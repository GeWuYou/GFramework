using System.IO;
using System.Threading;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Storage;
using GFramework.Core.Architectures;
using GFramework.Core.Events;
using GFramework.Core.Ioc;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Data.Events;
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

namespace GFramework.Game.Tests.Data;

/// <summary>
///     覆盖文件存储、槽位存档仓库和统一设置仓库的持久化行为测试。
/// </summary>
[TestFixture]
public class PersistenceTests
{
    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "gframework-persistence", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    ///     验证文件存储能够持久化数据，并拒绝包含路径逃逸的非法键。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task FileStorage_PersistsDataAndRejectsIllegalKeys()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer(), ".json");

        var saved = new TestSimpleData { Value = 5 };
        await storage.WriteAsync("folder/item", saved).ConfigureAwait(false);

        var loaded = await storage.ReadAsync<TestSimpleData>("folder/item").ConfigureAwait(false);
        Assert.That(loaded.Value, Is.EqualTo(saved.Value));

        Assert.ThrowsAsync<ArgumentException>(async () => await storage.WriteAsync("../escape", new TestSimpleData()).ConfigureAwait(false));
    }

    /// <summary>
    ///     验证槽位存档仓库的保存、加载、列举和删除行为。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task SaveRepository_ManagesSlots()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer());
        var config = new SaveConfiguration
        {
            SaveRoot = "saves",
            SaveSlotPrefix = "slot_",
            SaveFileName = "save.json"
        };

        var repository = new SaveRepository<TestSaveData>(storage, config);
        var data = new TestSaveData { Name = "hero" };

        await repository.SaveAsync(1, data);
        Assert.That(await repository.ExistsAsync(1));

        var loaded = await repository.LoadAsync(1);
        Assert.That(loaded.Name, Is.EqualTo(data.Name));

        var slots = await repository.ListSlotsAsync();
        Assert.That(slots, Is.EqualTo(new[] { 1 }));

        await repository.DeleteAsync(1);
        Assert.That(await repository.ExistsAsync(1), Is.False);
    }

    /// <summary>
    ///     验证存档仓库在加载旧版本数据时会执行迁移链并回写升级后的最新版本。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task SaveRepository_LoadAsync_Should_Apply_Migrations_And_Persist_Upgraded_Save()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer());
        var config = new SaveConfiguration
        {
            SaveRoot = "saves",
            SaveSlotPrefix = "slot_",
            SaveFileName = "save"
        };

        var writer = new SaveRepository<TestVersionedSaveData>(storage, config);
        await writer.SaveAsync(1, new TestVersionedSaveData
        {
            Name = "hero",
            Level = 5,
            Experience = 0,
            Version = 1
        });

        var repository = new SaveRepository<TestVersionedSaveData>(storage, config)
            .RegisterMigration(new TestSaveMigrationV1ToV2())
            .RegisterMigration(new TestSaveMigrationV2ToV3());

        var loaded = await repository.LoadAsync(1);
        var persisted = await storage.ReadAsync<TestVersionedSaveData>("saves/slot_1/save").ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Version, Is.EqualTo(3));
            Assert.That(loaded.Experience, Is.EqualTo(500));
            Assert.That(loaded.Name, Is.EqualTo("hero-v2"));
            Assert.That(persisted.Version, Is.EqualTo(3));
            Assert.That(persisted.Experience, Is.EqualTo(500));
            Assert.That(persisted.Name, Is.EqualTo("hero-v2"));
        });
    }

    /// <summary>
    ///     验证非版本化存档类型不能注册迁移器，避免构建无效迁移管线。
    /// </summary>
    /// <exception cref="InvalidOperationException">当存档类型未实现 <see cref="IVersionedData" /> 时抛出。</exception>
    [Test]
    public void SaveRepository_RegisterMigration_For_NonVersioned_Save_Should_Throw()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer());
        var config = new SaveConfiguration();
        var repository = new SaveRepository<TestSaveData>(storage, config);

        Assert.Throws<InvalidOperationException>(() => repository.RegisterMigration(new TestNonVersionedMigration()));
    }

    /// <summary>
    ///     验证同一源版本不能重复注册迁移器，避免迁移链配置被静默覆盖。
    /// </summary>
    /// <exception cref="InvalidOperationException">当同一源版本重复注册迁移器时抛出。</exception>
    [Test]
    public void SaveRepository_RegisterMigration_Should_Reject_Duplicate_FromVersion()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer());
        var config = new SaveConfiguration();
        var repository = new SaveRepository<TestVersionedSaveData>(storage, config);

        repository.RegisterMigration(new TestSaveMigrationV1ToV2());

        var exception = Assert.Throws<InvalidOperationException>(
            () => repository.RegisterMigration(new TestDuplicateSaveMigrationV1ToV2()));

        Assert.That(exception!.Message, Does.Contain("Duplicate save migration registration"));
    }

    /// <summary>
    ///     验证当迁移链缺少中间版本时，加载旧存档会明确失败而不是静默返回不完整数据。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    /// <exception cref="InvalidOperationException">当从旧版本到当前版本的迁移链不完整时抛出。</exception>
    [Test]
    public async Task SaveRepository_LoadAsync_Should_Throw_When_Migration_Chain_Is_Incomplete()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer());
        var config = new SaveConfiguration
        {
            SaveRoot = "saves",
            SaveSlotPrefix = "slot_",
            SaveFileName = "save"
        };

        var writer = new SaveRepository<TestVersionedSaveData>(storage, config);
        await writer.SaveAsync(1, new TestVersionedSaveData
        {
            Name = "legacy",
            Level = 3,
            Experience = 0,
            Version = 1
        });

        var repository = new SaveRepository<TestVersionedSaveData>(storage, config)
            .RegisterMigration(new TestSaveMigrationV1ToV2());

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await repository.LoadAsync(1).ConfigureAwait(false));
        Assert.That(exception!.Message, Does.Contain("from version 2"));
    }

    /// <summary>
    ///     验证迁移器声明的目标版本必须与返回数据上的实际版本一致，避免错误迁移结果被静默接受。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    /// <exception cref="InvalidOperationException">当迁移器返回的版本与声明目标版本不一致时抛出。</exception>
    [Test]
    public async Task SaveRepository_LoadAsync_Should_Throw_When_Migration_Result_Version_Does_Not_Match_Declaration()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer());
        var config = new SaveConfiguration
        {
            SaveRoot = "saves",
            SaveSlotPrefix = "slot_",
            SaveFileName = "save"
        };

        var writer = new SaveRepository<TestVersionedSaveData>(storage, config);
        await writer.SaveAsync(1, new TestVersionedSaveData
        {
            Name = "legacy",
            Level = 3,
            Experience = 0,
            Version = 1
        });

        var repository = new SaveRepository<TestVersionedSaveData>(storage, config)
            .RegisterMigration(new TestSaveMigrationV1ToV2ReturningV3());

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await repository.LoadAsync(1).ConfigureAwait(false));
        var persisted = await storage.ReadAsync<TestVersionedSaveData>("saves/slot_1/save").ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("declared target version 2"));
            Assert.That(persisted.Version, Is.EqualTo(1));
            Assert.That(persisted.Name, Is.EqualTo("legacy"));
            Assert.That(persisted.Level, Is.EqualTo(3));
            Assert.That(persisted.Experience, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证加载流程会在开始迁移前固定迁移表快照，避免并发注册让同一次加载看到变化中的链路。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    /// <exception cref="InvalidOperationException">当快照中缺少后续迁移链时抛出。</exception>
    [Test]
    public async Task SaveRepository_LoadAsync_Should_Use_Migration_Snapshot_When_Registrations_Change_Concurrently()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer());
        var config = new SaveConfiguration
        {
            SaveRoot = "saves",
            SaveSlotPrefix = "slot_",
            SaveFileName = "save"
        };

        var writer = new SaveRepository<TestVersionedSaveData>(storage, config);
        await writer.SaveAsync(1, new TestVersionedSaveData
        {
            Name = "legacy",
            Level = 3,
            Experience = 0,
            Version = 1
        });

        using var migrationStarted = new ManualResetEventSlim(false);
        using var continueMigration = new ManualResetEventSlim(false);

        var repository = new SaveRepository<TestVersionedSaveData>(storage, config)
            .RegisterMigration(new BlockingSaveMigrationV1ToV2(migrationStarted, continueMigration));

        var loadTask = repository.LoadAsync(1);

        Assert.That(migrationStarted.Wait(TimeSpan.FromSeconds(5)), Is.True, "First migration step did not start in time.");

        repository.RegisterMigration(new TestSaveMigrationV2ToV3());
        continueMigration.Set();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await loadTask.ConfigureAwait(false));
        var persisted = await storage.ReadAsync<TestVersionedSaveData>("saves/slot_1/save").ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("from version 2"));
            Assert.That(persisted.Version, Is.EqualTo(1));
            Assert.That(persisted.Name, Is.EqualTo("legacy"));
            Assert.That(persisted.Level, Is.EqualTo(3));
            Assert.That(persisted.Experience, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证统一设置仓库能够保存、重新加载并批量读取已注册的设置数据。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task UnifiedSettingsDataRepository_RoundTripsDataAndLoadAll()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer());
        var serializer = new JsonSerializer();
        var repo = new UnifiedSettingsDataRepository(
            storage,
            serializer,
            new DataRepositoryOptions { EnableEvents = false });

        var location = new TestDataLocation("settings/choice");
        repo.RegisterDataType(location, typeof(TestSimpleData));

        var data = new TestSimpleData { Value = 42 };
        await repo.SaveAsync(location, data);

        using var storage2 = new FileStorage(root, new JsonSerializer());
        var repo2 = new UnifiedSettingsDataRepository(
            storage2,
            serializer,
            new DataRepositoryOptions { EnableEvents = false });
        repo2.RegisterDataType(location, typeof(TestSimpleData));

        var loaded = await repo2.LoadAsync<TestSimpleData>(location);
        Assert.That(loaded.Value, Is.EqualTo(data.Value));

        var all = await repo2.LoadAllAsync();
        Assert.That(all.Keys, Contains.Item(location.Key));
        Assert.That(all[location.Key], Is.TypeOf<TestSimpleData>());
    }

    /// <summary>
    ///     验证通用数据仓库在覆盖已有数据时会创建备份文件，并保留覆盖前的旧值。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task DataRepository_SaveAsync_Should_Create_Backup_When_Overwriting_Existing_Data()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer(), ".json");
        var repository = new DataRepository(
            storage,
            new DataRepositoryOptions
            {
                AutoBackup = true,
                EnableEvents = false
            });
        var location = new TestDataLocation("options", namespaceValue: "profile");

        await repository.SaveAsync(location, new TestSimpleData { Value = 1 });
        await repository.SaveAsync(location, new TestSimpleData { Value = 2 });

        var current = await repository.LoadAsync<TestSimpleData>(location);
        var backup = await storage.ReadAsync<TestSimpleData>("profile/options.backup");

        Assert.Multiple(() =>
        {
            Assert.That(current.Value, Is.EqualTo(2));
            Assert.That(backup.Value, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     验证通用数据仓库的批量保存只发送批量事件，不重复发送单项保存事件。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task DataRepository_SaveAllAsync_Should_Emit_Only_Batch_Event()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer(), ".json");
        var repository = new DataRepository(
            storage,
            new DataRepositoryOptions
            {
                AutoBackup = false,
                EnableEvents = true
            });
        var context = CreateEventContext();
        ((IContextAware)repository).SetContext(context);

        var location1 = new TestDataLocation("graphics", namespaceValue: "settings");
        var location2 = new TestDataLocation("audio", namespaceValue: "settings");
        var savedEventCount = 0;
        var batchEventCount = 0;

        context.RegisterEvent<DataSavedEvent<TestSimpleData>>(_ => savedEventCount++);
        context.RegisterEvent<DataBatchSavedEvent>(_ => batchEventCount++);

        await repository.SaveAllAsync(
        [
            (location1, new TestSimpleData { Value = 10 }),
            (location2, new TestSimpleData { Value = 20 })
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(savedEventCount, Is.Zero);
            Assert.That(batchEventCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     验证批量覆盖已有数据时仍会按每个条目的运行时类型执行备份与回写，而不会退化为 <see cref="IData" />。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task DataRepository_SaveAllAsync_Should_Preserve_Runtime_Types_When_Overwriting_Existing_Data()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer(), ".json");
        var repository = new DataRepository(
            storage,
            new DataRepositoryOptions
            {
                AutoBackup = true,
                EnableEvents = false
            });
        var numberLocation = new TestDataLocation("graphics", namespaceValue: "settings");
        var textLocation = new TestDataLocation("profile", namespaceValue: "settings");

        await repository.SaveAllAsync(
        [
            (numberLocation, new TestSimpleData { Value = 1 }),
            (textLocation, new TestNamedData { Name = "old-name" })
        ]);

        await repository.SaveAllAsync(
        [
            (numberLocation, new TestSimpleData { Value = 2 }),
            (textLocation, new TestNamedData { Name = "new-name" })
        ]);

        var currentNumber = await repository.LoadAsync<TestSimpleData>(numberLocation);
        var currentText = await repository.LoadAsync<TestNamedData>(textLocation);
        var backupNumber = await storage.ReadAsync<TestSimpleData>("settings/graphics.backup");
        var backupText = await storage.ReadAsync<TestNamedData>("settings/profile.backup");

        Assert.Multiple(() =>
        {
            Assert.That(currentNumber.Value, Is.EqualTo(2));
            Assert.That(currentText.Name, Is.EqualTo("new-name"));
            Assert.That(backupNumber.Value, Is.EqualTo(1));
            Assert.That(backupText.Name, Is.EqualTo("old-name"));
        });
    }

    /// <summary>
    ///     验证统一设置仓库在批量覆盖时会为整个聚合文件创建备份，并只发送批量事件。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task UnifiedSettingsDataRepository_SaveAllAsync_Should_Create_Backup_And_Emit_Only_Batch_Event()
    {
        var root = CreateTempRoot();
        var location1 = new TestDataLocation("settings/graphics");
        var location2 = new TestDataLocation("settings/audio");

        using (var seedStorage = new FileStorage(root, new JsonSerializer(), ".json"))
        {
            var seedRepository = new UnifiedSettingsDataRepository(
                seedStorage,
                new JsonSerializer(),
                new DataRepositoryOptions
                {
                    AutoBackup = true,
                    EnableEvents = false
                });
            seedRepository.RegisterDataType(location1, typeof(TestSimpleData));
            seedRepository.RegisterDataType(location2, typeof(TestSimpleData));

            await seedRepository.SaveAsync(location1, new TestSimpleData { Value = 1 });
        }

        using var storage = new FileStorage(root, new JsonSerializer(), ".json");
        var repository = new UnifiedSettingsDataRepository(
            storage,
            new JsonSerializer(),
            new DataRepositoryOptions
            {
                AutoBackup = true,
                EnableEvents = true
            });
        repository.RegisterDataType(location1, typeof(TestSimpleData));
        repository.RegisterDataType(location2, typeof(TestSimpleData));

        var context = CreateEventContext();
        ((IContextAware)repository).SetContext(context);

        var savedEventCount = 0;
        var batchEventCount = 0;

        context.RegisterEvent<DataSavedEvent<TestSimpleData>>(_ => savedEventCount++);
        context.RegisterEvent<DataBatchSavedEvent>(_ => batchEventCount++);

        await repository.SaveAllAsync(
        [
            (location1, new TestSimpleData { Value = 2 }),
            (location2, new TestSimpleData { Value = 3 })
        ]);

        var current = await repository.LoadAsync<TestSimpleData>(location1);
        var backupJson = await File.ReadAllTextAsync(Path.Combine(root, "settings.json.backup.json"));

        Assert.Multiple(() =>
        {
            Assert.That(current.Value, Is.EqualTo(2));
            Assert.That(savedEventCount, Is.Zero);
            Assert.That(batchEventCount, Is.EqualTo(1));
            Assert.That(backupJson, Does.Contain("settings/graphics"));
            Assert.That(backupJson, Does.Contain("\\\"Value\\\":1"));
        });
    }

    /// <summary>
    ///     验证统一设置仓库在删除某个 section 时会回写聚合文件，并保留删除前的统一文件备份。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task UnifiedSettingsDataRepository_DeleteAsync_Should_Persist_Deletion_And_Create_Backup()
    {
        var root = CreateTempRoot();
        var location1 = new TestDataLocation("settings/graphics");
        var location2 = new TestDataLocation("settings/audio");

        using (var storage = new FileStorage(root, new JsonSerializer(), ".json"))
        {
            var repository = new UnifiedSettingsDataRepository(
                storage,
                new JsonSerializer(),
                new DataRepositoryOptions
                {
                    AutoBackup = true,
                    EnableEvents = false
                });
            repository.RegisterDataType(location1, typeof(TestSimpleData));
            repository.RegisterDataType(location2, typeof(TestSimpleData));

            await repository.SaveAllAsync(
            [
                (location1, new TestSimpleData { Value = 7 }),
                (location2, new TestSimpleData { Value = 11 })
            ]);
        }

        using var verifyStorage = new FileStorage(root, new JsonSerializer(), ".json");
        var verifyRepository = new UnifiedSettingsDataRepository(
            verifyStorage,
            new JsonSerializer(),
            new DataRepositoryOptions
            {
                AutoBackup = true,
                EnableEvents = false
            });
        verifyRepository.RegisterDataType(location1, typeof(TestSimpleData));
        verifyRepository.RegisterDataType(location2, typeof(TestSimpleData));

        await verifyRepository.DeleteAsync(location2);

        var remaining = await verifyRepository.LoadAsync<TestSimpleData>(location1);
        var removedExists = await verifyRepository.ExistsAsync(location2);
        var backupJson = await File.ReadAllTextAsync(Path.Combine(root, "settings.json.backup.json"));

        Assert.Multiple(() =>
        {
            Assert.That(remaining.Value, Is.EqualTo(7));
            Assert.That(removedExists, Is.False);
            Assert.That(backupJson, Does.Contain("settings/audio"));
            Assert.That(backupJson, Does.Contain("\\\"Value\\\":11"));
        });
    }

    /// <summary>
    ///     验证统一设置仓库在保存提交失败时不会污染内存缓存，并且失败修改不会泄漏到后续无关保存。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task UnifiedSettingsDataRepository_SaveAsync_When_Persist_Fails_Should_Keep_Cache_Consistent()
    {
        var root = CreateTempRoot();
        var primaryLocation = new TestDataLocation("settings/graphics");
        var secondaryLocation = new TestDataLocation("settings/audio");

        using (var seedStorage = new FileStorage(root, new JsonSerializer(), ".json"))
        {
            var seedRepository = new UnifiedSettingsDataRepository(
                seedStorage,
                new JsonSerializer(),
                new DataRepositoryOptions { EnableEvents = false });
            seedRepository.RegisterDataType(primaryLocation, typeof(TestSimpleData));
            seedRepository.RegisterDataType(secondaryLocation, typeof(TestSimpleData));
            await seedRepository.SaveAsync(primaryLocation, new TestSimpleData { Value = 1 });
        }

        using var innerStorage = new FileStorage(root, new JsonSerializer(), ".json");
        var throwingStorage = new ToggleWriteFailureStorage(innerStorage, "settings.json");
        var repository = new UnifiedSettingsDataRepository(
            throwingStorage,
            new JsonSerializer(),
            new DataRepositoryOptions { EnableEvents = false });
        repository.RegisterDataType(primaryLocation, typeof(TestSimpleData));
        repository.RegisterDataType(secondaryLocation, typeof(TestSimpleData));

        throwingStorage.ThrowOnWrite = true;
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await repository.SaveAsync(primaryLocation, new TestSimpleData { Value = 99 }).ConfigureAwait(false));

        var cachedAfterFailure = await repository.LoadAsync<TestSimpleData>(primaryLocation).ConfigureAwait(false);
        Assert.That(cachedAfterFailure.Value, Is.EqualTo(1));

        throwingStorage.ThrowOnWrite = false;
        await repository.SaveAsync(secondaryLocation, new TestSimpleData { Value = 7 }).ConfigureAwait(false);

        using var verifyStorage = new FileStorage(root, new JsonSerializer(), ".json");
        var verifyRepository = new UnifiedSettingsDataRepository(
            verifyStorage,
            new JsonSerializer(),
            new DataRepositoryOptions { EnableEvents = false });
        verifyRepository.RegisterDataType(primaryLocation, typeof(TestSimpleData));
        verifyRepository.RegisterDataType(secondaryLocation, typeof(TestSimpleData));

        var persistedPrimary = await verifyRepository.LoadAsync<TestSimpleData>(primaryLocation).ConfigureAwait(false);
        var persistedSecondary = await verifyRepository.LoadAsync<TestSimpleData>(secondaryLocation).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(persistedPrimary.Value, Is.EqualTo(1));
            Assert.That(persistedSecondary.Value, Is.EqualTo(7));
        });
    }

    /// <summary>
    ///     验证统一设置仓库在删除提交失败时不会把未提交删除留在缓存里，也不会泄漏到后续保存。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task UnifiedSettingsDataRepository_DeleteAsync_When_Persist_Fails_Should_Keep_Cache_Consistent()
    {
        var root = CreateTempRoot();
        var primaryLocation = new TestDataLocation("settings/graphics");
        var secondaryLocation = new TestDataLocation("settings/audio");

        using (var seedStorage = new FileStorage(root, new JsonSerializer(), ".json"))
        {
            var seedRepository = new UnifiedSettingsDataRepository(
                seedStorage,
                new JsonSerializer(),
                new DataRepositoryOptions { EnableEvents = false });
            seedRepository.RegisterDataType(primaryLocation, typeof(TestSimpleData));
            seedRepository.RegisterDataType(secondaryLocation, typeof(TestSimpleData));
            await seedRepository.SaveAllAsync(
            [
                (primaryLocation, new TestSimpleData { Value = 3 }),
                (secondaryLocation, new TestSimpleData { Value = 5 })
            ]);
        }

        using var innerStorage = new FileStorage(root, new JsonSerializer(), ".json");
        var throwingStorage = new ToggleWriteFailureStorage(innerStorage, "settings.json");
        var repository = new UnifiedSettingsDataRepository(
            throwingStorage,
            new JsonSerializer(),
            new DataRepositoryOptions { EnableEvents = false });
        repository.RegisterDataType(primaryLocation, typeof(TestSimpleData));
        repository.RegisterDataType(secondaryLocation, typeof(TestSimpleData));

        throwingStorage.ThrowOnWrite = true;
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await repository.DeleteAsync(secondaryLocation).ConfigureAwait(false));

        Assert.That(await repository.ExistsAsync(secondaryLocation).ConfigureAwait(false), Is.True);

        throwingStorage.ThrowOnWrite = false;
        await repository.SaveAsync(primaryLocation, new TestSimpleData { Value = 9 }).ConfigureAwait(false);

        using var verifyStorage = new FileStorage(root, new JsonSerializer(), ".json");
        var verifyRepository = new UnifiedSettingsDataRepository(
            verifyStorage,
            new JsonSerializer(),
            new DataRepositoryOptions { EnableEvents = false });
        verifyRepository.RegisterDataType(primaryLocation, typeof(TestSimpleData));
        verifyRepository.RegisterDataType(secondaryLocation, typeof(TestSimpleData));

        var persistedPrimary = await verifyRepository.LoadAsync<TestSimpleData>(primaryLocation).ConfigureAwait(false);
        var persistedSecondary = await verifyRepository.LoadAsync<TestSimpleData>(secondaryLocation).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(persistedPrimary.Value, Is.EqualTo(9));
            Assert.That(persistedSecondary.Value, Is.EqualTo(5));
        });
    }

    /// <summary>
    ///     验证统一设置仓库在启用事件时，只为显式仓库操作发送加载、保存、批量保存和删除事件。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task UnifiedSettingsDataRepository_WithEvents_Should_Emit_Only_Public_Operation_Events()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer(), ".json");
        var repository = new UnifiedSettingsDataRepository(
            storage,
            new JsonSerializer(),
            new DataRepositoryOptions
            {
                AutoBackup = true,
                EnableEvents = true
            });
        var context = CreateEventContext();
        ((IContextAware)repository).SetContext(context);

        var location1 = new TestDataLocation("settings/graphics");
        var location2 = new TestDataLocation("settings/audio");
        repository.RegisterDataType(location1, typeof(TestSimpleData));
        repository.RegisterDataType(location2, typeof(TestSimpleData));

        var loadedEventCount = 0;
        var savedEventCount = 0;
        var batchEventCount = 0;
        var deletedEventCount = 0;

        context.RegisterEvent<DataLoadedEvent<TestSimpleData>>(_ => loadedEventCount++);
        context.RegisterEvent<DataSavedEvent<TestSimpleData>>(_ => savedEventCount++);
        context.RegisterEvent<DataBatchSavedEvent>(_ => batchEventCount++);
        context.RegisterEvent<DataDeletedEvent>(_ => deletedEventCount++);

        _ = await repository.LoadAsync<TestSimpleData>(location1);
        await repository.SaveAsync(location1, new TestSimpleData { Value = 5 });
        await repository.SaveAllAsync(
        [
            (location1, new TestSimpleData { Value = 6 }),
            (location2, new TestSimpleData { Value = 7 })
        ]);
        await repository.DeleteAsync(location2);

        Assert.Multiple(() =>
        {
            Assert.That(loadedEventCount, Is.EqualTo(1));
            Assert.That(savedEventCount, Is.EqualTo(1));
            Assert.That(batchEventCount, Is.EqualTo(1));
            Assert.That(deletedEventCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     创建带事件总线的真实架构上下文，供上下文感知仓库测试使用。
    /// </summary>
    /// <returns>可用于发送和监听事件的架构上下文。</returns>
    private static ArchitectureContext CreateEventContext()
    {
        var container = new MicrosoftDiContainer();
        container.Register<IEventBus>(new EventBus());
        container.Freeze();
        return new ArchitectureContext(container);
    }

    private sealed class TestSaveMigrationV1ToV2 : ISaveMigration<TestVersionedSaveData>
    {
        public int FromVersion => 1;

        public int ToVersion => 2;

        public TestVersionedSaveData Migrate(TestVersionedSaveData oldData)
        {
            return new TestVersionedSaveData
            {
                Name = $"{oldData.Name}-v2",
                Level = oldData.Level,
                Experience = oldData.Level * 100,
                Version = 2,
                LastModified = oldData.LastModified
            };
        }
    }

    private sealed class BlockingSaveMigrationV1ToV2(
        ManualResetEventSlim migrationStarted,
        ManualResetEventSlim continueMigration) : ISaveMigration<TestVersionedSaveData>
    {
        public int FromVersion => 1;

        public int ToVersion => 2;

        public TestVersionedSaveData Migrate(TestVersionedSaveData oldData)
        {
            migrationStarted.Set();

            if (!continueMigration.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new InvalidOperationException("Timed out while waiting to continue the save migration test.");
            }

            return new TestVersionedSaveData
            {
                Name = $"{oldData.Name}-v2",
                Level = oldData.Level,
                Experience = oldData.Level * 100,
                Version = 2,
                LastModified = oldData.LastModified
            };
        }
    }

    private sealed class TestSaveMigrationV2ToV3 : ISaveMigration<TestVersionedSaveData>
    {
        public int FromVersion => 2;

        public int ToVersion => 3;

        public TestVersionedSaveData Migrate(TestVersionedSaveData oldData)
        {
            return new TestVersionedSaveData
            {
                Name = oldData.Name,
                Level = oldData.Level,
                Experience = oldData.Experience,
                Version = 3,
                LastModified = oldData.LastModified
            };
        }
    }

    private sealed class TestDuplicateSaveMigrationV1ToV2 : ISaveMigration<TestVersionedSaveData>
    {
        public int FromVersion => 1;

        public int ToVersion => 2;

        public TestVersionedSaveData Migrate(TestVersionedSaveData oldData)
        {
            return new TestVersionedSaveData
            {
                Name = $"{oldData.Name}-duplicate",
                Level = oldData.Level,
                Experience = oldData.Experience,
                Version = 2,
                LastModified = oldData.LastModified
            };
        }
    }

    private sealed class TestSaveMigrationV1ToV2ReturningV3 : ISaveMigration<TestVersionedSaveData>
    {
        public int FromVersion => 1;

        public int ToVersion => 2;

        public TestVersionedSaveData Migrate(TestVersionedSaveData oldData)
        {
            return new TestVersionedSaveData
            {
                Name = oldData.Name,
                Level = oldData.Level,
                Experience = oldData.Experience,
                Version = 3,
                LastModified = oldData.LastModified
            };
        }
    }

    private sealed class TestNonVersionedMigration : ISaveMigration<TestSaveData>
    {
        public int FromVersion => 1;

        public int ToVersion => 2;

        public TestSaveData Migrate(TestSaveData oldData)
        {
            return new TestSaveData
            {
                Name = oldData.Name
            };
        }
    }

    /// <summary>
    ///     为统一设置仓库失败场景测试提供可切换的写入失败包装器。
    /// </summary>
    private sealed class ToggleWriteFailureStorage(IStorage innerStorage, string failingKey) : IStorage
    {
        /// <summary>
        ///     获取或设置是否在目标键写入时主动抛出异常。
        /// </summary>
        public bool ThrowOnWrite { get; set; }

        /// <inheritdoc />
        public bool Exists(string key)
        {
            return innerStorage.Exists(key);
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string key)
        {
            return innerStorage.ExistsAsync(key);
        }

        /// <inheritdoc />
        public T Read<T>(string key)
        {
            return innerStorage.Read<T>(key);
        }

        /// <inheritdoc />
        public T Read<T>(string key, T defaultValue)
        {
            return innerStorage.Read(key, defaultValue);
        }

        /// <inheritdoc />
        public Task<T> ReadAsync<T>(string key)
        {
            return innerStorage.ReadAsync<T>(key);
        }

        /// <inheritdoc />
        public void Write<T>(string key, T value)
        {
            ThrowIfNeeded(key);
            innerStorage.Write(key, value);
        }

        /// <inheritdoc />
        public Task WriteAsync<T>(string key, T value)
        {
            ThrowIfNeeded(key);
            return innerStorage.WriteAsync(key, value);
        }

        /// <inheritdoc />
        public void Delete(string key)
        {
            innerStorage.Delete(key);
        }

        /// <inheritdoc />
        public Task DeleteAsync(string key)
        {
            return innerStorage.DeleteAsync(key);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<string>> ListDirectoriesAsync(string path = "")
        {
            return innerStorage.ListDirectoriesAsync(path);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<string>> ListFilesAsync(string path = "")
        {
            return innerStorage.ListFilesAsync(path);
        }

        /// <inheritdoc />
        public Task<bool> DirectoryExistsAsync(string path)
        {
            return innerStorage.DirectoryExistsAsync(path);
        }

        /// <inheritdoc />
        public Task CreateDirectoryAsync(string path)
        {
            return innerStorage.CreateDirectoryAsync(path);
        }

        /// <summary>
        ///     在启用失败开关且命中目标键时抛出一致的写入失败异常。
        /// </summary>
        /// <param name="key">当前正在写入的存储键。</param>
        private void ThrowIfNeeded(string key)
        {
            if (ThrowOnWrite && string.Equals(key, failingKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Simulated unified settings write failure.");
            }
        }
    }
}
