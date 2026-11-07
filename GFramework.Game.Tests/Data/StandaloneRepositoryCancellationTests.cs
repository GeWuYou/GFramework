// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Core.Abstractions.Serializer;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data;
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
        var repository = new SaveRepository<TestSaveData>(storage.Object, new SaveConfiguration());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

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
        storage.Setup(value => value.DirectoryExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        storage.Setup(value => value.WriteAsync(It.IsAny<string>(), It.IsAny<TestSaveData>()))
            .Returns(() => { entered.TrySetResult(); return release.Task; });
        var repository = new SaveRepository<TestSaveData>(storage.Object, new SaveConfiguration());
        using var cancellation = new CancellationTokenSource();

        var save = repository.SaveAsync(1, new TestSaveData(), cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        Assert.That(save.IsCompleted, Is.False);
        release.TrySetResult();
        await save.WaitAsync(TimeSpan.FromSeconds(5));

        storage.Verify(value => value.WriteAsync(It.IsAny<string>(), It.IsAny<TestSaveData>()), Times.Once);
    }

    /// <summary>A canceled settings mutation waiting behind a write cannot become the committed cache.</summary>
    [Test]
    public async Task UnifiedSettings_WhenCanceledWaitingForCommit_DoesNotMutateCache()
    {
        var entered = NewCompletion();
        var release = NewCompletion();
        var storage = new Mock<IStorage>(MockBehavior.Strict);
        storage.Setup(value => value.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        storage.Setup(value => value.WriteAsync(It.IsAny<string>(), It.IsAny<UnifiedSettingsFile>()))
            .Returns(() => { entered.TrySetResult(); return release.Task; });
        var repository = new UnifiedSettingsDataRepository(storage.Object, new JsonSerializer(),
            new DataRepositoryOptions { AutoBackup = false, EnableEvents = false });
        var location = new TestDataLocation("audio");
        var first = repository.SaveAsync(location, new TestSimpleData { Value = 7 });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource();
        var second = repository.SaveAsync(location, new TestSimpleData { Value = 9 }, cancellation.Token);
        cancellation.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(() => second);
        release.TrySetResult();
        await first.WaitAsync(TimeSpan.FromSeconds(5));

        var loaded = await repository.LoadAsync<TestSimpleData>(location);
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
