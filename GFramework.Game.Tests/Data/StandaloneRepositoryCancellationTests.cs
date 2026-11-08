// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Core.Abstractions.Serializer;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Storage;
using GFramework.Game.Data;
using GFramework.Game.Serializer;

namespace GFramework.Game.Tests.Data;

/// <summary>Checks standalone repository cancellation without abandoning accepted persistence work.</summary>
[TestFixture]
public sealed class StandaloneRepositoryCancellationTests
{
    /// <summary>A canceled save cannot create directories or touch existing data.</summary>
    [Test]
    public async Task SaveAsync_WhenAlreadyCanceled_DoesNotTouchStorage()
    {
        var storage = new Mock<IStorage>(MockBehavior.Strict);
        _ = storage.As<IStorageBackup>();
        var repository = new SaveRepository<TestSaveData>(storage.Object, new SaveConfiguration(),
            new SaveRepositoryOptions<TestSaveData> { GameId = "test", CurrentVersion = 1 });
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.CatchAsync<OperationCanceledException>(() => repository.SaveAsync(1, new TestSaveData(), cancellation.Token));

        storage.VerifyNoOtherCalls();
    }

    /// <summary>Once an atomic write has started, cancellation cannot publish completion ahead of the write.</summary>
    [Test]
    public async Task SaveAsync_WhenCanceledDuringAcceptedWrite_JoinsWrite()
    {
        var entered = NewCompletion();
        var release = NewCompletion();
        var storage = new Mock<IStorage>(MockBehavior.Strict);
        storage.Setup(value => value.ExistsAsync(It.IsAny<string>())).ReturnsAsync(value: false);
        var snapshots = storage.As<IStorageBackup>();
        snapshots.Setup(value => value.WriteSnapshotAsync(It.IsAny<string>(), It.IsAny<StorageSnapshot>(), It.IsAny<CancellationToken>()))
            .Returns(() => { entered.TrySetResult(); return release.Task; });
        var repository = new SaveRepository<TestSaveData>(storage.Object, new SaveConfiguration(),
            new SaveRepositoryOptions<TestSaveData> { GameId = "test", CurrentVersion = 1 });
        using var cancellation = new CancellationTokenSource();

        var save = repository.SaveAsync(1, new TestSaveData(), cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None);
        await cancellation.CancelAsync();
        Assert.That(save.IsCompleted, Is.False);
        release.TrySetResult();
        await save.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None);

        snapshots.Verify(value => value.WriteSnapshotAsync(It.IsAny<string>(), It.IsAny<StorageSnapshot>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>A canceled settings mutation waiting behind a write cannot become the committed cache.</summary>
    [Test]
    public async Task UnifiedSettings_WhenCanceledWaitingForCommit_DoesNotMutateCache()
    {
        var entered = NewCompletion();
        var release = NewCompletion();
        var storage = new Mock<IStorage>(MockBehavior.Strict);
        storage.Setup(value => value.ExistsAsync(It.IsAny<string>())).ReturnsAsync(value: false);
        storage.Setup(value => value.WriteAsync(It.IsAny<string>(), It.IsAny<UnifiedSettingsFile>()))
            .Returns(() => { entered.TrySetResult(); return release.Task; });
        var repository = new UnifiedSettingsDataRepository(storage.Object, new JsonSerializer(),
            new DataRepositoryOptions { AutoBackup = false, EnableEvents = false });
        var location = new TestDataLocation("audio");
        var first = repository.SaveAsync(location, new TestSimpleData { Value = 7 });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource();
        var second = repository.SaveAsync(location, new TestSimpleData { Value = 9 }, cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.CatchAsync<OperationCanceledException>(() => second);
        release.TrySetResult();
        await first.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None);

        var loaded = await repository.LoadAsync<TestSimpleData>(location, cancellationToken: global::System.Threading.CancellationToken.None);
        Assert.That(loaded.Value, Is.EqualTo(7));
        storage.Verify(value => value.WriteAsync(It.IsAny<string>(), It.IsAny<UnifiedSettingsFile>()), Times.Once);
    }

    /// <summary>Null dependencies fail during composition rather than on an unrelated later operation.</summary>
    [Test]
    public void Constructors_RejectMissingDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new DataRepository(null!));
        Assert.Throws<ArgumentNullException>(() => new UnifiedSettingsDataRepository(null!, new JsonSerializer()));
        Assert.Throws<ArgumentNullException>(() => new UnifiedSettingsDataRepository(Mock.Of<IStorage>(), null!));
    }

    /// <summary>Uses asynchronous continuations so deterministic gates do not depend on inline completion.</summary>
    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
