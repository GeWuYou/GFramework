// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pool;
using GFramework.Core.Pool;
using NUnit.Framework;

namespace GFramework.Core.Tests.Pool;

/// <summary>Verifies borrow identity and callback admission without requiring an engine host.</summary>
[TestFixture]
public sealed class PoolOwnershipTests
{
    /// <summary>Rejects an unowned object without invoking its reset or creating a pool bucket.</summary>
    [Test]
    public void Release_ExternalObject_LeavesPoolAndObjectUnchanged()
    {
        var pool = new ProbePool();
        var external = new ProbeItem();

        Assert.Throws<InvalidOperationException>(() => pool.Release("bullet", external));

        Assert.That(external.ReleaseCount, Is.Zero);
        Assert.That(pool.GetStatistics("bullet").TotalReleased, Is.Zero);
        Assert.That(pool.GetPoolSize("bullet"), Is.Zero);
    }

    /// <summary>Borrowing from another pool instance does not grant return admission here.</summary>
    [Test]
    public void Release_OtherPoolObject_PreservesOriginalBorrow()
    {
        var owner = new ProbePool();
        var other = new ProbePool();
        var item = owner.Acquire("bullet");

        Assert.Throws<InvalidOperationException>(() => other.Release("bullet", item));

        Assert.That(item.ReleaseCount, Is.Zero);
        Assert.That(owner.GetActiveCount("bullet"), Is.EqualTo(1));
        owner.Release("bullet", item);
        Assert.That(owner.Acquire("bullet"), Is.SameAs(item));
    }

    /// <summary>Equal values remain independently borrowed reference objects.</summary>
    [Test]
    public void Acquire_EqualObjects_TracksTheirReferenceIdentity()
    {
        var pool = new ProbePool();
        var first = pool.Acquire("bullet");
        var second = pool.Acquire("bullet");

        Assert.That(first.Equals(second), Is.True);
        Assert.That(first, Is.Not.SameAs(second));
        pool.Release("bullet", first);
        pool.Release("bullet", second);

        Assert.That(pool.GetPoolSize("bullet"), Is.EqualTo(2));
        Assert.That(pool.GetActiveCount("bullet"), Is.Zero);
    }

    /// <summary>Clearing idle objects does not lose the original key of an outstanding borrow.</summary>
    [Test]
    public void Clear_OutstandingBorrow_CanReturnOnceToOriginalKey()
    {
        var pool = new ProbePool();
        var item = pool.Acquire("bullet");
        pool.Clear();

        Assert.That(item.DestroyCount, Is.Zero);
        Assert.Throws<InvalidOperationException>(() => pool.Release("enemy", item));
        pool.Release("bullet", item);
        Assert.Throws<InvalidOperationException>(() => pool.Release("bullet", item));

        Assert.That(item.ReleaseCount, Is.EqualTo(1));
        Assert.That(pool.Acquire("bullet"), Is.SameAs(item));
    }

    /// <summary>A callback cannot recursively insert the same object into the free stack.</summary>
    [Test]
    public void Release_ReentrantReturn_IsRejectedBeforeCallback()
    {
        var pool = new ProbePool();
        var item = pool.Acquire("bullet");
        item.OnReleaseAction = () => Assert.Throws<InvalidOperationException>(() => pool.Release("bullet", item));

        pool.Release("bullet", item);

        Assert.That(item.ReleaseCount, Is.EqualTo(1));
        Assert.That(pool.GetPoolSize("bullet"), Is.EqualTo(1));
    }

    /// <summary>A failed reset leaves the borrow with its caller and can be retried.</summary>
    [Test]
    public void Release_CallbackFails_PreservesBorrowAndStatistics()
    {
        var pool = new ProbePool();
        var item = pool.Acquire("bullet");
        item.OnReleaseAction = () => throw new InvalidOperationException("reset failed");

        Assert.Throws<InvalidOperationException>(() => pool.Release("bullet", item));

        Assert.That(pool.GetActiveCount("bullet"), Is.EqualTo(1));
        Assert.That(pool.GetPoolSize("bullet"), Is.Zero);
        Assert.That(pool.GetStatistics("bullet").TotalReleased, Is.Zero);
        item.OnReleaseAction = null;
        pool.Release("bullet", item);
        Assert.That(pool.Acquire("bullet"), Is.SameAs(item));
    }

    /// <summary>Acquisition failure cannot leave an unreachable borrowed object behind.</summary>
    [Test]
    public void Acquire_CallbackFails_RetiresUnreturnedObject()
    {
        var pool = new ProbePool { FailAcquire = true };

        Assert.Throws<InvalidOperationException>(() => pool.Acquire("bullet"));

        var failed = pool.LastCreated!;
        Assert.That(failed.DestroyCount, Is.EqualTo(1));
        Assert.That(pool.GetActiveCount("bullet"), Is.Zero);
        Assert.That(pool.GetPoolSize("bullet"), Is.Zero);
        Assert.Throws<InvalidOperationException>(() => pool.Release("bullet", failed));
        pool.FailAcquire = false;
        Assert.That(pool.Acquire("bullet"), Is.Not.SameAs(failed));
    }

