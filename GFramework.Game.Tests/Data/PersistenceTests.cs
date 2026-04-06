using System.IO;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

namespace GFramework.Game.Tests.Data;

[TestFixture]
public class PersistenceTests
{
    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "gframework-persistence", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    [Test]
    public async Task FileStorage_PersistsDataAndRejectsIllegalKeys()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer(), ".json");

        var saved = new TestSimpleData { Value = 5 };
        await storage.WriteAsync("folder/item", saved);

        var loaded = await storage.ReadAsync<TestSimpleData>("folder/item");
        Assert.That(loaded.Value, Is.EqualTo(saved.Value));

        Assert.ThrowsAsync<ArgumentException>(async () => await storage.WriteAsync("../escape", new TestSimpleData()));
    }

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
        var persisted = await storage.ReadAsync<TestVersionedSaveData>("saves/slot_1/save");

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

    [Test]
    public void SaveRepository_RegisterMigration_For_NonVersioned_Save_Should_Throw()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer());
        var config = new SaveConfiguration();
        var repository = new SaveRepository<TestSaveData>(storage, config);

        Assert.Throws<InvalidOperationException>(() => repository.RegisterMigration(new TestNonVersionedMigration()));
    }

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

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await repository.LoadAsync(1));
        Assert.That(exception!.Message, Does.Contain("from version 2"));
    }

    [Test]
    public async Task UnifiedSettingsDataRepository_RoundTripsDataAndLoadAll()
    {
        var root = CreateTempRoot();
        using var storage = new FileStorage(root, new JsonSerializer());
        var serializer = new JsonSerializer();
        var repo = new UnifiedSettingsDataRepository(
            storage,
            serializer,
            new DataRepositoryOptions { EnableEvents = false },
            "settings.json");

        var location = new TestDataLocation("settings/choice");
        repo.RegisterDataType(location, typeof(TestSimpleData));

        var data = new TestSimpleData { Value = 42 };
        await repo.SaveAsync(location, data);

        using var storage2 = new FileStorage(root, new JsonSerializer());
        var repo2 = new UnifiedSettingsDataRepository(
            storage2,
            serializer,
            new DataRepositoryOptions { EnableEvents = false },
            "settings.json");
        repo2.RegisterDataType(location, typeof(TestSimpleData));

        var loaded = await repo2.LoadAsync<TestSimpleData>(location);
        Assert.That(loaded.Value, Is.EqualTo(data.Value));

        var all = await repo2.LoadAllAsync();
        Assert.That(all.Keys, Contains.Item(location.Key));
        Assert.That(all[location.Key], Is.TypeOf<TestSimpleData>());
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
}
