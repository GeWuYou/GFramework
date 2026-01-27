using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     AsyncOperation的单元测试类
///     测试内容包括：
///     - 初始化状态
///     - 完成和状态检查
///     - 异常处理
///     - 延续操作
///     - GetAwaiter
///     - IsCompleted属性
/// </summary>
[TestFixture]
public class AsyncOperationTests
{
    /// <summary>
    ///     验证AsyncOperation初始状态为未完成
    /// </summary>
    [Test]
    public void AsyncOperation_Should_Not_Be_Done_Initially()
    {
        var op = new AsyncOperation();

        Assert.That(op.IsDone, Is.False);
    }

    /// <summary>
    ///     验证AsyncOperation初始状态IsCompleted为false
    /// </summary>
    [Test]
    public void AsyncOperation_Should_Not_Be_Completed_Initially()
    {
        var op = new AsyncOperation();

        Assert.That(op.IsCompleted, Is.False);
    }

    /// <summary>
    ///     验证SetCompleted后IsDone应该为true
    /// </summary>
    [Test]
    public void SetCompleted_Should_Set_IsDone_To_True()
    {
        var op = new AsyncOperation();

        op.SetCompleted();

        Assert.That(op.IsDone, Is.True);
    }

    /// <summary>
    ///     验证SetCompleted后IsCompleted应该为true
    /// </summary>
    [Test]
    public void SetCompleted_Should_Set_IsCompleted_To_True()
    {
        var op = new AsyncOperation();

        op.SetCompleted();

        Assert.That(op.IsCompleted, Is.True);
    }

    /// <summary>
    ///     验证SetCompleted只能被调用一次
    /// </summary>
    [Test]
    public void SetCompleted_Should_Be_Idempotent()
    {
        var op = new AsyncOperation();

        op.SetCompleted();
        op.SetCompleted();
        op.SetCompleted();

        Assert.That(op.IsDone, Is.True);
    }

    /// <summary>
    ///     验证SetException后IsDone应该为true
    /// </summary>
    [Test]
    public void SetException_Should_Set_IsDone_To_True()
    {
        var op = new AsyncOperation();

        op.SetException(new InvalidOperationException("Test exception"));

        Assert.That(op.IsDone, Is.True);
    }

    /// <summary>
    ///     验证SetException后Task应该包含异常
    /// </summary>
    [Test]
    public void SetException_Should_Set_Exception_On_Task()
    {
        var op = new AsyncOperation();
        var expectedException = new InvalidOperationException("Test exception");

        op.SetException(expectedException);

        Assert.That(async () => await op.Task, Throws.InstanceOf<InvalidOperationException>());
    }

    /// <summary>
    ///     验证OnCompleted应该在已完成时立即执行延续
    /// </summary>
    [Test]
    public void OnCompleted_Should_Execute_Immediately_When_Already_Completed()
    {
        var op = new AsyncOperation();
        var continuationCalled = false;

        op.SetCompleted();
        op.OnCompleted(() => continuationCalled = true);

        Assert.That(continuationCalled, Is.True);
    }

    /// <summary>
    ///     验证OnCompleted应该在未完成时不立即执行延续
    /// </summary>
    [Test]
    public void OnCompleted_Should_Not_Execute_Immediately_When_Not_Completed()
    {
        var op = new AsyncOperation();
        var continuationCalled = false;

        op.OnCompleted(() => continuationCalled = true);

        Assert.That(continuationCalled, Is.False);
    }

    /// <summary>
    ///     验证延续应该在SetCompleted后被调用
    /// </summary>
    [Test]
    public void Continuation_Should_Be_Called_After_SetCompleted()
    {
        var op = new AsyncOperation();
        var continuationCalled = false;

        op.OnCompleted(() => continuationCalled = true);
        op.SetCompleted();

        Assert.That(continuationCalled, Is.True);
    }

    /// <summary>
    ///     验证多个延续应该都能被调用
    /// </summary>
    [Test]
    public void Multiple_Continuations_Should_All_Be_Called()
    {
        var op = new AsyncOperation();
        var callCount = 0;

        op.OnCompleted(() => callCount++);
        op.OnCompleted(() => callCount++);
        op.OnCompleted(() => callCount++);
        op.SetCompleted();

        Assert.That(callCount, Is.EqualTo(3));
    }

