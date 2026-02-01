using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.extensions;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     CoroutineComposeExtensions的单元测试类
///     测试内容包括：
///     - Then(Action) 方法：协程完成后执行动作
///     - Then(IEnumerator) 方法：两个协程顺序组合
/// </summary>
[TestFixture]
public class CoroutineComposeExtensionsTests
{
    /// <summary>
    ///     创建一个简单的测试协程，执行指定次数并记录执行
    /// </summary>
    private static IEnumerator<IYieldInstruction> CreateTestCoroutine(int steps, Action? onStep = null)
    {
        for (var i = 0; i < steps; i++)
        {
            onStep?.Invoke();
            yield return new WaitOneFrame();
        }
    }

    /// <summary>
    ///     验证Then(Action)应该返回有效的协程
    /// </summary>
    [Test]
    public void Then_Action_Should_Return_Valid_Coroutine()
    {
        var first = CreateTestCoroutine(1);
        var combined = first.Then(() => { });

        Assert.That(combined, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    ///     验证Then(Action)应该先执行协程再执行动作
    /// </summary>
    [Test]
    public void Then_Action_Should_Execute_Coroutine_Then_Action()
    {
        var executionOrder = new List<string>();

        var first = CreateTestCoroutine(2, () => executionOrder.Add("coroutine"));
        var combined = first.Then(() => executionOrder.Add("action"));

        // 执行组合协程
        while (combined.MoveNext())
        {
            combined.Current.Update(0.016);
        }

        Assert.That(executionOrder.Count, Is.EqualTo(3));
        Assert.That(executionOrder[0], Is.EqualTo("coroutine"));
        Assert.That(executionOrder[1], Is.EqualTo("coroutine"));
        Assert.That(executionOrder[2], Is.EqualTo("action"));
    }

    /// <summary>
    ///     验证Then(Action)应该在空协程后立即执行动作
    /// </summary>
    [Test]
    public void Then_Action_Should_Execute_Action_After_Empty_Coroutine()
    {
        var actionExecuted = false;

        var first = CreateTestCoroutine(0);
        var combined = first.Then(() => actionExecuted = true);

        // 空协程应该立即完成并执行动作
        var hasMore = combined.MoveNext();

        Assert.That(hasMore, Is.False);
        Assert.That(actionExecuted, Is.True);
    }

    /// <summary>
    ///     验证Then(Action)应该正确传递yield指令
    /// </summary>
    [Test]
    public void Then_Action_Should_Pass_Through_Yield_Instructions()
    {
        var yieldCount = 0;

        var first = CreateTestCoroutine(3);
        var combined = first.Then(() => { });

        while (combined.MoveNext())
        {
            Assert.That(combined.Current, Is.InstanceOf<IYieldInstruction>());
            yieldCount++;
        }

        Assert.That(yieldCount, Is.EqualTo(3));
    }

    /// <summary>
    ///     验证Then(Action)应该支持链式调用
    /// </summary>
    [Test]
    public void Then_Action_Should_Support_Chaining()
    {
        var executionOrder = new List<int>();

        var first = CreateTestCoroutine(1, () => executionOrder.Add(1));
        var combined = first
            .Then(() => executionOrder.Add(2))
            .Then(() => executionOrder.Add(3));

        while (combined.MoveNext())
        {
            combined.Current.Update(0.016);
        }

        Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    /// <summary>
    ///     验证Then(Action)动作中的异常应该正常抛出
    /// </summary>
    [Test]
    public void Then_Action_Should_Propagate_Exception_From_Action()
    {
        var first = CreateTestCoroutine(1);
        var combined = first.Then(() => throw new InvalidOperationException("Test exception"));

        // 第一步应该正常执行
        Assert.That(combined.MoveNext(), Is.True);
        combined.Current.Update(0.016);

        // 动作执行时应该抛出异常
        Assert.Throws<InvalidOperationException>(() => combined.MoveNext());
    }

    /// <summary>
    ///     验证Then(IEnumerator)应该返回有效的协程
    /// </summary>
    [Test]
    public void Then_Coroutine_Should_Return_Valid_Coroutine()
    {
        var first = CreateTestCoroutine(1);
        var second = CreateTestCoroutine(1);
        var combined = first.Then(second);

        Assert.That(combined, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    ///     验证Then(IEnumerator)应该顺序执行两个协程
    /// </summary>
    [Test]
    public void Then_Coroutine_Should_Execute_In_Sequence()
    {
        var executionOrder = new List<string>();

        var first = CreateTestCoroutine(2, () => executionOrder.Add("first"));
        var second = CreateTestCoroutine(2, () => executionOrder.Add("second"));
        var combined = first.Then(second);

        while (combined.MoveNext())
        {
            combined.Current.Update(0.016);
        }

        Assert.That(executionOrder.Count, Is.EqualTo(4));
        Assert.That(executionOrder[0], Is.EqualTo("first"));
        Assert.That(executionOrder[1], Is.EqualTo("first"));
        Assert.That(executionOrder[2], Is.EqualTo("second"));
        Assert.That(executionOrder[3], Is.EqualTo("second"));
    }

    /// <summary>
    ///     验证Then(IEnumerator)应该正确处理空的第一个协程
    /// </summary>
    [Test]
    public void Then_Coroutine_Should_Handle_Empty_First_Coroutine()
    {
        var secondExecuted = false;

        var first = CreateTestCoroutine(0);
        var second = CreateTestCoroutine(1, () => secondExecuted = true);
        var combined = first.Then(second);

        // 应该立即开始执行第二个协程
        Assert.That(combined.MoveNext(), Is.True);
        combined.Current.Update(0.016);
        Assert.That(combined.MoveNext(), Is.False);

        Assert.That(secondExecuted, Is.True);
    }

    /// <summary>
    ///     验证Then(IEnumerator)应该正确处理空的第二个协程
    /// </summary>
    [Test]
    public void Then_Coroutine_Should_Handle_Empty_Second_Coroutine()
    {
        var firstExecuteCount = 0;

        var first = CreateTestCoroutine(2, () => firstExecuteCount++);
        var second = CreateTestCoroutine(0);
        var combined = first.Then(second);

        while (combined.MoveNext())
        {
            combined.Current.Update(0.016);
        }

        Assert.That(firstExecuteCount, Is.EqualTo(2));
    }

    /// <summary>
    ///     验证Then(IEnumerator)应该正确处理两个空协程
    /// </summary>
    [Test]
    public void Then_Coroutine_Should_Handle_Both_Empty_Coroutines()
    {
        var first = CreateTestCoroutine(0);
        var second = CreateTestCoroutine(0);
        var combined = first.Then(second);

        Assert.That(combined.MoveNext(), Is.False);
    }

    /// <summary>
    ///     验证Then(IEnumerator)应该正确传递所有yield指令
    /// </summary>
    [Test]
    public void Then_Coroutine_Should_Pass_Through_All_Yield_Instructions()
    {
        var yieldCount = 0;

        var first = CreateTestCoroutine(3);
        var second = CreateTestCoroutine(2);
        var combined = first.Then(second);

        while (combined.MoveNext())
        {
            Assert.That(combined.Current, Is.InstanceOf<IYieldInstruction>());
            yieldCount++;
        }

        Assert.That(yieldCount, Is.EqualTo(5));
    }

    /// <summary>
    ///     验证Then(IEnumerator)应该支持链式调用
    /// </summary>
    [Test]
    public void Then_Coroutine_Should_Support_Chaining()
    {
        var executionOrder = new List<int>();

        var first = CreateTestCoroutine(1, () => executionOrder.Add(1));
        var second = CreateTestCoroutine(1, () => executionOrder.Add(2));
        var third = CreateTestCoroutine(1, () => executionOrder.Add(3));

        var combined = first.Then(second).Then(third);

        while (combined.MoveNext())
        {
            combined.Current.Update(0.016);
        }

        Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    /// <summary>
    ///     验证Then应该支持混合链式调用（协程和动作）
    /// </summary>
    [Test]
    public void Then_Should_Support_Mixed_Chaining()
    {
        var executionOrder = new List<string>();

        var first = CreateTestCoroutine(1, () => executionOrder.Add("coroutine1"));
        var second = CreateTestCoroutine(1, () => executionOrder.Add("coroutine2"));

        var combined = first
            .Then(() => executionOrder.Add("action1"))
            .Then(second)
            .Then(() => executionOrder.Add("action2"));

        while (combined.MoveNext())
        {
            combined.Current.Update(0.016);
        }

        Assert.That(executionOrder, Is.EqualTo(new[] { "coroutine1", "action1", "coroutine2", "action2" }));
    }

    /// <summary>
    ///     验证Then应该处理带有延迟指令的协程
    /// </summary>
    [Test]
    public void Then_Should_Handle_Delay_Instructions()
    {
        var actionExecuted = false;

        IEnumerator<IYieldInstruction> DelayCoroutine()
        {
            yield return new Delay(0.5);
        }

        var combined = DelayCoroutine().Then(() => actionExecuted = true);

        // 第一步返回延迟指令
        Assert.That(combined.MoveNext(), Is.True);
        Assert.That(combined.Current, Is.InstanceOf<Delay>());

        // 更新延迟指令直到完成
        var delay = (Delay)combined.Current;
        delay.Update(0.5);

        // 完成后执行动作
        Assert.That(combined.MoveNext(), Is.False);
        Assert.That(actionExecuted, Is.True);
    }

    /// <summary>
    ///     验证Then应该正确处理多层嵌套
    /// </summary>
    [Test]
    public void Then_Should_Handle_Deep_Nesting()
    {
        var count = 0;

        var coroutine = CreateTestCoroutine(1, () => count++);

        // 深度嵌套
        for (var i = 0; i < 10; i++)
        {
            var nextCoroutine = CreateTestCoroutine(1, () => count++);
            coroutine = coroutine.Then(nextCoroutine);
        }

        while (coroutine.MoveNext())
        {
            coroutine.Current.Update(0.016);
        }

        Assert.That(count, Is.EqualTo(11));
    }
}