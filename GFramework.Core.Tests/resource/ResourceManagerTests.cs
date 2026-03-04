using System.IO;
using GFramework.Core.Abstractions.resource;
using GFramework.Core.resource;
using NUnit.Framework;

namespace GFramework.Core.Tests.resource;

/// <summary>
///     测试用的简单资源类
/// </summary>
public class TestResource
{
    public string Content { get; set; } = string.Empty;
    public bool IsDisposed { get; set; }
}

/// <summary>
///     测试用的资源加载器
/// </summary>
public class TestResourceLoader : IResourceLoader<TestResource>
{
    private readonly Dictionary<string, string> _resourceData = new();

    public TestResource Load(string path)
    {
        if (_resourceData.TryGetValue(path, out var content))
        {
            return new TestResource { Content = content };
        }

        throw new FileNotFoundException($"Resource not found: {path}");
    }

    public async Task<TestResource> LoadAsync(string path)
    {
        await Task.Delay(10); // 模拟异步加载
        return Load(path);
    }

    public void Unload(TestResource resource)
    {
        resource.IsDisposed = true;
    }

    public bool CanLoad(string path)
    {
        return _resourceData.ContainsKey(path);
    }

    public void AddTestData(string path, string content)
    {
        _resourceData[path] = content;
    }
}

/// <summary>
///     ResourceManager 功能测试类
/// </summary>
[TestFixture]
public class ResourceManagerTests
{
    [SetUp]
    public void SetUp()
    {
        _resourceManager = new ResourceManager();
        _testLoader = new TestResourceLoader();
        _testLoader.AddTestData("test/resource1.txt", "Content 1");
        _testLoader.AddTestData("test/resource2.txt", "Content 2");
        _resourceManager.RegisterLoader(_testLoader);
    }

    [TearDown]
    public void TearDown()
    {
        _resourceManager.UnloadAll();
    }

    private ResourceManager _resourceManager = null!;
    private TestResourceLoader _testLoader = null!;

    [Test]
    public void Load_Should_Load_Resource()
    {
        var resource = _resourceManager.Load<TestResource>("test/resource1.txt");

        Assert.That(resource, Is.Not.Null);
        Assert.That(resource!.Content, Is.EqualTo("Content 1"));
    }

    [Test]
    public void Load_Should_Return_Cached_Resource()
    {
        var resource1 = _resourceManager.Load<TestResource>("test/resource1.txt");
        var resource2 = _resourceManager.Load<TestResource>("test/resource1.txt");

        Assert.That(resource1, Is.SameAs(resource2));
    }

    [Test]
    public async Task LoadAsync_Should_Load_Resource()
    {
        var resource = await _resourceManager.LoadAsync<TestResource>("test/resource1.txt");

        Assert.That(resource, Is.Not.Null);
        Assert.That(resource!.Content, Is.EqualTo("Content 1"));
    }

    [Test]
    public void Load_Should_Throw_When_No_Loader_Registered()
    {
        _resourceManager.UnregisterLoader<TestResource>();

        Assert.Throws<InvalidOperationException>(() => { _resourceManager.Load<TestResource>("test/resource1.txt"); });
    }

    [Test]
    public void IsLoaded_Should_Return_True_When_Resource_Loaded()
    {
        _resourceManager.Load<TestResource>("test/resource1.txt");

        Assert.That(_resourceManager.IsLoaded("test/resource1.txt"), Is.True);
    }

    [Test]
    public void IsLoaded_Should_Return_False_When_Resource_Not_Loaded()
    {
        Assert.That(_resourceManager.IsLoaded("test/nonexistent.txt"), Is.False);
    }

    [Test]
    public void Unload_Should_Remove_Resource()
    {
        _resourceManager.Load<TestResource>("test/resource1.txt");

        var unloaded = _resourceManager.Unload("test/resource1.txt");

        Assert.That(unloaded, Is.True);
        Assert.That(_resourceManager.IsLoaded("test/resource1.txt"), Is.False);
    }

