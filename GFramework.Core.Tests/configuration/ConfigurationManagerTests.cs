using System.IO;
using GFramework.Core.configuration;
using NUnit.Framework;

namespace GFramework.Core.Tests.configuration;

/// <summary>
///     ConfigurationManager 功能测试类
/// </summary>
[TestFixture]
public class ConfigurationManagerTests
{
    [SetUp]
    public void SetUp()
    {
        _configManager = new ConfigurationManager();
    }

    [TearDown]
    public void TearDown()
    {
        _configManager.Clear();
    }

    private ConfigurationManager _configManager = null!;

    [Test]
    public void SetConfig_And_GetConfig_Should_Work()
    {
        _configManager.SetConfig("test.key", 42);

        var value = _configManager.GetConfig<int>("test.key");

        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void GetConfig_With_DefaultValue_Should_Return_DefaultValue_When_Key_Not_Exists()
    {
        var value = _configManager.GetConfig("nonexistent.key", 100);

        Assert.That(value, Is.EqualTo(100));
    }

    [Test]
    public void GetConfig_Should_Return_Default_When_Key_Not_Exists()
    {
        var value = _configManager.GetConfig<int>("nonexistent.key");

        Assert.That(value, Is.EqualTo(0));
    }

    [Test]
    public void HasConfig_Should_Return_True_When_Key_Exists()
    {
        _configManager.SetConfig("test.key", "value");

        Assert.That(_configManager.HasConfig("test.key"), Is.True);
    }

    [Test]
    public void HasConfig_Should_Return_False_When_Key_Not_Exists()
    {
        Assert.That(_configManager.HasConfig("nonexistent.key"), Is.False);
    }

    [Test]
    public void RemoveConfig_Should_Remove_Existing_Key()
    {
        _configManager.SetConfig("test.key", "value");

        var removed = _configManager.RemoveConfig("test.key");

        Assert.That(removed, Is.True);
        Assert.That(_configManager.HasConfig("test.key"), Is.False);
    }

    [Test]
    public void RemoveConfig_Should_Return_False_When_Key_Not_Exists()
    {
        var removed = _configManager.RemoveConfig("nonexistent.key");

        Assert.That(removed, Is.False);
    }

    [Test]
    public void Clear_Should_Remove_All_Configs()
    {
        _configManager.SetConfig("key1", "value1");
        _configManager.SetConfig("key2", "value2");

        _configManager.Clear();

        Assert.That(_configManager.Count, Is.EqualTo(0));
    }

    [Test]
    public void Count_Should_Return_Correct_Number()
    {
        _configManager.SetConfig("key1", "value1");
        _configManager.SetConfig("key2", "value2");
        _configManager.SetConfig("key3", "value3");

        Assert.That(_configManager.Count, Is.EqualTo(3));
    }

    [Test]
    public void GetAllKeys_Should_Return_All_Keys()
    {
        _configManager.SetConfig("key1", "value1");
        _configManager.SetConfig("key2", "value2");

        var keys = _configManager.GetAllKeys().ToList();

        Assert.That(keys, Has.Count.EqualTo(2));
        Assert.That(keys, Contains.Item("key1"));
        Assert.That(keys, Contains.Item("key2"));
    }

    [Test]
    public void GetConfig_Should_Support_Different_Types()
    {
        _configManager.SetConfig("int.key", 42);
        _configManager.SetConfig("string.key", "hello");
        _configManager.SetConfig("bool.key", true);
        _configManager.SetConfig("double.key", 3.14);

        Assert.That(_configManager.GetConfig<int>("int.key"), Is.EqualTo(42));
        Assert.That(_configManager.GetConfig<string>("string.key"), Is.EqualTo("hello"));
        Assert.That(_configManager.GetConfig<bool>("bool.key"), Is.True);
        Assert.That(_configManager.GetConfig<double>("double.key"), Is.EqualTo(3.14).Within(0.001));
    }

    [Test]
    public void WatchConfig_Should_Trigger_When_Config_Changes()
    {
        var callCount = 0;
        var receivedValue = 0;

        _configManager.WatchConfig<int>("test.key", value =>
        {
            callCount++;
            receivedValue = value;
        });

        _configManager.SetConfig("test.key", 42);

        Assert.That(callCount, Is.EqualTo(1));
        Assert.That(receivedValue, Is.EqualTo(42));
    }

    [Test]
    public void WatchConfig_Should_Not_Trigger_When_Value_Not_Changed()
    {
        _configManager.SetConfig("test.key", 42);

        var callCount = 0;
        _configManager.WatchConfig<int>("test.key", _ => callCount++);

        _configManager.SetConfig("test.key", 42);

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void UnRegister_Should_Stop_Watching()
    {
        var callCount = 0;

        var unRegister = _configManager.WatchConfig<int>("test.key", _ => callCount++);

        _configManager.SetConfig("test.key", 42);
        Assert.That(callCount, Is.EqualTo(1));

        unRegister.UnRegister();

        _configManager.SetConfig("test.key", 100);
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Multiple_Watchers_Should_All_Be_Triggered()
    {
        var count1 = 0;
        var count2 = 0;

        _configManager.WatchConfig<int>("test.key", _ => count1++);
        _configManager.WatchConfig<int>("test.key", _ => count2++);

        _configManager.SetConfig("test.key", 42);

        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }

    [Test]
    public void SaveToJson_And_LoadFromJson_Should_Work()
    {
        _configManager.SetConfig("key1", "value1");
        _configManager.SetConfig("key2", 42);

        var json = _configManager.SaveToJson();

        var newManager = new ConfigurationManager();
        newManager.LoadFromJson(json);

        Assert.That(newManager.GetConfig<string>("key1"), Is.EqualTo("value1"));
        Assert.That(newManager.GetConfig<int>("key2"), Is.EqualTo(42));
    }

    [Test]
    public void SaveToFile_And_LoadFromFile_Should_Work()
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            _configManager.SetConfig("key1", "value1");
            _configManager.SetConfig("key2", 42);

            _configManager.SaveToFile(tempFile);

            var newManager = new ConfigurationManager();
            newManager.LoadFromFile(tempFile);

            Assert.That(newManager.GetConfig<string>("key1"), Is.EqualTo("value1"));
            Assert.That(newManager.GetConfig<int>("key2"), Is.EqualTo(42));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Test]
    public void LoadFromFile_Should_Throw_When_File_Not_Exists()
    {
        Assert.Throws<FileNotFoundException>(() => { _configManager.LoadFromFile("nonexistent.json"); });
    }

    [Test]
    public void SetConfig_Should_Throw_When_Key_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => _configManager.SetConfig<string>(null!, "value"));
        Assert.Throws<ArgumentException>(() => _configManager.SetConfig("", "value"));
        Assert.Throws<ArgumentException>(() => _configManager.SetConfig("   ", "value"));
    }