    /// <summary>
    ///     验证延续应该在SetException后被调用
    /// </summary>
    [Test]
    public void Continuation_Should_Be_Called_After_SetException()
    {
        var op = new AsyncOperation();
        var continuationCalled = false;

        op.OnCompleted(() => continuationCalled = true);
        op.SetException(new InvalidOperationException("Test"));

        Assert.That(continuationCalled, Is.True);
    }

    /// <summary>
    ///     验证SetCompleted后设置的延续也应该被调用
    /// </summary>
    [Test]
    public void Continuation_Registered_After_Completed_Should_Be_Called()
    {
        var op = new AsyncOperation();
        var firstCalled = false;
        var secondCalled = false;

        op.OnCompleted(() => firstCalled = true);
        op.SetCompleted();
        op.OnCompleted(() => secondCalled = true);

        Assert.That(firstCalled, Is.True);
        Assert.That(secondCalled, Is.True);
    }

    /// <summary>
    ///     验证GetAwaiter应该返回自身
    /// </summary>
    [Test]
    public void GetAwaiter_Should_Return_Self()
    {
        var op = new AsyncOperation();

        var awaiter = op.GetAwaiter();

        Assert.That(awaiter, Is.SameAs(op));
    }

    /// <summary>
    ///     验证Update方法不应该改变状态
    /// </summary>
    [Test]
    public void Update_Should_Not_Change_State()
    {
        var op = new AsyncOperation();

        op.Update(0.1);

        Assert.That(op.IsDone, Is.False);
    }

    /// <summary>
    ///     验证AsyncOperation实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void AsyncOperation_Should_Implement_IYieldInstruction()
    {
        var op = new AsyncOperation();

        Assert.That(op, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    ///     验证Task属性应该返回有效的Task
    /// </summary>
    [Test]
    public void Task_Property_Should_Return_Valid_Task()
    {
        var op = new AsyncOperation();

        Assert.That(op.Task, Is.Not.Null);
    }

    /// <summary>
    ///     验证SetCompleted后Task应该完成
    /// </summary>
    [Test]
    public async Task Task_Should_Complete_After_SetCompleted()
    {
        var op = new AsyncOperation();

        op.SetCompleted();

        await op.Task;

        Assert.That(op.Task.IsCompleted, Is.True);
    }

    /// <summary>
    ///     验证SetException后Task应该失败
    /// </summary>
    [Test]
    public void Task_Should_Fault_After_SetException()
    {
        var op = new AsyncOperation();

        op.SetException(new InvalidOperationException("Test"));

        Assert.That(op.Task.IsFaulted, Is.True);
    }

    /// <summary>
    ///     验证SetCompleted只能设置一次
    /// </summary>
    [Test]
    public void SetCompleted_Should_Only_Set_Once()
    {
        var op = new AsyncOperation();
        var firstCallCompleted = false;
        var secondCallCompleted = false;

        op.OnCompleted(() => firstCallCompleted = true);
        op.SetCompleted();

        op.OnCompleted(() => secondCallCompleted = true);
        op.SetCompleted();

        Assert.That(firstCallCompleted, Is.True);
        Assert.That(secondCallCompleted, Is.True);
    }

    /// <summary>
    ///     验证SetException只能在未完成时设置
    /// </summary>
    [Test]
    public void SetException_Should_Not_Work_After_SetCompleted()
    {
        var op = new AsyncOperation();

        op.SetCompleted();
        op.SetException(new InvalidOperationException("Test"));

        Assert.That(op.Task.IsCompletedSuccessfully, Is.True);
        Assert.That(op.Task.IsFaulted, Is.False);
    }

    /// <summary>
    ///     验证SetCompleted不能在SetException后设置
    /// </summary>
    [Test]
    public void SetCompleted_Should_Not_Work_After_SetException()
    {
        var op = new AsyncOperation();

        op.SetException(new InvalidOperationException("Test"));
        op.SetCompleted();

        Assert.That(op.Task.IsFaulted, Is.True);
        Assert.That(op.Task.IsCompletedSuccessfully, Is.False);
    }

    /// <summary>
    ///     验证延续抛出的异常应该被捕获
    /// </summary>
    [Test]
    public void Continuation_Exception_Should_Be_Caught()
    {
        var op = new AsyncOperation();

        op.OnCompleted(() => throw new InvalidOperationException("Test exception"));

        Assert.DoesNotThrow(() => op.SetCompleted());
    }
}