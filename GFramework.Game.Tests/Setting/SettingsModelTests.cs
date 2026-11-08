// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Threading;
using GFramework.Core.Abstractions.Architectures;
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

        _ = model.GetData<TestSettingsData>();

        repository.Stored["TestSettingsData"] = new TestSettingsData
        {
            Version = 1,
            Value = "legacy",
        };

        await model.InitializeAsync();
        Assert.That(model.GetData<TestSettingsData>().Version, Is.EqualTo(1));

        model.GetData<TestSettingsData>().Version = 2;
        model.RegisterMigration(new TestSettingsMigration());

        repository.Stored["TestSettingsData"] = new TestSettingsData
        {
            Version = 1,
            Value = "legacy",
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

        _ = model.GetData<TestLatestSettingsData>();

        repository.Stored["TestLatestSettingsData"] = new TestLatestSettingsData
        {
            Version = 1,
            Value = "legacy",
        };

        model.RegisterMigration(new TestLatestSettingsMigrationV1ToV2());

        await Assert.CatchAsync<AggregateException>(() => model.InitializeAsync());

        var current = model.GetData<TestLatestSettingsData>();
        Assert.Multiple(() =>
        {
            Assert.That(model.IsInitialized, Is.False);
            Assert.That(current.Version, Is.EqualTo(3));
            Assert.That(current.Value, Is.EqualTo("default-v3"));
        });
    }

    [Test]
    public async Task RegisterMigration_During_Cache_Rebuild_Should_Not_Leave_Stale_Type_Cache()
    {
        var locationProvider = new TestDataLocationProvider();
        var repository = new FakeSettingsDataRepository();
        var model = new SettingsModel<FakeSettingsDataRepository>(locationProvider, repository);

        _ = model.GetData<TestLatestSettingsData>();

        model.RegisterMigration(new TestLatestSettingsMigrationV1ToV2());

        repository.Stored["TestLatestSettingsData"] = new TestLatestSettingsData
        {
            Version = 1,
            Value = "legacy",
        };

        var tasks = StartConcurrentMigrationWork(model);

        var (initializeTask, registerTask) = tasks;

        // The first load may observe the older migration snapshot; that failure must be observable.
        try
        {
            await Task.WhenAll(initializeTask, registerTask);
        }
        catch (AggregateException)
        {
            Assert.That(model.IsInitialized, Is.False);
        }
        await registerTask;

        repository.Stored["TestLatestSettingsData"] = new TestLatestSettingsData
        {
            Version = 1,
            Value = "legacy",
        };

        await model.InitializeAsync();

        var current = model.GetData<TestLatestSettingsData>();
        Assert.Multiple(() =>
        {
            Assert.That(current.Version, Is.EqualTo(3));
            Assert.That(current.Value, Is.EqualTo("legacy-migrated-v3"));
        });
    }

    /// <summary>A failed independent migration cannot commit an otherwise valid section early.</summary>
    [Test]
    public async Task InitializeAsync_WhenAnySectionFails_RetainsAllCurrentData()
    {
        var repository = new FakeSettingsDataRepository();
        var notifications = new List<object>();
        var model = new SettingsModel<FakeSettingsDataRepository>(new TestDataLocationProvider(), repository,
            publishEvent: notifications.Add);
        var simple = model.GetData<TestSettingsData>();
        var latest = model.GetData<TestLatestSettingsData>();
        repository.Stored["TestSettingsData"] = new TestSettingsData { Value = "persisted", Version = 1 };
        repository.Stored["TestLatestSettingsData"] = new TestLatestSettingsData { Value = "legacy", Version = 1 };

        await Assert.CatchAsync<AggregateException>(() => model.InitializeAsync());

        Assert.Multiple(() =>
        {
            Assert.That(simple.Value, Is.EqualTo("default"));
            Assert.That(latest.Value, Is.EqualTo("default-v3"));
            Assert.That(model.IsInitialized, Is.False);
            Assert.That(notifications, Is.Empty);
        });
    }

    /// <summary>A failed save is observable and never publishes an all-saved success notification.</summary>
    [Test]
    public async Task SaveAllAsync_WhenRepositoryFails_PropagatesErrorWithoutSuccessNotification()
    {
        var repository = new Mock<ISettingsDataRepository>();
        repository.Setup(value => value.SaveAllAsync(
            It.IsAny<IEnumerable<(IDataLocation location, IData data)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new InvalidOperationException("write failed")));
        var notifications = new List<object>();
        var model = new SettingsModel<ISettingsDataRepository>(new TestDataLocationProvider(), repository.Object,
            publishEvent: notifications.Add);
        _ = model.GetData<TestSettingsData>();

        await Assert.CatchAsync<InvalidOperationException>(() => model.SaveAllAsync());

        Assert.That(notifications, Is.Empty);
    }

    /// <summary>Starts cache rebuild and registration while the shared migration lock is held.</summary>
    private static (Task InitializeTask, Task RegisterTask) StartConcurrentMigrationWork(
        SettingsModel<FakeSettingsDataRepository> model)
    {
        var lockField = typeof(SettingsModel<FakeSettingsDataRepository>)
            .GetField("_migrationMapLock", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(lockField, Is.Not.Null);

        var migrationMapLock = lockField!.GetValue(model);
        Assert.That(migrationMapLock, Is.Not.Null);

        return WithSynchronizationLockHeld(migrationMapLock!, () =>
        {
            var initializeTask = Task.Run(() => model.InitializeAsync());
            var registerTask = Task.Run(() => model.RegisterMigration(new TestLatestSettingsMigrationV2ToV3()));

            Thread.Sleep(50);

            Assert.Multiple(() =>
            {
                Assert.That(initializeTask.IsCompleted, Is.False);
                Assert.That(registerTask.IsCompleted, Is.False);
            });

            return (initializeTask, registerTask);
        });

    }

    /// <summary>
    ///     以与被测代码相同的同步原语持有反射获取到的锁对象，避免在 .NET 9+ 上把 <see cref="System.Threading.Lock" />
    ///     退化成 <see cref="Monitor" /> 语义，导致并发测试误判。
    /// </summary>
    /// <param name="syncRoot">通过反射读取到的私有锁字段。</param>
    /// <typeparam name="TResult">持锁代码返回的结果类型。</typeparam>
    /// <param name="action">持锁期间执行的断言与并发调度逻辑。</param>
    /// <returns>持锁代码的返回值。</returns>
    private static TResult WithSynchronizationLockHeld<TResult>(object syncRoot, Func<TResult> action)
    {
        ArgumentNullException.ThrowIfNull(syncRoot);
        ArgumentNullException.ThrowIfNull(action);

#if NET9_0_OR_GREATER
        if (syncRoot is System.Threading.Lock typedLock)
        {
            using (typedLock.EnterScope())
            {
                return action();
            }
        }
#endif

        lock (syncRoot)
        {
            return action();
        }
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
                Value = $"{data.Value}-migrated",
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
                Value = $"{data.Value}-migrated",
            };
        }
    }

    private sealed class TestLatestSettingsMigrationV2ToV3 : ISettingsMigration
    {
        public Type SettingsType => typeof(TestLatestSettingsData);

        public int FromVersion => 2;

        public int ToVersion => 3;

        public ISettingsSection Migrate(ISettingsSection oldData)
        {
            var data = (TestLatestSettingsData)oldData;
            return new TestLatestSettingsData
            {
                Version = 3,
                Value = $"{data.Value}-v3",
            };
        }
    }

    private sealed class FakeSettingsDataRepository : ISettingsDataRepository
    {
        public Dictionary<string, Type> RegisteredTypes { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, IData> Stored { get; } = new(StringComparer.Ordinal);

        public Task<T> LoadAsync<T>(IDataLocation location, CancellationToken cancellationToken = default) where T : class, IData, new()
        {
            return Task.FromResult(Stored.TryGetValue(location.Key, out var data) ? (T)data : new T());
        }

        public Task SaveAsync<T>(IDataLocation location, T data, CancellationToken cancellationToken = default) where T : class, IData
        {
            Stored[location.Key] = data;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(IDataLocation location, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stored.ContainsKey(location.Key));
        }

        public Task DeleteAsync(IDataLocation location, CancellationToken cancellationToken = default)
        {
            Stored.Remove(location.Key);
            return Task.CompletedTask;
        }

        public Task SaveAllAsync(IEnumerable<(IDataLocation location, IData data)> dataList, CancellationToken cancellationToken = default)
        {
            foreach (var (location, data) in dataList)
            {
                Stored[location.Key] = data;
            }

            return Task.CompletedTask;
        }

        public Task<IDictionary<string, IData>> LoadAllAsync(CancellationToken cancellationToken = default)
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
