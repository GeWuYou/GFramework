using System.Collections;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// 协程系统集成测试类，验证复杂场景下的协程交互和协同工作
/// </summary>
[TestFixture]
public class CoroutineIntegrationTests
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
    /// 测试复杂协程链式调用 - 多层嵌套协程
    /// </summary>
    [Test]
    public void ComplexChainedCoroutines_Should_ExecuteCorrectly()
    {
        var executionOrder = new List<string>();

        IEnumerator Coroutine1()
        {
            executionOrder.Add("Coroutine1-Start");
            yield return Coroutine2();
            executionOrder.Add("Coroutine1-End");
        }

        IEnumerator Coroutine2()
        {
            executionOrder.Add("Coroutine2-Start");
            yield return Coroutine3();
            executionOrder.Add("Coroutine2-End");
        }

        IEnumerator Coroutine3()
        {
            executionOrder.Add("Coroutine3-Start");
            yield return new WaitForSeconds(0.1f);
            executionOrder.Add("Coroutine3-End");
        }

        var handle = _scope.Launch(Coroutine1());
        _scheduler.Update(0.1f);

        Assert.That(handle.IsDone, Is.True);
        Assert.That(executionOrder, Is.EqualTo(new[]
        {
            "Coroutine1-Start",
            "Coroutine2-Start",
            "Coroutine3-Start",
            "Coroutine3-End",
            "Coroutine2-End",
            "Coroutine1-End"
        }));
    }

    /// <summary>
    /// 测试协程间数据传递 - 通过闭包共享状态
    /// </summary>
    [Test]
    public void CoroutineDataSharing_Should_WorkCorrectly()
    {
        var sharedData = new SharedData { Value = 0 };

        IEnumerator ProducerCoroutine()
        {
            for (int i = 1; i <= 5; i++)
            {
                sharedData.Value = i;
                yield return null;
            }
        }

        IEnumerator ConsumerCoroutine()
        {
            while (sharedData.Value < 5)
            {
                yield return new WaitForSeconds(0.05f);
            }
        }

        var producerHandle = _scope.Launch(ProducerCoroutine());
        var consumerHandle = _scope.Launch(ConsumerCoroutine());

        _scheduler.Update(0.05f);
        Assert.That(producerHandle.IsDone, Is.False);
        Assert.That(consumerHandle.IsDone, Is.False);

        _scheduler.Update(0.05f);
        _scheduler.Update(0.05f);
        _scheduler.Update(0.05f);

        Assert.That(producerHandle.IsDone, Is.True);
        Assert.That(consumerHandle.IsDone, Is.True);
        Assert.That(sharedData.Value, Is.EqualTo(5));
    }

    /// <summary>
    /// 测试协程与事件集成 - 协程中触发事件
    /// </summary>
    [Test]
    public void CoroutineEventIntegration_Should_WorkCorrectly()
    {
        var eventTriggered = false;
        Action? eventCallback = null;

        IEnumerator EventCoroutine()
        {
            yield return new WaitForSeconds(0.1f);
            eventCallback?.Invoke();
        }

        eventCallback = () => eventTriggered = true;

        var handle = _scope.Launch(EventCoroutine());
        _scheduler.Update(0.05f);

        Assert.That(eventTriggered, Is.False);

        _scheduler.Update(0.05f);

        Assert.That(eventTriggered, Is.True);
        Assert.That(handle.IsDone, Is.True);
    }

    /// <summary>
    /// 测试协程异常传播 - 嵌套协程中的异常处理
    /// </summary>
    [Test]
    public void CoroutineExceptionPropagation_Should_HandleNestedExceptions()
    {
        Exception? caughtException = null;

        IEnumerator InnerCoroutine()
        {
            yield return null;
            throw new InvalidOperationException("Inner exception");
        }

        IEnumerator OuterCoroutine()
        {
            yield return InnerCoroutine();
        }

        var handle = _scope.Launch(OuterCoroutine());
        handle.OnError += ex => caughtException = ex;

        _scheduler.Update(0.1f);

        Assert.That(caughtException, Is.Not.Null);
        Assert.That(caughtException, Is.TypeOf<InvalidOperationException>());
        Assert.That(handle.IsDone, Is.True);
    }

    /// <summary>
    /// 测试协程取消链 - 父协程取消子协程
    /// </summary>
    [Test]
    public void CoroutineCancellationChain_Should_PropagateCancels()
    {
        var innerExecuted = false;
        var outerExecuted = false;

        IEnumerator InnerCoroutine()
        {
            innerExecuted = true;
            yield break;
        }

        IEnumerator OuterCoroutine()
        {
            yield return InnerCoroutine();
            outerExecuted = true;
        }

        var innerHandle = _scope.Launch(InnerCoroutine());
        var outerHandle = _scope.Launch(OuterCoroutine());

        outerHandle.Cancel();

        _scheduler.Update(0.1f);

        Assert.That(outerExecuted, Is.False);
        Assert.That(innerExecuted, Is.True);
        Assert.That(outerHandle.IsCancelled, Is.True);
        Assert.That(innerHandle.IsDone, Is.True);
    }

    /// <summary>
    /// 测试协程超时控制 - 使用 WaitUntil 实现超时
    /// </summary>
    [Test]
    public void CoroutineTimeout_Should_WorkCorrectly()
    {
        var conditionMet = false;
        var timedOut = false;

        IEnumerator WorkerCoroutine()
        {
            yield return new WaitForSeconds(0.2f);
            conditionMet = true;
        }

        IEnumerator TimeoutCoroutine()
        {
            var worker = _scope.Launch(WorkerCoroutine());

            yield return new WaitUntil(() =>
            {
                if (worker.IsDone) return true;
                timedOut = true;
                return true;
            });

            worker.Cancel();
        }

        var handle = _scope.Launch(TimeoutCoroutine());

        _scheduler.Update(0.15f);

        Assert.That(conditionMet, Is.False);
        Assert.That(timedOut, Is.False);
        Assert.That(handle.IsDone, Is.False);

        _scheduler.Update(0.1f);

        Assert.That(conditionMet, Is.True);
        Assert.That(timedOut, Is.False);
        Assert.That(handle.IsDone, Is.True);
    }

    /// <summary>
    /// 测试协程同步等待 - 等待多个协程完成
    /// </summary>
    [Test]
    public void CoroutineSynchronization_Should_WaitForMultipleCoroutines()
    {
        var completedCount = 0;

        IEnumerator Task1()
        {
            yield return new WaitForSeconds(0.1f);
            completedCount++;
        }

        IEnumerator Task2()
        {
            yield return new WaitForSeconds(0.15f);
            completedCount++;
        }

        IEnumerator Task3()
        {
            yield return new WaitForSeconds(0.2f);
            completedCount++;
        }

        IEnumerator WaitAllCoroutine()
        {
            var task1 = _scope.Launch(Task1());
            var task2 = _scope.Launch(Task2());
            var task3 = _scope.Launch(Task3());

            yield return new WaitUntil(() =>
                task1.IsDone && task2.IsDone && task3.IsDone);
        }

        var handle = _scope.Launch(WaitAllCoroutine());

        _scheduler.Update(0.15f);
        Assert.That(completedCount, Is.EqualTo(2));
        Assert.That(handle.IsDone, Is.False);

        _scheduler.Update(0.05f);
        Assert.That(completedCount, Is.EqualTo(3));
        Assert.That(handle.IsDone, Is.True);
    }

    /// <summary>
    /// 测试协程竞态条件 - 多个协程竞争同一资源
    /// </summary>
    [Test]
    public void CoroutineRaceCondition_Should_HandleResourceCorrectly()
    {
        var resourceAccessCount = 0;
        var maxAccesses = 0;

        IEnumerator AccessResource(int id)
        {
            resourceAccessCount++;
            if (resourceAccessCount > maxAccesses)
                maxAccesses = resourceAccessCount;

            yield return new WaitForSeconds(0.05f);

            resourceAccessCount--;
            yield break;
        }

        var handles = new List<ICoroutineHandle>();

        for (int i = 0; i < 5; i++)
        {
            handles.Add(_scope.Launch(AccessResource(i)));
        }

        while (handles.Any(h => !h.IsDone))
        {
            _scheduler.Update(0.05f);
        }

        Assert.That(resourceAccessCount, Is.EqualTo(0));
        Assert.That(maxAccesses, Is.EqualTo(5));
    }

    /// <summary>
    /// 测试协程资源管理 - using 语句与协程
    /// </summary>
    [Test]
    public void CoroutineResourceManagement_Should_CleanupCorrectly()
    {
        var resourceDisposed = false;
        var resourceAccessed = false;

        var resource = new DisposableResource(() => resourceDisposed = true);

        IEnumerator ResourceUsingCoroutine()
        {
            using (resource)
            {
                yield return new WaitForSeconds(0.1f);
                resourceAccessed = true;
            }
        }

        var handle = _scope.Launch(ResourceUsingCoroutine());

        _scheduler.Update(0.05f);
        Assert.That(resourceAccessed, Is.False);
        Assert.That(resourceDisposed, Is.False);

        _scheduler.Update(0.05f);
        Assert.That(resourceAccessed, Is.True);
        Assert.That(resourceDisposed, Is.True);
        Assert.That(handle.IsDone, Is.True);
    }

    private class SharedData
    {
        public int Value { get; set; }
    }

    private class DisposableResource : IDisposable
    {
        private readonly Action _onDispose;

        public DisposableResource(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }
}
