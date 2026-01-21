using System.Collections;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// 协程作用域扩展方法测试类，验证延迟和重复执行协程的扩展功能
/// </summary>
[TestFixture]
public class CoroutineScopeExtensionsTests
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
    /// 测试延迟启动协程 - LaunchDelayed(delay, action)
    /// </summary>
    [Test]
    public void LaunchDelayed_Should_StartCoroutineAfterDelay()
    {
        var executed = false;
        var handle = _scope.LaunchDelayed(0.2f, () => executed = true);

        Assert.That(handle.IsDone, Is.False);
        Assert.That(executed, Is.False);

        _scheduler.Update(0.1f);
        Assert.That(handle.IsDone, Is.False);
        Assert.That(executed, Is.False);

        _scheduler.Update(0.1f);
        Assert.That(handle.IsDone, Is.True);
        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// 测试延迟时间准确性 - WaitForSeconds 等待正确时长
    /// </summary>
    [Test]
    public void LaunchDelayed_DelayTime_Should_BeAccurate()
    {
        var executionTimes = new List<float>();
        var elapsedTime = 0f;

        var handle = _scope.LaunchDelayed(0.3f, () => executionTimes.Add(elapsedTime));

        elapsedTime += 0.1f;
        _scheduler.Update(0.1f);
        Assert.That(executionTimes, Is.Empty);

        elapsedTime += 0.1f;
        _scheduler.Update(0.1f);
        Assert.That(executionTimes, Is.Empty);

        elapsedTime += 0.1f;
        _scheduler.Update(0.1f);
        Assert.That(executionTimes.Count, Is.EqualTo(1));
        Assert.That(executionTimes[0], Is.EqualTo(0.3f).Within(0.001f));
    }

    /// <summary>
    /// 测试延迟后执行动作 - action 参数正确调用
    /// </summary>
    [Test]
    public void LaunchDelayed_Action_Should_BeCalled()
    {
        var actionCalled = false;
        var capturedValue = 0;

        var handle = _scope.LaunchDelayed(0.1f, () =>
        {
            actionCalled = true;
            capturedValue = 42;
        });

        _scheduler.Update(0.1f);

        Assert.That(actionCalled, Is.True);
        Assert.That(capturedValue, Is.EqualTo(42));
    }

    /// <summary>
    /// 测试重复启动协程 - LaunchRepeating(interval, action)
    /// </summary>
    [Test]
    public void LaunchRepeating_Should_ExecuteRepeatedly()
    {
        var executionCount = 0;
        var handle = _scope.LaunchRepeating(0.1f, () => executionCount++);

        Assert.That(executionCount, Is.EqualTo(0));

        _scheduler.Update(0.1f);
        Assert.That(executionCount, Is.EqualTo(1));

        _scheduler.Update(0.1f);
        Assert.That(executionCount, Is.EqualTo(2));

        _scheduler.Update(0.1f);
        Assert.That(executionCount, Is.EqualTo(3));

        handle.Cancel();

        _scheduler.Update(0.1f);
        Assert.That(executionCount, Is.EqualTo(3));
    }

    /// <summary>
    /// 测试重复间隔准确性 - 循环间隔正确
    /// </summary>
    [Test]
    public void LaunchRepeating_Interval_Should_BeAccurate()
    {
        var executionTimes = new List<float>();
        var elapsedTime = 0f;

        var handle = _scope.LaunchRepeating(0.15f, () => executionTimes.Add(elapsedTime));

        elapsedTime += 0.1f;
        _scheduler.Update(0.1f);
        Assert.That(executionTimes, Is.Empty);

        elapsedTime += 0.05f;
        _scheduler.Update(0.05f);
        Assert.That(executionTimes.Count, Is.EqualTo(1));
        Assert.That(executionTimes[0], Is.EqualTo(0.15f).Within(0.001f));

        elapsedTime += 0.15f;
        _scheduler.Update(0.15f);
        Assert.That(executionTimes.Count, Is.EqualTo(2));
        Assert.That(executionTimes[1], Is.EqualTo(0.3f).Within(0.001f));

        handle.Cancel();
    }

    /// <summary>
    /// 测试重复执行动作 - action 参数循环调用
    /// </summary>
    [Test]
    public void LaunchRepeating_Action_Should_BeCalledMultipleTimes()
    {
        var actionCalls = new List<int>();

        var handle = _scope.LaunchRepeating(0.1f, () =>
        {
            actionCalls.Add(actionCalls.Count);
        });

        _scheduler.Update(0.1f);
        _scheduler.Update(0.1f);
        _scheduler.Update(0.1f);

        Assert.That(actionCalls, Is.EqualTo(new[] { 0, 1, 2 }));

        handle.Cancel();
    }

    /// <summary>
    /// 测试空action处理 - action 为 null 不抛异常
    /// </summary>
    [Test]
    public void LaunchDelayed_WithNullAction_Should_NotThrowException()
    {
        Assert.DoesNotThrow(() =>
        {
            _scope.LaunchDelayed(0.1f, null!);
        });

        _scheduler.Update(0.1f);
    }

    /// <summary>
    /// 测试空action处理 - action 为 null 不抛异常（重复版本）
    /// </summary>
    [Test]
    public void LaunchRepeating_WithNullAction_Should_NotThrowException()
    {
        var handle = _scope.LaunchRepeating(0.1f, null!);

        Assert.DoesNotThrow(() =>
        {
            _scheduler.Update(0.1f);
            _scheduler.Update(0.1f);
        });

        handle.Cancel();
    }

    /// <summary>
    /// 测试取消延迟协程 - 返回的句柄可取消
    /// </summary>
    [Test]
    public void CancelDelayedCoroutine_Should_PreventExecution()
    {
        var executed = false;
        var handle = _scope.LaunchDelayed(0.2f, () => executed = true);

        _scheduler.Update(0.1f);
        Assert.That(executed, Is.False);

        handle.Cancel();
        _scheduler.Update(0.1f);

        Assert.That(executed, Is.False);
        Assert.That(handle.IsCancelled, Is.True);
    }

    /// <summary>
    /// 测试取消重复协程 - 返回的句柄可取消
    /// </summary>
    [Test]
    public void CancelRepeatingCoroutine_Should_StopExecution()
    {
        var executionCount = 0;
        var handle = _scope.LaunchRepeating(0.1f, () => executionCount++);

        _scheduler.Update(0.1f);
        _scheduler.Update(0.1f);
        Assert.That(executionCount, Is.EqualTo(2));

        handle.Cancel();

        _scheduler.Update(0.1f);
        _scheduler.Update(0.1f);
        Assert.That(executionCount, Is.EqualTo(2));
    }

    /// <summary>
    /// 测试多个延迟协程 - 同时启动多个延迟任务
    /// </summary>
    [Test]
    public void MultipleDelayedCoroutines_Should_ExecuteIndependently()
    {
        var executionOrder = new List<string>();

        _scope.LaunchDelayed(0.1f, () => executionOrder.Add("100ms"));
        _scope.LaunchDelayed(0.2f, () => executionOrder.Add("200ms"));
        _scope.LaunchDelayed(0.3f, () => executionOrder.Add("300ms"));

        _scheduler.Update(0.1f);
        Assert.That(executionOrder, Is.EqualTo(new[] { "100ms" }));

        _scheduler.Update(0.1f);
        Assert.That(executionOrder, Is.EqualTo(new[] { "100ms", "200ms" }));

        _scheduler.Update(0.1f);
        Assert.That(executionOrder, Is.EqualTo(new[] { "100ms", "200ms", "300ms" }));
    }

    /// <summary>
    /// 测试多个重复协程 - 同时启动多个重复任务
    /// </summary>
    [Test]
    public void MultipleRepeatingCoroutines_Should_ExecuteIndependently()
    {
        var counters = new Dictionary<string, int>
        {
            ["fast"] = 0,
            ["slow"] = 0
        };

        var fastHandle = _scope.LaunchRepeating(0.05f, () => counters["fast"]++);
        var slowHandle = _scope.LaunchRepeating(0.1f, () => counters["slow"]++);

        _scheduler.Update(0.1f);
        Assert.That(counters["fast"], Is.EqualTo(2));
        Assert.That(counters["slow"], Is.EqualTo(1));

        _scheduler.Update(0.1f);
        Assert.That(counters["fast"], Is.EqualTo(4));
        Assert.That(counters["slow"], Is.EqualTo(2));

        fastHandle.Cancel();
        slowHandle.Cancel();
    }
}