    [Test]
    public void GetConfig_Should_Throw_When_Key_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => _configManager.GetConfig<string>(null!));
        Assert.Throws<ArgumentException>(() => _configManager.GetConfig<string>(""));
        Assert.Throws<ArgumentException>(() => _configManager.GetConfig<string>("   "));
    }

    [Test]
    public void WatchConfig_Should_Throw_When_Parameters_Invalid()
    {
        Assert.Throws<ArgumentException>(() => _configManager.WatchConfig<int>(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => _configManager.WatchConfig<int>("key", null!));
    }

    [Test]
    public void Concurrent_SetConfig_Should_Be_Thread_Safe()
    {
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();

        for (var i = 0; i < threadCount; i++)
        {
            var threadId = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    for (var j = 0; j < iterationsPerThread; j++)
                    {
                        _configManager.SetConfig($"key.{threadId}", j);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty);
    }

    [Test]
    public void Concurrent_GetConfig_And_SetConfig_Should_Be_Thread_Safe()
    {
        const int threadCount = 5;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();

        for (var i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    for (var j = 0; j < 100; j++)
                    {
                        if (j % 2 == 0)
                        {
                            _configManager.SetConfig("shared.key", j);
                        }
                        else
                        {
                            _configManager.GetConfig<int>("shared.key");
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty);
    }

    [Test]
    public void Concurrent_WatchConfig_Should_Be_Thread_Safe()
    {
        const int threadCount = 10;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();
        var callCounts = new int[threadCount];

        for (var i = 0; i < threadCount; i++)
        {
            var threadId = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    _configManager.WatchConfig<int>("test.key",
                        _ => { Interlocked.Increment(ref callCounts[threadId]); });
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        _configManager.SetConfig("test.key", 42);

        Assert.That(exceptions, Is.Empty);
        Assert.That(callCounts.Sum(), Is.EqualTo(threadCount));
    }
}