// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     验证额外宿主生命周期由统一启动入口绑定，不依赖某个引擎的包装入口。
/// </summary>
[TestFixture]
public sealed class CoroutineCancellationSourceTests
{
    /// <summary>
    ///     验证独立作用域直接接收带生命周期的枚举器时会自动绑定取消。
    /// </summary>
    [Test]
    public void Scope_Run_Should_Bind_Enumerator_Lifetime()
    {
        using var scheduler = new CoroutineScheduler(new FakeTimeSource());
        using var scope = scheduler.CreateScope(CoroutineFailurePolicy.Supervisor);
        using var lifetime = new CancellationTokenSource();
        var iterator = new BoundEnumerator(lifetime.Token);
        var handle = scope.Run(iterator);
        Assert.That(iterator.MoveNextCount, Is.EqualTo(1));

        lifetime.Cancel();
        scheduler.Update();

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(iterator.DisposeCount, Is.EqualTo(1));
        Assert.That(scope.ActiveCount, Is.Zero);
    }

    /// <summary>
    ///     验证工厂返回的枚举器同样在第一步执行前取得生命周期绑定。
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void Already_Cancelled_Lifetime_Should_Prevent_First_Step(bool fromFactory)
    {
        using var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var iterator = new BoundEnumerator(new CancellationToken(true));
        var handle = fromFactory ? scheduler.Run(_ => iterator) : scheduler.Run(iterator);

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(iterator.MoveNextCount, Is.Zero);
        Assert.That(iterator.DisposeCount, Is.EqualTo(1));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>
    ///     验证 RunChild 接收的宿主生命周期只取消子任务，不误终止仍可继续的父流程。
    /// </summary>
    [Test]
    public void RunChild_Should_Bind_Lifetime_Without_Cancelling_Parent()
    {
        using var scheduler = new CoroutineScheduler(new FakeTimeSource());
        using var lifetime = new CancellationTokenSource();
        var iterator = new BoundEnumerator(lifetime.Token);
        CoroutineHandle child = default;

        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            child = context.RunChild(iterator);
            yield return new Delay(100);
        }

        var parent = scheduler.Run(Parent);
        lifetime.Cancel();
        scheduler.Update();

        Assert.That(scheduler.TryGetResult(child, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(scheduler.IsCoroutineAlive(parent), Is.True);
        Assert.That(iterator.DisposeCount, Is.EqualTo(1));
        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证附加生命周期绑定后，原先传入的外部取消令牌仍然有效。
    /// </summary>
    [Test]
    public void Enumerator_Lifetime_Should_Not_Replace_Explicit_Cancellation()
    {
        using var scheduler = new CoroutineScheduler(new FakeTimeSource());
        using var lifetime = new CancellationTokenSource();
        using var external = new CancellationTokenSource();
        var iterator = new BoundEnumerator(lifetime.Token);
        var handle = scheduler.Run(iterator, cancellationToken: external.Token);

        external.Cancel();
        scheduler.Update();

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(lifetime.IsCancellationRequested, Is.False);
        Assert.That(iterator.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证令牌读取失败仍释放已经取得所有权的枚举器，取消异常也不能掩盖接入故障。
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void Lifetime_Getter_Failure_Should_Fault_And_Dispose_Without_Starting(bool cancellationException)
    {
        using var scheduler = new CoroutineScheduler(new FakeTimeSource());
        Exception failure = cancellationException
            ? new OperationCanceledException("lifetime getter failed")
            : new InvalidOperationException("lifetime getter failed");
        var iterator = new BoundEnumerator(default, failure);
        var handle = scheduler.Run(iterator);

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.SameAs(failure));
        Assert.That(iterator.MoveNextCount, Is.Zero);
        Assert.That(iterator.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>提供独立生命周期并记录启动与释放次数的纯 Core 探针。</summary>
    private sealed class BoundEnumerator(CancellationToken token, Exception? tokenFailure = null)
        : IEnumerator<IYieldInstruction>, ICoroutineCancellationSource
    {
        /// <summary>获取执行次数。</summary>
        public int MoveNextCount { get; private set; }

        /// <summary>获取释放次数。</summary>
        public int DisposeCount { get; private set; }

        /// <inheritdoc />
        public CancellationToken CancellationToken => tokenFailure == null ? token : throw tokenFailure;

        /// <inheritdoc />
        public IYieldInstruction Current => new WaitOneFrame();

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            MoveNextCount++;
            return true;
        }

        /// <inheritdoc />
        public void Reset() => throw new NotSupportedException();

        /// <inheritdoc />
        public void Dispose() => DisposeCount++;
    }
}