    /// <summary>Both callback errors remain observable when failed acquisition cleanup also fails.</summary>
    [Test]
    public void Acquire_ResetAndCleanupFail_RetainsBothErrorsWithoutBorrow()
    {
        var pool = new ProbePool { FailAcquire = true, FailDestroy = true };

        var error = Assert.Throws<AggregateException>(() => pool.Acquire("bullet"));

        Assert.That(error!.InnerExceptions.Select(static item => item.Message),
            Is.EqualTo(["acquire failed", "destroy failed"]));
        Assert.That(pool.GetActiveCount("bullet"), Is.Zero);
        Assert.That(pool.LastCreated!.DestroyCount, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => pool.Release("bullet", pool.LastCreated!));
    }

    /// <summary>Acquire callbacks cannot return an object whose preparation is still in progress.</summary>
    [Test]
    public void Acquire_ReentrantReturn_CannotPublishAnIdleAlias()
    {
        var pool = new ProbePool();
        pool.OnAcquireAction = item => Assert.Throws<InvalidOperationException>(() => pool.Release("bullet", item));

        var first = pool.Acquire("bullet");
        var second = pool.Acquire("bullet");

        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(pool.GetPoolSize("bullet"), Is.Zero);
        Assert.That(pool.GetActiveCount("bullet"), Is.EqualTo(2));
        Assert.That(first.ReleaseCount, Is.Zero);
    }

    /// <summary>Prewarm retains its existing explicit-count behavior regardless of the return capacity.</summary>
    [Test]
    public void Prewarm_PreservesExplicitCountAndReturnCapacity()
    {
        var pool = new ProbePool();
        pool.SetMaxCapacity("bullet", 2);
        pool.Prewarm("bullet", 5);
        Assert.That(pool.GetPoolSize("bullet"), Is.EqualTo(5));

        var item = pool.Acquire("bullet");
        pool.Release("bullet", item);

        Assert.That(pool.GetPoolSize("bullet"), Is.EqualTo(4));
        Assert.That(item.DestroyCount, Is.EqualTo(1));
    }

    /// <summary>Creates independently owned items with optional acquisition failure.</summary>
    private sealed class ProbePool : AbstractObjectPoolSystem<string, ProbeItem>
    {
        /// <summary>Gets or sets whether newly created items fail their acquire callback.</summary>
        public bool FailAcquire
        {
            get; set;
        }

        /// <summary>Gets or sets whether the final destruction callback should fail.</summary>
        public bool FailDestroy
        {
            get; set;
        }

        /// <summary>Gets or sets an acquire callback that may attempt reentrancy.</summary>
        public Action<ProbeItem>? OnAcquireAction
        {
            get; set;
        }

        /// <summary>Gets the most recently created item, including an item whose acquisition failed.</summary>
        public ProbeItem? LastCreated
        {
            get; private set;
        }

        /// <inheritdoc />
        protected override ProbeItem Create(string key)
        {
            LastCreated = new ProbeItem { FailAcquire = FailAcquire, FailDestroy = FailDestroy, OnAcquireAction = OnAcquireAction };
            return LastCreated;
        }
    }

    /// <summary>Deliberately value-equal objects reveal accidental use of value equality for ownership.</summary>
    private sealed class ProbeItem : IPoolableObject
    {
        /// <summary>Gets or sets whether acquisition should fail.</summary>
        public bool FailAcquire
        {
            get; init;
        }

        /// <summary>Gets whether the final destruction callback should fail.</summary>
        public bool FailDestroy
        {
            get; init;
        }

        /// <summary>Gets the callback invoked while acquisition is still being prepared.</summary>
        public Action<ProbeItem>? OnAcquireAction
        {
            get; init;
        }

        /// <summary>Gets or sets a reset callback that can fail or attempt reentrancy.</summary>
        public Action? OnReleaseAction
        {
            get; set;
        }

        /// <summary>Gets the number of admitted reset callbacks.</summary>
        public int ReleaseCount
        {
            get; private set;
        }

        /// <summary>Gets the number of final destruction callbacks.</summary>
        public int DestroyCount
        {
            get; private set;
        }

        /// <inheritdoc />
        public void OnAcquire()
        {
            if (FailAcquire)
                throw new InvalidOperationException("acquire failed");
            OnAcquireAction?.Invoke(this);
        }

        /// <inheritdoc />
        public void OnRelease()
        {
            ReleaseCount++;
            OnReleaseAction?.Invoke();
        }

        /// <inheritdoc />
        public void OnPoolDestroy()
        {
            DestroyCount++;
            if (FailDestroy)
                throw new InvalidOperationException("destroy failed");
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is ProbeItem;

        /// <inheritdoc />
        public override int GetHashCode() => 0;
    }
}
