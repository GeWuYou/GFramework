using System.Collections;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// 协程作用域测试类，验证协程作用域的管理和控制功能
/// </summary>
[TestFixture]
public class CoroutineScopeTests
{
    private CoroutineScheduler _scheduler = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduler = new CoroutineScheduler();
    }

    /// <summary>
    /// 测试基础协程启动 - Launch(IEnumerator) 方法
    /// </summary>
    [Test]
    public void Launch_Should_StartCoroutine()
    {
        var scope = new CoroutineScope(_scheduler, "TestScope");
        var executed = false;

        IEnumerator Coroutine()
        {
            executed = true;
            yield break;
        }

        var handle = scope.Launch(Coroutine());

        Assert.That(handle, Is.Not.Null);
        Assert.That(handle.IsDone, Is.False);

        _scheduler.Update(0.1f);

        Assert.That(executed, Is.True);
        Assert.That(handle.IsDone, Is.True);

        scope.Dispose();
    }

    /// <summary>
    /// 测试协程作用域状态 - IsActive 属性
    /// </summary>
    [Test]
    public void IsActive_Should_BeCorrect()
    {
        var scope = new CoroutineScope(_scheduler, "TestScope");

        Assert.That(scope.IsActive, Is.True);

        scope.Cancel();

        Assert.That(scope.IsActive, Is.False);

        scope.Dispose();
    }

    /// <summary>
    /// 测试协程作用域取消 - Cancel() 方法
    /// </summary>
    [Test]
    public void Cancel_Should_StopAllCoroutines()
    {
        var scope = new CoroutineScope(_scheduler, "TestScope");
        var executed1 = false;
        var executed2 = false;
        var executed3 = false;

        IEnumerator Coroutine1()
        {
            executed1 = true;
            yield break;
        }

        IEnumerator Coroutine2()
        {
            executed2 = true;
            yield break;
        }

        IEnumerator Coroutine3()
        {
            executed3 = true;
            yield break;
        }

        var handle1 = scope.Launch(Coroutine1());
        var handle2 = scope.Launch(Coroutine2());
        var handle3 = scope.Launch(Coroutine3());

        scope.Cancel();

        _scheduler.Update(0.1f);

        Assert.That(executed1, Is.False);
        Assert.That(executed2, Is.False);
        Assert.That(executed3, Is.False);
        Assert.That(handle1.IsCancelled, Is.True);
        Assert.That(handle2.IsCancelled, Is.True);
        Assert.That(handle3.IsCancelled, Is.True);

        scope.Dispose();
    }

    /// <summary>
    /// 测试子作用域管理 - 父子作用域关系
    /// </summary>
    [Test]
    public void ChildScope_Should_BeManagedByParent()
    {
        var parentScope = new CoroutineScope(_scheduler, "ParentScope");
        var childScope = new CoroutineScope(_scheduler, "ChildScope", parentScope);

        Assert.That(parentScope.IsActive, Is.True);
        Assert.That(childScope.IsActive, Is.True);

        parentScope.Cancel();

        Assert.That(parentScope.IsActive, Is.False);
        Assert.That(childScope.IsActive, Is.False);

        childScope.Dispose();
        parentScope.Dispose();
    }

    /// <summary>
    /// 测试取消传播 - 父作用域取消时子作用域也取消
    /// </summary>
    [Test]
    public void ParentCancel_Should_PropagateToChild()
    {
        var parentScope = new CoroutineScope(_scheduler, "ParentScope");
        var childScope = new CoroutineScope(_scheduler, "ChildScope", parentScope);
        var executed = false;

        IEnumerator Coroutine()
        {
            executed = true;
            yield break;
        }

        var handle = childScope.Launch(Coroutine());
        parentScope.Cancel();

        _scheduler.Update(0.1f);

        Assert.That(executed, Is.False);
        Assert.That(handle.IsCancelled, Is.True);

        childScope.Dispose();
        parentScope.Dispose();
    }

    /// <summary>
    /// 测试运行中协程跟踪 - _runningCoroutines 集合
    /// </summary>
    [Test]
    public void RunningCoroutines_Should_BeTracked()
    {
        var scope = new CoroutineScope(_scheduler, "TestScope");

        IEnumerator Coroutine()
        {
            yield break;
        }

        var handle1 = scope.Launch(Coroutine());
        var handle2 = scope.Launch(Coroutine());
        var handle3 = scope.Launch(Coroutine());

        _scheduler.Update(0.1f);

        Assert.That(handle1.IsDone, Is.True);
        Assert.That(handle2.IsDone, Is.True);
        Assert.That(handle3.IsDone, Is.True);

        scope.Dispose();
    }

    /// <summary>
    /// 测试协程完成自动移除 - OnComplete 事件处理
    /// </summary>
    [Test]
    public void CompletedCoroutine_Should_BeRemovedFromTracking()
    {
        var scope = new CoroutineScope(_scheduler, "TestScope");

        IEnumerator Coroutine()
        {
            yield break;
        }

        var handle = scope.Launch(Coroutine());

        Assert.That(handle.IsDone, Is.False);

        _scheduler.Update(0.1f);

        Assert.That(handle.IsDone, Is.True);

        scope.Dispose();
    }

    /// <summary>
    /// 测试协程错误自动移除 - OnError 事件处理
    /// </summary>
    [Test]
    public void FailedCoroutine_Should_BeRemovedFromTracking()
    {
        var scope = new CoroutineScope(_scheduler, "TestScope");

        IEnumerator Coroutine()
        {
            yield return null;
            throw new Exception("Test error");
        }

        var handle = scope.Launch(Coroutine());

        Assert.That(handle.IsDone, Is.False);

        _scheduler.Update(0.1f);

        Assert.That(handle.IsDone, Is.True);

        scope.Dispose();
    }

    /// <summary>
    /// 测试作用域销毁 - Dispose() 方法调用 Cancel
    /// </summary>
    [Test]
    public void Dispose_Should_CallCancel()
    {
        var scope = new CoroutineScope(_scheduler, "TestScope");
        var executed = false;

        IEnumerator Coroutine()
        {
            executed = true;
            yield break;
        }

        var handle = scope.Launch(Coroutine());
        scope.Dispose();

        _scheduler.Update(0.1f);

        Assert.That(executed, Is.False);
        Assert.That(handle.IsCancelled, Is.True);
    }

    /// <summary>
    /// 测试不活跃作用域拒绝启动 - 抛出 InvalidOperationException
    /// </summary>
    [Test]
    public void LaunchOnInactiveScope_Should_ThrowInvalidOperationException()
    {
        var scope = new CoroutineScope(_scheduler, "TestScope");

        IEnumerator Coroutine()
        {
            yield break;
        }

        scope.Cancel();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            scope.Launch(Coroutine());
        });

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("not active"));

        scope.Dispose();
    }

    /// <summary>
    /// 测试协程上下文设置 - CoroutineContext 创建
    /// </summary>
    [Test]
    public void CoroutineContext_Should_BeCreatedCorrectly()
    {
        var scope = new CoroutineScope(_scheduler, "TestScope");

        IEnumerator Coroutine()
        {
            yield break;
        }

        var handle = scope.Launch(Coroutine());

        Assert.That(handle.Context, Is.Not.Null);
        Assert.That(handle.Context.Scope, Is.EqualTo(scope));
        Assert.That(handle.Context.Scheduler, Is.EqualTo(_scheduler));
        Assert.That(handle.Context.Owner, Is.EqualTo(scope));

        scope.Dispose();
    }

    /// <summary>
    /// 测试空调度器异常 - scheduler 为 null 抛出异常
    /// </summary>
    [Test]
    public void NullScheduler_Should_ThrowArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var scope = new CoroutineScope(null!, "TestScope");
        });

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ParamName, Is.EqualTo("scheduler"));
    }

    /// <summary>
    /// 测试多协程管理 - 同一作用域启动多个协程
    /// </summary>
    [Test]
    public void MultipleCoroutines_Should_BeManagedCorrectly()
    {
        var scope = new CoroutineScope(_scheduler, "TestScope");
        var executionCount = 0;

        IEnumerator Coroutine()
        {
            executionCount++;
            yield break;
        }

        var handle1 = scope.Launch(Coroutine());
        var handle2 = scope.Launch(Coroutine());
        var handle3 = scope.Launch(Coroutine());

        _scheduler.Update(0.1f);

        Assert.That(executionCount, Is.EqualTo(3));
        Assert.That(handle1.IsDone, Is.True);
        Assert.That(handle2.IsDone, Is.True);
        Assert.That(handle3.IsDone, Is.True);

        scope.Dispose();
    }

    /// <summary>
    /// 测试嵌套作用域 - 多层父子关系
    /// </summary>
    [Test]
    public void NestedScopes_Should_WorkCorrectly()
    {
        var rootScope = new CoroutineScope(_scheduler, "RootScope");
        var level1Scope = new CoroutineScope(_scheduler, "Level1", rootScope);
        var level2Scope = new CoroutineScope(_scheduler, "Level2", level1Scope);

        Assert.That(rootScope.IsActive, Is.True);
        Assert.That(level1Scope.IsActive, Is.True);
        Assert.That(level2Scope.IsActive, Is.True);

        rootScope.Cancel();

        Assert.That(rootScope.IsActive, Is.False);
        Assert.That(level1Scope.IsActive, Is.False);
        Assert.That(level2Scope.IsActive, Is.False);

        level2Scope.Dispose();
        level1Scope.Dispose();
        rootScope.Dispose();
    }
}
