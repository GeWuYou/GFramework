// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Storage;
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

namespace GFramework.Game.Tests.Data;

/// <summary>Verifies that slot admission includes existence checks as well as snapshot reads.</summary>
[TestFixture]
public sealed class SaveRepositoryConcurrencyTests
{
    /// <summary>A delete racing an admitted load waits until the existing snapshot has been read.</summary>
    [Test]
    public async Task LoadAndDelete_KeepExistenceCheckInsideSlotAdmission()
    {
        var root = Path.Combine(Path.GetTempPath(), "gframework-save-race-" + Guid.NewGuid().ToString("N"));
        using var backing = new FileStorage(root, new JsonSerializer());
        var configuration = new SaveConfiguration();
        var options = new SaveRepositoryOptions<TestSaveData> { GameId = "race", CurrentVersion = 1 };
        await new SaveRepository<TestSaveData>(backing, configuration, options).SaveAsync(1, new TestSaveData { Name = "hero" });
        var checkedExists = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCheck = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var storage = new Mock<IStorage>();
        storage.Setup(value => value.ExistsAsync(It.IsAny<string>())).Returns(async (string key) =>
        {
            var exists = await backing.ExistsAsync(key).ConfigureAwait(false);
            checkedExists.TrySetResult();
            await releaseCheck.Task.ConfigureAwait(false);
            return exists;
        });
        storage.Setup(value => value.DeleteAsync(It.IsAny<string>()))
            .Returns((string key) => backing.DeleteAsync(key));
        storage.As<IStorageBackup>().Setup(value => value.ReadSnapshotAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string key, CancellationToken token) => backing.ReadSnapshotAsync(key, token));
        var repository = new SaveRepository<TestSaveData>(storage.Object, configuration, options);
        var load = repository.LoadAsync(1);
        try
        {
            await checkedExists.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var deletion = repository.DeleteAsync(1);
            Assert.That(deletion.IsCompleted, Is.False, "Deletion entered while load still owned its existence check.");
            releaseCheck.TrySetResult();
            Assert.That((await load.WaitAsync(TimeSpan.FromSeconds(5))).Name, Is.EqualTo("hero"));
            await deletion.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That((await repository.LoadAsync(1)).Name, Is.Empty);
        }
        finally
        {
            releaseCheck.TrySetResult();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
