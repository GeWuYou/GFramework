using System.Collections;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// 协程句柄测试类，验证协程句柄的完整生命周期和功能
/// </summary>
[TestFixture]
public class CoroutineHandleTests
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
    /// 测试协程基础执行 - 简单的 IEnumerator 执行
    /// </summary>
    [Test]
    public void BasicCoroutine_Should_ExecuteSuccessfully()
    {
        var executed = false;
        var routine = CreateSimpleCoroutine(() => executed = true);

        var handle = _scope.Launch(routine);
        Assert.That(handle.IsDone, Is.False);

        _scheduler.Update(0.1f);
        Assert.That(handle.IsDone, Is.True);
        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// 测试协程完成状态 - IsDone 属性正确性
    /// </summary>
    [Test]
    public void IsDone_Should_BeCorrect()
    {
        var routine = CreateSimpleCoroutine(() => { });
        var handle = _scope.Launch(routine);

        Assert.That(handle.IsDone, Is.False);

        _scheduler.Update(0.1f);

        Assert.That(handle.IsDone, Is.True);
    }

    /// <summary>
    /// 测试协程取消操作 - Cancel() 方法调用
    /// </summary>
    [Test]
    public void Cancel_Should_StopCoroutine()
    {
        var executed = false;
        var routine = CreateSimpleCoroutine(() => executed = true);

        var handle = _scope.Launch(routine);
        handle.Cancel();

        Assert.That(handle.IsCancelled, Is.True);
        Assert.That(handle.IsDone, Is.True);

        _scheduler.Update(0.1f);

        Assert.That(executed, Is.False);
    }

    /// <summary>
    /// 测试嵌套协程执行 - yield return IEnumerator 支持
    /// </summary>
    [Test]
    public void NestedCoroutine_Should_ExecuteSuccessfully()
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

        var handle = _scope.Launch(OuterCoroutine());
        _scheduler.Update(0.1f);

        Assert.That(handle.IsDone, Is.True);
        Assert.That(innerExecuted, Is.True);
        Assert.That(outerExecuted, Is.True);
    }

    /// <summary>
    /// 测试协程句柄嵌套 - yield return CoroutineHandle 支持
    /// </summary>
    [Test]
    public void YieldCoroutineHandle_Should_WaitForCompletion()
    {
        var innerExecuted = false;
        var outerExecuted = false;

        IEnumerator InnerCoroutine()
        {
            innerExecuted = true;
            yield return null;
        }

        IEnumerator OuterCoroutine()
        {
            var innerHandle = _scope.Launch(InnerCoroutine());
            yield return innerHandle;
            outerExecuted = true;
        }

        var handle = _scope.Launch(OuterCoroutine());
        _scheduler.Update(0.1f);

        Assert.That(handle.IsDone, Is.True);
        Assert.That(innerExecuted, Is.True);
        Assert.That(outerExecuted, Is.True);
    }

    /// <summary>
    /// 测试Yield指令支持 - yield return IYieldInstruction 支持
    /// </summary>
    [Test]
    public void YieldInstruction_Should_BeSupported()
    {
        var executed = false;

        IEnumerator Coroutine()
        {
            yield return new WaitForSeconds(0.1f);
            executed = true;
        }

        var handle = _scope.Launch(Coroutine());
        _scheduler.Update(0.05f);

        Assert.That(handle.IsDone, Is.False);
        Assert.That(executed, Is.False);

        _scheduler.Update(0.05f);

        Assert.That(handle.IsDone, Is.True);
        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// 测试OnComplete 事件触发
    /// </summary>
    [Test]
    public void OnComplete_Should_TriggerEvent()
    {
        var eventTriggered = false;
        var routine = CreateSimpleCoroutine(() => { });

        var handle = _scope.Launch(routine);
        handle.OnComplete += () => eventTriggered = true;

        Assert.That(eventTriggered, Is.False);

        _scheduler.Update(0.1f);

        Assert.That(eventTriggered, Is.True);
    }

    /// <summary>
    /// 测试OnComplete 事件在取消时触发
    /// </summary>
    [Test]
    public void OnComplete_Should_TriggerOnCancel()
    {
        var eventTriggered = false;
        var routine = CreateSimpleCoroutine(() => { });

        var handle = _scope.Launch(routine);
        handle.OnComplete += () => eventTriggered = true;

        handle.Cancel();

        Assert.That(eventTriggered, Is.True);
    }

    /// <summary>
    /// 测试OnError 事件触发 - 捕获协程中的异常
    /// </summary>
    [Test]
    public void OnError_Should_TriggerEvent()
    {
        Exception? caughtException = null;

        IEnumerator Coroutine()
        {
            yield return null;
            throw new InvalidOperationException("Test exception");
        }

        var handle = _scope.Launch(Coroutine());
        handle.OnError += ex => caughtException = ex;

        _scheduler.Update(0.1f);

        Assert.That(caughtException, Is.Not.Null);
        Assert.That(caughtException, Is.TypeOf<InvalidOperationException>());
    }

    /// <summary>
    /// 测试协程栈管理 - Push/Pop 操作正确性
    /// </summary>
    [Test]
    public void CoroutineStack_Should_ManageNestedCoroutines()
    {
        var executionOrder = new List<string>();

        IEnumerator Level3()
        {
            executionOrder.Add("Level3");
            yield break;
        }

        IEnumerator Level2()
        {
            yield return Level3();
            executionOrder.Add("Level2");
        }

        IEnumerator Level1()
        {
            yield return Level2();
            executionOrder.Add("Level1");
        }

        var handle = _scope.Launch(Level1());
        _scheduler.Update(0.1f);

        Assert.That(handle.IsDone, Is.True);
        Assert.That(executionOrder, Is.EqualTo(new[] { "Level3", "Level2", "Level1" }));
    }

    /// <summary>
    /// 测试异常状态处理 - HandleError 方法
    /// </summary>
    [Test]
    public void Exception_Should_HandleGracefully()
    {
        var executed = false;

        IEnumerator Coroutine()
        {
            executed = true;
            yield return null;
            throw new Exception("Error");
        }

        var handle = _scope.Launch(Coroutine());
        _scheduler.Update(0.1f);

        Assert.That(executed, Is.True);
        Assert.That(handle.IsDone, Is.True);
    }

    /// <summary>
    /// 测试等待指令状态管理 - _waitingInstruction 更新
    /// </summary>
    [Test]
    public void WaitingInstruction_Should_UpdateCorrectly()
    {
        var executed = false;

        IEnumerator Coroutine()
        {
            yield return new WaitForSeconds(0.2f);
            executed = true;
        }

        var handle = _scope.Launch(Coroutine());

        _scheduler.Update(0.1f);
        Assert.That(handle.IsDone, Is.False);

        _scheduler.Update(0.1f);
        Assert.That(handle.IsDone, Is.True);
        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// 测试协程多次执行 - 同一协程多次启动
    /// </summary>
    [Test]
    public void SameCoroutine_Should_ExecuteMultipleTimes()
    {
        var executionCount = 0;

        IEnumerator Coroutine()
        {
            executionCount++;
            yield break;
        }

        var handle1 = _scope.Launch(Coroutine());
        var handle2 = _scope.Launch(Coroutine());

        _scheduler.Update(0.1f);

        Assert.That(handle1.IsDone, Is.True);
        Assert.That(handle2.IsDone, Is.True);
        Assert.That(executionCount, Is.EqualTo(2));
    }

    /// <summary>
    /// 测试不支持的Yield类型 - 抛出 InvalidOperationException
    /// </summary>
    [Test]
    public void UnsupportedYieldType_Should_ThrowInvalidOperationException()
    {
        IEnumerator Coroutine()
        {
            yield return 123;
        }

        var handle = _scope.Launch(Coroutine());
        Exception? caughtException = null;
        handle.OnError += ex => caughtException = ex;

        _scheduler.Update(0.1f);

        Assert.That(caughtException, Is.Not.Null);
        Assert.That(caughtException, Is.TypeOf<InvalidOperationException>());
        Assert.That(caughtException!.Message, Does.Contain("Unsupported yield type"));
    }

    /// <summary>
    /// 测试协程上下文获取 - Context 属性
    /// </summary>
    [Test]
    public void Context_Should_BeAccessible()
    {
        IEnumerator Coroutine()
        {
            yield break;
        }

        var handle = _scope.Launch(Coroutine());

        Assert.That(handle.Context, Is.Not.Null);
        Assert.That(handle.Context.Scope, Is.EqualTo(_scope));
        Assert.That(handle.Context.Scheduler, Is.EqualTo(_scheduler));
        Assert.That(handle.Context.Owner, Is.EqualTo(_scope));
    }

    /// <summary>
    /// 创建简单的协程辅助方法
    /// </summary>
    private IEnumerator CreateSimpleCoroutine(Action action)
    {
        action();
        yield break;
    }
}
