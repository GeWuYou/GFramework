using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Rule;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Setting;

namespace GFramework.Game.Tests.Setting;

[TestFixture]
public sealed class SettingsModelTests
{
    [Test]
    public void GetData_After_Initialize_Should_Register_New_Type_In_Repository()
    {
        var locationProvider = new TestDataLocationProvider();
        var repository = new FakeSettingsDataRepository();
        var model = new SettingsModel<FakeSettingsDataRepository>(locationProvider, repository);
        ((IContextAware)model).SetContext(new Mock<IArchitectureContext>(MockBehavior.Loose).Object);

        ((IInitializable)model).Initialize();

        _ = model.GetData<TestSettingsData>();

        Assert.That(repository.RegisteredTypes, Contains.Key("TestSettingsData"));
        Assert.That(repository.RegisteredTypes["TestSettingsData"], Is.EqualTo(typeof(TestSettingsData)));
    }

    [Test]
    public async Task RegisterMigration_After_Cache_Warmup_Should_Invalidate_Type_Cache()
    {
        var locationProvider = new TestDataLocationProvider();
        var repository = new FakeSettingsDataRepository();
        var model = new SettingsModel<FakeSettingsDataRepository>(locationProvider, repository);
        ((IContextAware)model).SetContext(new Mock<IArchitectureContext>(MockBehavior.Loose).Object);

        _ = model.GetData<TestSettingsData>();
        ((IInitializable)model).Initialize();

        repository.Stored["TestSettingsData"] = new TestSettingsData
        {
            Version = 1,
            Value = "legacy"
        };

        await model.InitializeAsync();
        Assert.That(model.GetData<TestSettingsData>().Version, Is.EqualTo(1));

        model.GetData<TestSettingsData>().Version = 2;
        model.RegisterMigration(new TestSettingsMigration());

        repository.Stored["TestSettingsData"] = new TestSettingsData
        {
            Version = 1,
            Value = "legacy"
        };

        await model.InitializeAsync();

        var current = model.GetData<TestSettingsData>();
        Assert.Multiple(() =>
        {
            Assert.That(current.Version, Is.EqualTo(2));
            Assert.That(current.Value, Is.EqualTo("legacy-migrated"));
        });
    }

    [Test]
    public void RegisterMigration_Should_Reject_Duplicate_FromVersion_For_Same_SettingsType()
    {
        var locationProvider = new TestDataLocationProvider();
        var repository = new FakeSettingsDataRepository();
        var model = new SettingsModel<FakeSettingsDataRepository>(locationProvider, repository);

        model.RegisterMigration(new TestSettingsMigration());

        var exception = Assert.Throws<InvalidOperationException>(() => model.RegisterMigration(new TestSettingsMigration()));

        Assert.That(exception!.Message, Does.Contain("Duplicate settings migration registration"));
    }

    [Test]
    public async Task InitializeAsync_Should_Keep_Current_Instance_When_Migration_Chain_Is_Incomplete()
    {
        var locationProvider = new TestDataLocationProvider();
        var repository = new FakeSettingsDataRepository();
        var model = new SettingsModel<FakeSettingsDataRepository>(locationProvider, repository);
        ((IContextAware)model).SetContext(new Mock<IArchitectureContext>(MockBehavior.Loose).Object);

        _ = model.GetData<TestLatestSettingsData>();
        ((IInitializable)model).Initialize();

        repository.Stored["TestLatestSettingsData"] = new TestLatestSettingsData
        {
            Version = 1,
            Value = "legacy"
        };

        model.RegisterMigration(new TestLatestSettingsMigrationV1ToV2());

        await model.InitializeAsync();

        var current = model.GetData<TestLatestSettingsData>();
        Assert.Multiple(() =>
        {
            Assert.That(current.Version, Is.EqualTo(3));
            Assert.That(current.Value, Is.EqualTo("default-v3"));
        });
    }

    private sealed class TestSettingsData : ISettingsData
    {
        public string Value { get; set; } = "default";

        public int Version { get; set; } = 1;

        public DateTime LastModified { get; } = DateTime.UtcNow;

        public void Reset()
        {
            Value = "default";
            Version = 1;
        }

        public void LoadFrom(ISettingsData source)
        {
            if (source is not TestSettingsData data)
            {
                return;
            }

            Value = data.Value;
            Version = data.Version;
        }
    }

    private sealed class TestSettingsMigration : ISettingsMigration
    {
        public Type SettingsType => typeof(TestSettingsData);

        public int FromVersion => 1;

        public int ToVersion => 2;

        public ISettingsSection Migrate(ISettingsSection oldData)
        {
            var data = (TestSettingsData)oldData;
            return new TestSettingsData
            {
                Version = 2,
                Value = $"{data.Value}-migrated"
            };
        }
    }

    private sealed class TestLatestSettingsData : ISettingsData
    {
        public string Value { get; set; } = "default-v3";

        public int Version { get; set; } = 3;

        public DateTime LastModified { get; } = DateTime.UtcNow;

        public void Reset()
        {
            Value = "default-v3";
            Version = 3;
        }

        public void LoadFrom(ISettingsData source)
        {
            if (source is not TestLatestSettingsData data)
            {
                return;
            }

            Value = data.Value;
            Version = data.Version;
        }
    }

    private sealed class TestLatestSettingsMigrationV1ToV2 : ISettingsMigration
    {
        public Type SettingsType => typeof(TestLatestSettingsData);

        public int FromVersion => 1;

        public int ToVersion => 2;

        public ISettingsSection Migrate(ISettingsSection oldData)
        {
            var data = (TestLatestSettingsData)oldData;
            return new TestLatestSettingsData
            {
                Version = 2,
                Value = $"{data.Value}-migrated"
            };
        }
    }

    private sealed class FakeSettingsDataRepository : ISettingsDataRepository
    {
        public Dictionary<string, Type> RegisteredTypes { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, IData> Stored { get; } = new(StringComparer.Ordinal);

        public Task<T> LoadAsync<T>(IDataLocation location) where T : class, IData, new()
        {
            return Task.FromResult(Stored.TryGetValue(location.Key, out var data) ? (T)data : new T());
        }

        public Task SaveAsync<T>(IDataLocation location, T data) where T : class, IData
        {
            Stored[location.Key] = data;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(IDataLocation location)
        {
            return Task.FromResult(Stored.ContainsKey(location.Key));
        }

        public Task DeleteAsync(IDataLocation location)
        {
            Stored.Remove(location.Key);
            return Task.CompletedTask;
        }

        public Task SaveAllAsync(IEnumerable<(IDataLocation location, IData data)> dataList)
        {
            foreach (var (location, data) in dataList)
            {
                Stored[location.Key] = data;
            }

            return Task.CompletedTask;
        }

        public Task<IDictionary<string, IData>> LoadAllAsync()
        {
            IDictionary<string, IData> snapshot = new Dictionary<string, IData>(Stored, StringComparer.Ordinal);
            return Task.FromResult(snapshot);
        }

        public void RegisterDataType(IDataLocation location, Type type)
        {
            RegisteredTypes[location.Key] = type;
        }
    }

    private sealed class TestDataLocationProvider : IDataLocationProvider
    {
        public IDataLocation GetLocation(Type type)
        {
            return new TestDataLocation(type.Name);
        }
    }

    private sealed class TestDataLocation(string key) : IDataLocation
    {
        public string Key { get; } = key;

        public StorageKinds Kinds => StorageKinds.Memory;

        public string? Namespace => "tests";

        public IReadOnlyDictionary<string, string>? Metadata => null;
    }
}