    [Test]
    public void UnloadAll_Should_Remove_All_Resources()
    {
        _resourceManager.Load<TestResource>("test/resource1.txt");
        _resourceManager.Load<TestResource>("test/resource2.txt");

        _resourceManager.UnloadAll();

        Assert.That(_resourceManager.LoadedResourceCount, Is.EqualTo(0));
    }

    [Test]
    public void LoadedResourceCount_Should_Return_Correct_Count()
    {
        _resourceManager.Load<TestResource>("test/resource1.txt");
        _resourceManager.Load<TestResource>("test/resource2.txt");

        Assert.That(_resourceManager.LoadedResourceCount, Is.EqualTo(2));
    }

    [Test]
    public void GetLoadedResourcePaths_Should_Return_All_Paths()
    {
        _resourceManager.Load<TestResource>("test/resource1.txt");
        _resourceManager.Load<TestResource>("test/resource2.txt");

        var paths = _resourceManager.GetLoadedResourcePaths().ToList();

        Assert.That(paths, Has.Count.EqualTo(2));
        Assert.That(paths, Contains.Item("test/resource1.txt"));
        Assert.That(paths, Contains.Item("test/resource2.txt"));
    }

    [Test]
    public void GetHandle_Should_Return_Valid_Handle()
    {
        _resourceManager.Load<TestResource>("test/resource1.txt");

        var handle = _resourceManager.GetHandle<TestResource>("test/resource1.txt");

        Assert.That(handle, Is.Not.Null);
        Assert.That(handle!.IsValid, Is.True);
        Assert.That(handle.Resource, Is.Not.Null);
        Assert.That(handle.Path, Is.EqualTo("test/resource1.txt"));
    }

    [Test]
    public void GetHandle_Should_Return_Null_When_Resource_Not_Loaded()
    {
        var handle = _resourceManager.GetHandle<TestResource>("test/nonexistent.txt");

        Assert.That(handle, Is.Null);
    }

    [Test]
    public void Handle_Dispose_Should_Decrease_Reference_Count()
    {
        _resourceManager.Load<TestResource>("test/resource1.txt");

        var handle = _resourceManager.GetHandle<TestResource>("test/resource1.txt");
        handle!.Dispose();

        // 资源应该仍然存在，因为还有一个初始引用
        Assert.That(_resourceManager.IsLoaded("test/resource1.txt"), Is.True);
    }

    [Test]
    public async Task PreloadAsync_Should_Load_Resource()
    {
        await _resourceManager.PreloadAsync<TestResource>("test/resource1.txt");

        Assert.That(_resourceManager.IsLoaded("test/resource1.txt"), Is.True);
    }

