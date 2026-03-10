// Copyright (c) 2025 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GFramework.Core.Concurrency;

namespace GFramework.Core.Tests.Concurrency;

[TestFixture]
public sealed class AsyncKeyLockManagerTests
{
    [Test]
    public async Task AcquireLockAsync_Should_ReturnValidHandle()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();

        // Act
        await using var handle = await manager.AcquireLockAsync("test-key");

        // Assert
        Assert.That(handle, Is.Not.Null);
        Assert.That(handle.Key, Is.EqualTo("test-key"));
        Assert.That(handle.AcquiredTicks, Is.GreaterThan(0));
    }

    [Test]
    public async Task AcquireLockAsync_WithSameKey_Should_SerializeAccess()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();
        var executionOrder = new List<int>();
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < 5; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await using var handle = await manager.AcquireLockAsync("same-key");
                executionOrder.Add(index);
                await Task.Delay(10);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.That(executionOrder.Count, Is.EqualTo(5));
        Assert.That(executionOrder.Distinct().Count(), Is.EqualTo(5));
    }

    [Test]
    public async Task AcquireLockAsync_WithDifferentKeys_Should_AllowConcurrentAccess()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < 10; i++)
        {
            var key = $"key-{i}";
            tasks.Add(Task.Run(async () =>
            {
                await using var handle = await manager.AcquireLockAsync(key);
                var current = Interlocked.Increment(ref concurrentCount);
                maxConcurrent = Math.Max(maxConcurrent, current);
                await Task.Delay(50);
                Interlocked.Decrement(ref concurrentCount);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.That(maxConcurrent, Is.GreaterThan(1));
    }

    [Test]
    public async Task Dispose_Should_ReleaseHandle()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();
        var handle = await manager.AcquireLockAsync("test-key");

        // Act
        await handle.DisposeAsync();

        // Assert - 应该能再次获取锁
        await using var handle2 = await manager.AcquireLockAsync("test-key");
        Assert.That(handle2, Is.Not.Null);
    }

    [Test]
    public async Task ConcurrentAcquire_Should_NotThrowException()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < 100; i++)
        {
            var key = $"key-{i % 10}";
            tasks.Add(Task.Run(async () =>
            {
                await using var handle = await manager.AcquireLockAsync(key);
                await Task.Delay(1);
            }));
        }

        // Assert
        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
    }

    [Test]
    public async Task ConcurrentAcquireSameKey_Should_SerializeAccess()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();
        var counter = 0;
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await using var handle = await manager.AcquireLockAsync("same-key");
                var temp = counter;
                await Task.Delay(1);
                counter = temp + 1;
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.That(counter, Is.EqualTo(100));
    }

    [Test]
    public async Task Cleanup_Should_RemoveUnusedLocks()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager(
            cleanupInterval: TimeSpan.FromMilliseconds(100),
            lockTimeout: TimeSpan.FromMilliseconds(200));

        // Act
        await using (var handle = await manager.AcquireLockAsync("temp-key"))
        {
            // 持有锁
        }

        // 等待清理
        await Task.Delay(400);

        var stats = manager.GetStatistics();

        // Assert
        Assert.That(stats.TotalCleaned, Is.GreaterThan(0));
    }

    [Test]
    public async Task Cleanup_Should_NotRemoveActiveLocks()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager(
            cleanupInterval: TimeSpan.FromMilliseconds(100),
            lockTimeout: TimeSpan.FromMilliseconds(200));

        // Act
        await using var handle = await manager.AcquireLockAsync("active-key");

        // 等待清理尝试
        await Task.Delay(400);

        var activeLocks = manager.GetActiveLocks();

        // Assert
        Assert.That(activeLocks.ContainsKey("active-key"), Is.True);
    }

    [Test]
    public async Task GetStatistics_Should_ReturnCorrectCounts()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();

        // Act
        await using (await manager.AcquireLockAsync("key1"))
        {
            await using var handle2 = await manager.AcquireLockAsync("key2");
            var stats = manager.GetStatistics();

            // Assert
            Assert.That(stats.TotalAcquired, Is.EqualTo(2));
            Assert.That(stats.ActiveLockCount, Is.EqualTo(2));
        }

        var finalStats = manager.GetStatistics();
        Assert.That(finalStats.TotalReleased, Is.EqualTo(2));
    }

    [Test]
    public async Task GetActiveLocks_Should_ReturnCurrentLocks()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();

        // Act
        await using var handle1 = await manager.AcquireLockAsync("key1");
        await using var handle2 = await manager.AcquireLockAsync("key2");

        var activeLocks = manager.GetActiveLocks();

        // Assert
        Assert.That(activeLocks.Count, Is.EqualTo(2));
        Assert.That(activeLocks.ContainsKey("key1"), Is.True);
        Assert.That(activeLocks.ContainsKey("key2"), Is.True);
        Assert.That(activeLocks["key1"].ReferenceCount, Is.EqualTo(1));
    }

    [Test]
    public void AcquireLockAsync_AfterDispose_Should_ThrowObjectDisposedException()
    {
        // Arrange
        var manager = new AsyncKeyLockManager();
        manager.Dispose();

        // Act & Assert
        Assert.ThrowsAsync<ObjectDisposedException>(async () => await manager.AcquireLockAsync("test-key"));
    }

    [Test]
    public async Task AcquireLockAsync_WithCancellation_Should_ThrowOperationCanceledException()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();
        using var cts = new CancellationTokenSource();

        // 先获取锁
        await using var handle = await manager.AcquireLockAsync("test-key", cts.Token);

        // Act
        await cts.CancelAsync();

        // Assert
        Assert.CatchAsync<OperationCanceledException>(async () =>
            await manager.AcquireLockAsync("test-key", cts.Token));
    }

    [Test]
    public void AcquireLock_Sync_Should_Work()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();

        // Act
        using var handle = manager.AcquireLock("test-key");

        // Assert
        Assert.That(handle, Is.Not.Null);
        Assert.That(handle.Key, Is.EqualTo("test-key"));
    }

    [Test]
    public async Task CleanupDuringAcquire_Should_NotCauseRaceCondition()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager(
            cleanupInterval: TimeSpan.FromMilliseconds(50),
            lockTimeout: TimeSpan.FromMilliseconds(100));

        var tasks = new List<Task>();

        // Act - 在清理过程中不断获取和释放锁
        for (var i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (var j = 0; j < 10; j++)
                {
                    await using var handle = await manager.AcquireLockAsync($"key-{j % 5}");
                    await Task.Delay(10);
                }
            }));
        }

        // Assert
        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
    }

    [Test]
    public void MultipleDispose_Should_BeSafe()
    {
        // Arrange
        var manager = new AsyncKeyLockManager();

        // Act
        manager.Dispose();
        manager.Dispose();

        // Assert - 不应该抛出异常
        Assert.Pass();
    }

    [Test]
    public async Task HandleDispose_MultipleTimes_Should_BeSafe()
    {
        // Arrange
        using var manager = new AsyncKeyLockManager();
        var handle = await manager.AcquireLockAsync("test-key");

        // Act
        await handle.DisposeAsync();
        await handle.DisposeAsync();
        handle.Dispose();

        // Assert - 不应该抛出异常
        Assert.Pass();
    }
}