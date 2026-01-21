using System.Collections;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using NUnit.Framework;
using System.Threading;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// 协程调度器测试类，验证协程调度器的调度和执行功能
/// </summary>
[TestFixture]
public class CoroutineSchedulerTests
{
    private CoroutineScheduler _scheduler = null!;
    private CoroutineScope _scope = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduler = new CoroutineScheduler();
        _scope = new CoroutineScope(_scheduler, "TestScope");
    }

    [TearDown]
    public void TearDown()
    {
        _scope?.Dispose();
    }

    /// <summary>
    /// 测试基础更新操作 - Update(deltaTime) 方法
    /// </summary>
    [Test]
    public void Update_Should_ProcessCoroutines()
    {
        var executed = false;
        IEnumerator Coroutine()
        {
            executed = true;
            yield break;
        }

        _scope.Launch(Coroutine());
        Assert.That(executed, Is.False);

        _scheduler.Update(0.1f);
        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// 测试ActiveCount 属性 - 正确统计活跃协程
    /// </summary>
    [Test]
    public void ActiveCount_Should_CountActiveCoroutines()
    {
        Assert.That(_scheduler.ActiveCount, Is.EqualTo(0));

        _scope.Launch(CreateSimpleCoroutine());
        Assert.That(_scheduler.ActiveCount, Is.EqualTo(1));

        _scope.Launch(CreateSimpleCoroutine());
        Assert.That(_scheduler.ActiveCount, Is.EqualTo(2));

        _scope.Launch(CreateSimpleCoroutine());
        Assert.That(_scheduler.ActiveCount, Is.EqualTo(3));

        _scheduler.Update(0.1f);
        Assert.That(_scheduler.ActiveCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 测试多协程并发执行 - 同时启动多个协程
    /// </summary>
    [Test]
    public void MultipleCoroutines_Should_ExecuteConcurrently()
    {
        var executionCount = 0;

        IEnumerator Coroutine()
        {
            Interlocked.Increment(ref executionCount);
            yield break;
        }

        _scope.Launch(Coroutine());
        _scope.Launch(Coroutine());
        _scope.Launch(Coroutine());

        _scheduler.Update(0.1f);

        Assert.That(executionCount, Is.EqualTo(3));
    }

    /// <summary>
    /// 测试协程完成移除 - IsDone 协程自动移除
    /// </summary>
    [Test]
    public void CompletedCoroutine_Should_BeRemoved()
    {
        var handle = _scope.Launch(CreateSimpleCoroutine());

        Assert.That(_scheduler.ActiveCount, Is.EqualTo(1));

        _scheduler.Update(0.1f);

        Assert.That(_scheduler.ActiveCount, Is.EqualTo(0));
        Assert.That(handle.IsDone, Is.True);
    }

    /// <summary>
    /// 测试作用域不活跃时取消 - Scope.IsActive = false 时取消
    /// </summary>
    [Test]
    public void InactiveScope_Should_CancelCoroutines()
    {
        var executed = false;
        IEnumerator Coroutine()
        {
            executed = true;
            yield break;
        }

        var handle = _scope.Launch(Coroutine());
        _scope.Cancel();

        _scheduler.Update(0.1f);

        Assert.That(executed, Is.False);
        Assert.That(handle.IsCancelled, Is.True);
        Assert.That(_scheduler.ActiveCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 测试线程安全检查 - 跨线程调用抛出异常
    /// </summary>
    [Test]
    public void CrossThreadUpdate_Should_ThrowException()
    {
        var exceptionThrown = false;
        Exception? caughtException = null;

        // 先在当前线程调用一次，初始化线程ID
        _scheduler.Update(0.1f);

        var thread = new Thread(() =>
        {
            try
            {
                _scheduler.Update(0.1f);
            }
            catch (InvalidOperationException ex)
            {
                exceptionThrown = true;
                caughtException = ex;
            }
        });

        thread.Start();
        thread.Join();

        Assert.That(exceptionThrown, Is.True);
        Assert.That(caughtException, Is.Not.Null);
        Assert.That(caughtException!.Message, Does.Contain("must be updated on same thread"));
    }

    /// <summary>
    /// 测试待添加协程统计 - _toAdd 计入 ActiveCount
    /// </summary>
    [Test]
    public void ToAddCoroutines_Should_BeIncludedInActiveCount()
    {
        _scope.Launch(CreateSimpleCoroutine());
        _scope.Launch(CreateSimpleCoroutine());

        Assert.That(_scheduler.ActiveCount, Is.EqualTo(2));

        _scope.Launch(CreateSimpleCoroutine());

        Assert.That(_scheduler.ActiveCount, Is.EqualTo(3));
    }

    /// <summary>
    /// 测试协程添加时机 - _toAdd 正确合并到 _active
    /// </summary>
    [Test]
    public void ToAddCoroutines_Should_BeMergedIntoActive()
    {
        var executed = false;

        IEnumerator Coroutine()
        {
            executed = true;
            yield break;
        }

        _scope.Launch(Coroutine());

        Assert.That(executed, Is.False);

        _scheduler.Update(0.1f);

        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// 测试协程移除时机 - _toRemove 正确清理 _active
    /// </summary>
    [Test]
    public void ToRemoveCoroutines_Should_BeClearedFromActive()
    {
        var handle = _scope.Launch(CreateSimpleCoroutine());

        Assert.That(_scheduler.ActiveCount, Is.EqualTo(1));

        _scheduler.Update(0.1f);

        Assert.That(_scheduler.ActiveCount, Is.EqualTo(0));
        Assert.That(handle.IsDone, Is.True);
    }

    /// <summary>
    /// 测试空列表处理 - 无协程时的 Update 行为
    /// </summary>
    [Test]
    public void EmptyScheduler_Should_HandleGracefully()
    {
        Assert.DoesNotThrow(() =>
        {
            _scheduler.Update(0.1f);
            _scheduler.Update(0.2f);
            _scheduler.Update(0.3f);
        });
    }

    /// <summary>
    /// 测试高频协程启动 - 快速连续启动多个协程
    /// </summary>
    [Test]
    public void RapidCoroutineStart_Should_HandleGracefully()
    {
        var executionCount = 0;

        IEnumerator Coroutine()
        {
            Interlocked.Increment(ref executionCount);
            yield break;
        }

        for (int i = 0; i < 100; i++)
        {
            _scope.Launch(Coroutine());
        }

        Assert.That(_scheduler.ActiveCount, Is.EqualTo(100));

        _scheduler.Update(0.1f);

        Assert.That(executionCount, Is.EqualTo(100));
        Assert.That(_scheduler.ActiveCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 测试协程生命周期 - 从启动到完成的完整流程
    /// </summary>
    [Test]
    public void CoroutineLifecycle_Should_WorkCorrectly()
    {
        var executionOrder = new List<string>();

        IEnumerator Coroutine()
        {
            executionOrder.Add("Start");
            yield return new WaitForSeconds(0.1f);
            executionOrder.Add("Middle");
            yield return new WaitForSeconds(0.1f);
            executionOrder.Add("End");
        }

        var handle = _scope.Launch(Coroutine());

        Assert.That(handle.IsDone, Is.False);
        Assert.That(executionOrder, Is.Empty);

        _scheduler.Update(0.1f);
        Assert.That(handle.IsDone, Is.False);
        Assert.That(executionOrder, Is.EqualTo(new[] { "Start" }));

        _scheduler.Update(0.1f);
        Assert.That(handle.IsDone, Is.False);
        Assert.That(executionOrder, Is.EqualTo(new[] { "Start", "Middle" }));

        _scheduler.Update(0.1f);
        Assert.That(handle.IsDone, Is.True);
        Assert.That(executionOrder, Is.EqualTo(new[] { "Start", "Middle", "End" }));
    }

    /// <summary>
    /// 创建简单的协程辅助方法
    /// </summary>
    private IEnumerator CreateSimpleCoroutine()
    {
        yield break;
    }
}
