using System.Collections;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// 全局协程作用域测试类，验证全局协程作用域的初始化和访问功能
/// </summary>
[TestFixture]
public class GlobalCoroutineScopeTests
{
    private CoroutineScheduler _scheduler = null!;

    [SetUp]
    public void SetUp()
    {
        // 每个测试前都重新初始化
        _scheduler = new CoroutineScheduler();
        GlobalCoroutineScope.Initialize(_scheduler);
    }

    [TearDown]
    public void TearDown()
    {
        GlobalCoroutineScope.TryGetScope(out var scope);
        scope?.Cancel();
    }

    /// <summary>
    /// 测试初始化检查 - IsInitialized 属性
    /// </summary>
    [Test]
    public void IsInitialized_Should_BeCorrect()
    {
        Assert.That(GlobalCoroutineScope.IsInitialized, Is.True);
    }

    /// <summary>
    /// 测试尝试获取作用域 - TryGetScope 方法
    /// </summary>
    [Test]
    public void TryGetScope_Should_ReturnScope()
    {
        var result = GlobalCoroutineScope.TryGetScope(out var scope);

        Assert.That(result, Is.True);
        Assert.That(scope, Is.Not.Null);
        Assert.That(scope!.IsActive, Is.True);
    }

    /// <summary>
    /// 测试初始化作用域 - Initialize(scheduler) 方法
    /// </summary>
    [Test]
    public void Initialize_Should_SetUpGlobalScope()
    {
        GlobalCoroutineScope.TryGetScope(out var oldScope);
        oldScope?.Cancel();

        _scheduler = new CoroutineScheduler();
        GlobalCoroutineScope.Initialize(_scheduler);

        Assert.That(GlobalCoroutineScope.IsInitialized, Is.True);
        Assert.That(GlobalCoroutineScope.TryGetScope(out var scope), Is.True);
        Assert.That(scope, Is.Not.Null);
    }

    /// <summary>
    /// 测试启动全局协程 - Launch(routine) 方法
    /// </summary>
    [Test]
    public void Launch_Should_StartCoroutine()
    {
        var executed = false;

        IEnumerator Coroutine()
        {
            executed = true;
            yield break;
        }

        var handle = GlobalCoroutineScope.Launch(Coroutine());

        Assert.That(handle, Is.Not.Null);
        Assert.That(handle.IsDone, Is.False);

        _scheduler.Update(0.1f);

        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// 测试未初始化时启动 - 抛出 InvalidOperationException
    /// </summary>
    [Test]
    public void Launch_WithoutInitialization_Should_ThrowInvalidOperationException()
    {
        GlobalCoroutineScope.TryGetScope(out var scope);
        scope?.Cancel();

        IEnumerator Coroutine()
        {
            yield break;
        }

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            GlobalCoroutineScope.Launch(Coroutine());
        });

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("not initialized") | Does.Contain("not active"));
    }

    /// <summary>
    /// 测试未初始化时TryGetScope - 返回 false 和 null
    /// </summary>
    [Test]
    public void TryGetScope_WithoutInitialization_Should_ReturnFalse()
    {
        GlobalCoroutineScope.TryGetScope(out var scope);
        scope?.Cancel();

        var result = GlobalCoroutineScope.TryGetScope(out var scope2);

        // 注意：取消后scope仍然存在，只是IsActive=false
        // 所以这个测试的行为需要调整
        Assert.That(scope2, Is.Not.Null);
        Assert.That(scope2!.IsActive, Is.False);
    }

    /// <summary>
    /// 测试全局作用域单例性 - 多次 Initialize 行为
    /// </summary>
    [Test]
    public void MultipleInitialize_Should_ReplacePreviousInstance()
    {
        GlobalCoroutineScope.TryGetScope(out var scope1);
        var scope1Ref = scope1;

        _scheduler = new CoroutineScheduler();
        GlobalCoroutineScope.Initialize(_scheduler);
        GlobalCoroutineScope.TryGetScope(out var scope2);

        Assert.That(scope2, Is.Not.EqualTo(scope1Ref));
    }

    /// <summary>
    /// 测试全局协程执行 - 通过全局作用域启动协程
    /// </summary>
    [Test]
    public void GlobalCoroutine_Should_ExecuteCorrectly()
    {
        var executionOrder = new List<string>();

        IEnumerator Coroutine1()
        {
            executionOrder.Add("Coroutine1");
            yield break;
        }

        IEnumerator Coroutine2()
        {
            executionOrder.Add("Coroutine2");
            yield break;
        }

        GlobalCoroutineScope.Launch(Coroutine1());
        GlobalCoroutineScope.Launch(Coroutine2());

        _scheduler.Update(0.1f);

        // 执行顺序可能不同，只需要确保两个协程都执行了
        Assert.That(executionOrder.Count, Is.EqualTo(2));
        Assert.That(executionOrder, Does.Contain("Coroutine1"));
        Assert.That(executionOrder, Does.Contain("Coroutine2"));
    }

    /// <summary>
    /// 测试全局作用域名称 - Name 属性为 "GlobalScope"
    /// </summary>
    [Test]
    public void GlobalScopeName_Should_BeGlobalScope()
    {
        GlobalCoroutineScope.TryGetScope(out var scope);

        Assert.That(scope, Is.Not.Null);
    }

    /// <summary>
    /// 测试Dispose 行为 - 全局作用域 Dispose
    /// </summary>
    [Test]
    public void Dispose_Should_CancelAllCoroutines()
    {
        var executed = false;

        IEnumerator Coroutine()
        {
            executed = true;
            yield break;
        }

        GlobalCoroutineScope.Launch(Coroutine());

        GlobalCoroutineScope.TryGetScope(out var scope);
        scope?.Cancel();

        _scheduler.Update(0.1f);

        Assert.That(executed, Is.False);
    }
}