    [Test]
    public void Load_Should_Throw_When_Path_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => _resourceManager.Load<TestResource>(null!));
        Assert.Throws<ArgumentException>(() => _resourceManager.Load<TestResource>(""));
        Assert.Throws<ArgumentException>(() => _resourceManager.Load<TestResource>("   "));
    }

    [Test]
    public void RegisterLoader_Should_Throw_When_Loader_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => { _resourceManager.RegisterLoader<TestResource>(null!); });
    }

    [Test]
    public void Concurrent_Load_Should_Be_Thread_Safe()
    {
        const int threadCount = 10;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();
        var resources = new TestResource?[threadCount];

        for (var i = 0; i < threadCount; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    resources[index] = _resourceManager.Load<TestResource>("test/resource1.txt");
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
        Assert.That(resources.All(r => r != null), Is.True);
        Assert.That(resources.All(r => r == resources[0]), Is.True, "所有线程应该获得同一个资源实例");
    }

    [Test]
    public async Task Concurrent_LoadAsync_Should_Be_Thread_Safe()
    {
        const int threadCount = 10;
        var tasks = new Task<TestResource?>[threadCount];

        for (var i = 0; i < threadCount; i++)
        {
            tasks[i] = _resourceManager.LoadAsync<TestResource>("test/resource1.txt");
        }

        var resources = await Task.WhenAll(tasks);

        Assert.That(resources.All(r => r != null), Is.True);
        Assert.That(resources.All(r => r == resources[0]), Is.True, "所有任务应该获得同一个资源实例");
    }

    [Test]
    public void Concurrent_Load_Different_Resources_Should_Work()
    {
        const int threadCount = 2;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();

        tasks[0] = Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < 100; i++)
                {
                    _resourceManager.Load<TestResource>("test/resource1.txt");
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

        tasks[1] = Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < 100; i++)
                {
                    _resourceManager.Load<TestResource>("test/resource2.txt");
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

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty);
        Assert.That(_resourceManager.LoadedResourceCount, Is.EqualTo(2));
    }

    [Test]
    public void ManualReleaseStrategy_Should_Not_Auto_Unload()
    {
        _resourceManager.SetReleaseStrategy(new ManualReleaseStrategy());
        _resourceManager.Load<TestResource>("test/resource1.txt");

        using (var handle = _resourceManager.GetHandle<TestResource>("test/resource1.txt"))
        {
            handle?.Dispose();
        }

        // 验证资源仍然在缓存中
        Assert.That(_resourceManager.LoadedResourceCount, Is.EqualTo(1));
        Assert.That(_resourceManager.IsLoaded("test/resource1.txt"), Is.True);

        // 手动获取资源验证其未被卸载
        var cachedResource = _resourceManager.GetHandle<TestResource>("test/resource1.txt");
        Assert.That(cachedResource, Is.Not.Null);
        Assert.That(cachedResource!.Resource, Is.Not.Null);
    }

    [Test]
    public void AutoReleaseStrategy_Should_Auto_Unload_When_RefCount_Zero()
    {
        _resourceManager.SetReleaseStrategy(new AutoReleaseStrategy());
        _resourceManager.Load<TestResource>("test/resource1.txt");
        var handle = _resourceManager.GetHandle<TestResource>("test/resource1.txt");

        // 释放句柄
        handle!.Dispose();

        // 自动释放策略：资源应该被自动卸载
        Assert.That(_resourceManager.IsLoaded("test/resource1.txt"), Is.False);
    }

    [Test]
    public void AutoReleaseStrategy_Should_Not_Unload_With_Active_References()
    {
        _resourceManager.SetReleaseStrategy(new AutoReleaseStrategy());
        _resourceManager.Load<TestResource>("test/resource1.txt");
        var handle1 = _resourceManager.GetHandle<TestResource>("test/resource1.txt");
        var handle2 = _resourceManager.GetHandle<TestResource>("test/resource1.txt");

        // 释放一个句柄
        handle1!.Dispose();

        // 还有一个活跃引用，资源不应该被卸载
        Assert.That(_resourceManager.IsLoaded("test/resource1.txt"), Is.True);

        // 释放第二个句柄
        handle2!.Dispose();

        // 现在资源应该被自动卸载
        Assert.That(_resourceManager.IsLoaded("test/resource1.txt"), Is.False);
    }

    [Test]
    public void SetReleaseStrategy_Should_Throw_When_Strategy_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => { _resourceManager.SetReleaseStrategy(null!); });
    }

    [Test]
    public void Can_Switch_Between_Release_Strategies()
    {
        // 开始使用手动释放策略
        _resourceManager.Load<TestResource>("test/resource1.txt");
        var handle1 = _resourceManager.GetHandle<TestResource>("test/resource1.txt");
        handle1!.Dispose();
        Assert.That(_resourceManager.IsLoaded("test/resource1.txt"), Is.True);

        // 切换到自动释放策略
        _resourceManager.SetReleaseStrategy(new AutoReleaseStrategy());
        _resourceManager.Load<TestResource>("test/resource2.txt");
        var handle2 = _resourceManager.GetHandle<TestResource>("test/resource2.txt");
        handle2!.Dispose();
        Assert.That(_resourceManager.IsLoaded("test/resource2.txt"), Is.False);
    }
}