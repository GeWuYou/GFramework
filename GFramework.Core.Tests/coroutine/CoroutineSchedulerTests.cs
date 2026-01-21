using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// 协程调度器的单元测试类
/// 测试内容包括：
/// - 协程调度器的创建和初始化
/// - 运行协程
/// - 更新协程状态
/// - 暂停和恢复协程
/// - 终止协程
/// - 协程等待机制
/// - 标签管理
/// - 清空所有协程
/// - 异常处理
/// - 扩展容量
/// - 主动协程计数
/// - 时间差值属性
/// </summary>
[TestFixture]
public class CoroutineSchedulerTests
{
    /// <summary>
    /// 测试初始化方法，在每个测试方法执行前设置测试环境
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _timeSource = new TestTimeSource();
        _scheduler = new CoroutineScheduler(_timeSource, instanceId: 1, initialCapacity: 4);
    }

    /// <summary>
    /// 测试用的时间源实例
    /// </summary>
    private TestTimeSource _timeSource = null!;

    /// <summary>
    /// 测试用的协程调度器实例
    /// </summary>
    private CoroutineScheduler _scheduler = null!;

    /// <summary>
    /// 验证协程调度器创建时应该有正确的初始状态
    /// </summary>
    [Test]
    public void CoroutineScheduler_Should_Initialize_With_Correct_State()
    {
        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证协程调度器应该在创建时接受有效的时间源
    /// </summary>
    [Test]
    public void CoroutineScheduler_Should_Accept_Valid_TimeSource()
    {
        Assert.That(_scheduler.DeltaTime, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证协程调度器应该抛出ArgumentNullException当timeSource为null
    /// </summary>
    [Test]
    public void CoroutineScheduler_Should_Throw_ArgumentNullException_When_TimeSource_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new CoroutineScheduler(null!));
    }

    /// <summary>
    /// 验证运行协程应该返回有效的句柄
    /// </summary>
    [Test]
    public void Run_Should_Return_Valid_Handle()
    {
        var coroutine = CreateSimpleCoroutine();
        var handle = _scheduler.Run(coroutine);

        Assert.That(handle.IsValid, Is.True);
        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(1));
    }

    /// <summary>
    /// 验证运行null协程应该返回无效的句柄
    /// </summary>
    [Test]
    public void Run_Should_Return_Invalid_Handle_For_Null_Coroutine()
    {
        var handle = _scheduler.Run(null);

        Assert.That(handle.IsValid, Is.False);
        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证Update方法应该推进时间并更新协程状态
    /// </summary>
    [Test]
    public void Update_Should_Advance_Time_And_Update_Coroutines()
    {
        var coroutine = CreateSimpleCoroutine();
        var handle = _scheduler.Run(coroutine);

        _scheduler.Update();

        Assert.That(_scheduler.DeltaTime, Is.EqualTo(0.1));
    }

    /// <summary>
    /// 验证暂停协程应该成功
    /// </summary>
    [Test]
    public void Pause_Should_Succeed_For_Valid_Handle()
    {
        var coroutine = CreateYieldingCoroutine(new Delay(1.0));
        var handle = _scheduler.Run(coroutine);

        var result = _scheduler.Pause(handle);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// 验证暂停无效的句柄应该失败
    /// </summary>
    [Test]
    public void Pause_Should_Fail_For_Invalid_Handle()
    {
        var result = _scheduler.Pause(default);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// 验证恢复协程应该成功
    /// </summary>
    [Test]
    public void Resume_Should_Succeed_For_Valid_Handle()
    {
        var coroutine = CreateYieldingCoroutine(new Delay(1.0));
        var handle = _scheduler.Run(coroutine);
        _scheduler.Pause(handle);

        var result = _scheduler.Resume(handle);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// 验证恢复无效的句柄应该失败
    /// </summary>
    [Test]
    public void Resume_Should_Fail_For_Invalid_Handle()
    {
        var result = _scheduler.Resume(default);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// 验证终止协程应该成功
    /// </summary>
    [Test]
    public void Kill_Should_Succeed_For_Valid_Handle()
    {
        var coroutine = CreateYieldingCoroutine(new Delay(1.0));
        var handle = _scheduler.Run(coroutine);

        var result = _scheduler.Kill(handle);

        Assert.That(result, Is.True);
        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证终止无效的句柄应该失败
    /// </summary>
    [Test]
    public void Kill_Should_Fail_For_Invalid_Handle()
    {
        var result = _scheduler.Kill(default);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// 验证WaitForCoroutine方法应该正确设置等待状态
    /// </summary>
    [Test]
    public void WaitForCoroutine_Should_Set_Waiting_State()
    {
        var targetCoroutine = CreateYieldingCoroutine(new Delay(1.0));
        var currentCoroutine = CreateYieldingCoroutine(new WaitOneFrame());

        var targetHandle = _scheduler.Run(targetCoroutine);
        var currentHandle = _scheduler.Run(currentCoroutine);

        _scheduler.WaitForCoroutine(currentHandle, targetHandle);

        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(2));
    }

    /// <summary>
    /// 验证WaitForCoroutine方法应该抛出异常当等待自己
    /// </summary>
    [Test]
    public void WaitForCoroutine_Should_Throw_When_Waiting_For_Self()
    {
        var coroutine = CreateYieldingCoroutine(new WaitOneFrame());
        var handle = _scheduler.Run(coroutine);

        Assert.Throws<InvalidOperationException>(() => _scheduler.WaitForCoroutine(handle, handle));
    }

    /// <summary>
    /// 验证WaitForCoroutine方法应该处理无效的目标句柄
    /// </summary>
    [Test]
    public void WaitForCoroutine_Should_Handle_Invalid_Target_Handle()
    {
        var coroutine = CreateYieldingCoroutine(new WaitOneFrame());
        var handle = _scheduler.Run(coroutine);

        Assert.DoesNotThrow(() => _scheduler.WaitForCoroutine(handle, default));
    }

    /// <summary>
    /// 验证根据标签终止协程应该正确工作
    /// </summary>
    [Test]
    public void KillByTag_Should_Kill_All_Coroutines_With_Tag()
    {
        var coroutine1 = CreateYieldingCoroutine(new Delay(1.0));
        var coroutine2 = CreateYieldingCoroutine(new Delay(1.0));
        var coroutine3 = CreateYieldingCoroutine(new Delay(1.0));

        _scheduler.Run(coroutine1, "test");
        _scheduler.Run(coroutine2, "test");
        _scheduler.Run(coroutine3, "other");

        var killedCount = _scheduler.KillByTag("test");

        Assert.That(killedCount, Is.EqualTo(2));
        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(1));
    }

    /// <summary>
    /// 验证根据不存在的标签终止协程应该返回0
    /// </summary>
    [Test]
    public void KillByTag_Should_Return_Zero_For_Nonexistent_Tag()
    {
        var coroutine = CreateYieldingCoroutine(new Delay(1.0));
        _scheduler.Run(coroutine, "test");

        var killedCount = _scheduler.KillByTag("nonexistent");

        Assert.That(killedCount, Is.EqualTo(0));
        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(1));
    }

    /// <summary>
    /// 验证清空所有协程应该正确工作
    /// </summary>
    [Test]
    public void Clear_Should_Remove_All_Coroutines()
    {
        var coroutine1 = CreateYieldingCoroutine(new Delay(1.0));
        var coroutine2 = CreateYieldingCoroutine(new Delay(1.0));

        _scheduler.Run(coroutine1);
        _scheduler.Run(coroutine2);

        var clearedCount = _scheduler.Clear();

        Assert.That(clearedCount, Is.EqualTo(2));
        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证清空空的调度器应该返回0
    /// </summary>
    [Test]
    public void Clear_Should_Return_Zero_For_Empty_Scheduler()
    {
        var clearedCount = _scheduler.Clear();

        Assert.That(clearedCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证协程调度器应该正确处理协程异常
    /// </summary>
    [Test]
    public void Scheduler_Should_Handle_Coroutine_Exceptions()
    {
        var coroutine = CreateExceptionCoroutine();
        _scheduler.Run(coroutine);

        Assert.DoesNotThrow(() => _scheduler.Update());
    }

    /// <summary>
    /// 验证协程调度器应该在协程抛出异常后减少活跃协程计数
    /// </summary>
    [Test]
    public void Scheduler_Should_Decrement_ActiveCount_After_Exception()
    {
        var coroutine = CreateExceptionCoroutine();
        _scheduler.Run(coroutine);

        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(1));

        _scheduler.Update();

        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证协程调度器应该扩展容量当槽位已满
    /// </summary>
    [Test]
    public void Scheduler_Should_Expand_Capacity_When_Slots_Full()
    {
        var coroutines = new List<IEnumerator<IYieldInstruction>>();
        for (var i = 0; i < 10; i++)
        {
            coroutines.Add(CreateYieldingCoroutine(new Delay(1.0)));
        }

        foreach (var coroutine in coroutines)
        {
            _scheduler.Run(coroutine);
        }

        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(10));
    }

    /// <summary>
    /// 验证协程调度器应该使用提供的时间源
    /// </summary>
    [Test]
    public void Scheduler_Should_Use_Provided_TimeSource()
    {
        var coroutine = CreateSimpleCoroutine();
        _scheduler.Run(coroutine);

        _scheduler.Update();

        Assert.That(_scheduler.DeltaTime, Is.EqualTo(0.1));
    }

    /// <summary>
    /// 验证协程调度器应该正确计算活跃协程计数
    /// </summary>
    [Test]
    public void ActiveCoroutineCount_Should_Reflect_Active_Coroutines()
    {
        var coroutine1 = CreateYieldingCoroutine(new Delay(1.0));
        var coroutine2 = CreateYieldingCoroutine(new Delay(1.0));

        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(0));

        _scheduler.Run(coroutine1);
        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(1));

        _scheduler.Run(coroutine2);
        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(2));

        var handle = _scheduler.Run(CreateYieldingCoroutine(new Delay(1.0)));
        _scheduler.Kill(handle);
        Assert.That(_scheduler.ActiveCoroutineCount, Is.EqualTo(2));
    }

    /// <summary>
    /// 验证协程可以在不同阶段完成
    /// </summary>
    [Test]
    public void Coroutines_Should_Complete_At_Different_Stages()
    {
        var immediateCount = 0;
        var delayedCount = 0;

        _scheduler.Run(CreateImmediateCoroutine(() => immediateCount++));
        _scheduler.Run(CreateYieldingCoroutine(new Delay(1.0), () => delayedCount++));

        _scheduler.Update();

        Assert.That(immediateCount, Is.EqualTo(1));
        Assert.That(delayedCount, Is.EqualTo(0));

        _scheduler.Update();

        Assert.That(delayedCount, Is.EqualTo(1));
    }

    /// <summary>
    /// 验证暂停的协程不应该被更新
    /// </summary>
    [Test]
    public void Paused_Coroutine_Should_Not_Be_Updated()
    {
        var executeCount = 0;
        var coroutine = CreateCountingCoroutine(() => executeCount++);
        var handle = _scheduler.Run(coroutine);

        _scheduler.Pause(handle);
        _scheduler.Update();

        Assert.That(executeCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证恢复的协程应该继续执行
    /// </summary>
    [Test]
    public void Resumed_Coroutine_Should_Continue_Execution()
    {
        var executeCount = 0;
        var coroutine = CreateCountingCoroutine(() => executeCount++);
        var handle = _scheduler.Run(coroutine);

        _scheduler.Pause(handle);
        _scheduler.Update();
        Assert.That(executeCount, Is.EqualTo(0));

        _scheduler.Resume(handle);
        _scheduler.Update();
        Assert.That(executeCount, Is.EqualTo(1));
    }

    /// <summary>
    /// 创建简单的立即完成协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateSimpleCoroutine()
    {
        yield break;
    }

    /// <summary>
    /// 创建带等待指令的协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateYieldingCoroutine(IYieldInstruction yieldInstruction)
    {
        yield return yieldInstruction;
    }

    /// <summary>
    /// 创建带等待指令和回调的协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateYieldingCoroutine(IYieldInstruction yieldInstruction,
        Action? onComplete = null)
    {
        yield return yieldInstruction;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 创建立即完成并执行回调的协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateImmediateCoroutine(Action? onComplete = null)
    {
        onComplete?.Invoke();
        yield break;
    }

    /// <summary>
    /// 创建计数协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateCountingCoroutine(Action? onExecute = null)
    {
        while (true)
        {
            onExecute?.Invoke();
            yield return new WaitOneFrame();
        }
    }

    /// <summary>
    /// 创建抛出异常的协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateExceptionCoroutine()
    {
        yield return new WaitOneFrame();
        throw new InvalidOperationException("Test exception");
    }
}

/// <summary>
/// 测试用时间源类，实现ITimeSource接口
/// </summary>
public class TestTimeSource : ITimeSource
{
    /// <summary>
    /// 获取当前时间
    /// </summary>
    public double CurrentTime { get; private set; }

    /// <summary>
    /// 获取时间增量
    /// </summary>
    public double DeltaTime { get; private set; }

    /// <summary>
    /// 更新时间源状态
    /// </summary>
    public void Update()
    {
        DeltaTime = 0.1;
        CurrentTime += DeltaTime;
    }
}