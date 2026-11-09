// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data.Events;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Game.Tests.Data;

/// <summary>Checks explicit batch admission and observation isolation without hiding durable-write failures.</summary>
[TestFixture]
public sealed class RepositoryNotificationBoundaryTests
{
    /// <summary>An unknown later runtime type is rejected before any accepted earlier item can write.</summary>
    [Test]
    public async Task UnknownBatchTypeFailsBeforeTheFirstStorageOperation()
    {
        var storage = new Mock<IStorage>(MockBehavior.Strict);
        var repository = new DataRepository(storage.Object).RegisterDataType<NumberData>();
        var first = Mock.Of<IDataLocation>(location => location.Key == "first");
        var second = Mock.Of<IDataLocation>(location => location.Key == "second");
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SaveAllAsync(
            [(first, new NumberData()), (second, new TextData())]));
        storage.VerifyNoOtherCalls();
    }

    /// <summary>Empty batches need no type registrations and malformed batches never reach storage.</summary>
    [Test]
    public async Task BatchAdmissionHandlesEmptyNullAndCanceledInputs()
    {
        var storage = new Mock<IStorage>(MockBehavior.Strict);
        var repository = new DataRepository(storage.Object, new DataRepositoryOptions { EnableEvents = false })
            .RegisterDataType<NumberData>().RegisterDataType<NumberData>();
        await repository.SaveAllAsync([]);
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.SaveAllAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.SaveAllAsync(
            [(Mock.Of<IDataLocation>(), null!)]));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.SaveAllAsync([(null!, new NumberData())]));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.CatchAsync<OperationCanceledException>(() => repository.SaveAllAsync([], cancellation.Token));
        storage.VerifyNoOtherCalls();
    }

    /// <summary>Both storage layouts keep successful operations successful and deliver later direct notifications.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task CompletedWritesAndReadsSurvivePublisherFailures(bool unified)
    {
        var root = Path.Combine(Path.GetTempPath(), "gframework-notification-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var storage = new FileStorage(root, new JsonSerializer(), ".json");
            var events = new List<object>();
            Action<object> publisher = _ => throw new InvalidOperationException("observer");
            publisher += events.Add;
            var options = new DataRepositoryOptions { AutoBackup = true };
            IDataRepository repository = unified
                ? new UnifiedSettingsDataRepository(storage, new JsonSerializer(), options,
                    publishEvent: publisher, observerLogger: NullLogger.Instance)
                : new DataRepository(storage, options, publisher, NullLogger.Instance)
                    .RegisterDataType<NumberData>().RegisterDataType<TextData>();
            var first = Mock.Of<IDataLocation>(location => location.Key == "number");
            var second = Mock.Of<IDataLocation>(location => location.Key == "text");
            await repository.SaveAsync(first, new NumberData { Value = 1 });
            await repository.SaveAllAsync([(first, new NumberData { Value = 2 }), (second, new TextData { Value = "two" })]);
            Assert.That((await repository.LoadAsync<NumberData>(first)).Value, Is.EqualTo(2));
            Assert.That((await repository.LoadAsync<TextData>(second)).Value, Is.EqualTo("two"));
            await repository.DeleteAsync(second);
            Assert.That(await repository.ExistsAsync(second), Is.False);
            Assert.That(events.OfType<DataSavedEvent<NumberData>>().Count(), Is.EqualTo(1));
            Assert.That(events.OfType<DataBatchSavedEvent>().Count(), Is.EqualTo(1));
            Assert.That(events.OfType<DataDeletedEvent>().Count(), Is.EqualTo(1));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Real storage errors remain the operation result rather than becoming notification successes.</summary>
    [Test]
    public async Task SingleWriteNeedsNoRegistrationAndPreservesStorageFailure()
    {
        var storage = new Mock<IStorage>();
        var failure = new IOException("storage failed");
        storage.Setup(value => value.WriteAsync(It.IsAny<string>(), It.IsAny<NumberData>()))
            .ThrowsAsync(failure);
        var notifications = 0;
        var repository = new DataRepository(storage.Object, publishEvent: _ => notifications++);
        var error = await Assert.ThrowsAsync<IOException>(() =>
            repository.SaveAsync(Mock.Of<IDataLocation>(location => location.Key == "number"), new NumberData()));
        Assert.That(error, Is.SameAs(failure));
        Assert.That(notifications, Is.Zero);
    }

    /// <summary>A concrete numeric persistence type.</summary>
    public sealed class NumberData : IData
    {
        /// <summary>The serialized value.</summary>
        public int Value
        {
            get; set;
        }
    }
    /// <summary>A distinct concrete text persistence type.</summary>
    public sealed class TextData : IData
    {
        /// <summary>The serialized value.</summary>
        public string Value { get; set; } = "";
    }
}
