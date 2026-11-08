// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DodgeTheCreeps.Advanced.Core.Gameplay;
using DodgeTheCreeps.Advanced.Core.Progress;
using DodgeTheCreeps.Advanced.Core.Settings;
using NUnit.Framework;

namespace DodgeTheCreeps.Advanced.Tests;

/// <summary>Verifies application behavior without a Godot runtime, framework container or filesystem.</summary>
[TestFixture]
public sealed class RunUseCasesTests
{
    /// <summary>Document confirmation must finish before a live board is replaced.</summary>
    [Test]
    public async Task CancelledRecoveryPreservesExistingBoard()
    {
        var trace = new List<string>();
        var board = new Board(trace);
        var store = new Store(trace) { Loaded = null };
        var session = new GameSession();
        using var runs = new RunUseCases(session, board, store);
        Assert.That(await runs.StartAsync(restore: true, contentReady: true, "base:yard").ConfigureAwait(false), Is.False);
        Assert.That(trace, Is.EqualTo(new[] { "load" }));
    }

    /// <summary>Continuation uses the checkpoint arena and restores rather than starting a fresh random board.</summary>
    [Test]
    public async Task RestoreUsesSavedArenaAndLeavesCheckpointClean()
    {
        var trace = new List<string>();
        var saved = Snapshot();
        saved.ArenaId = "moon:crater";
        var board = new Board(trace);
        var session = new GameSession();
        using var runs = new RunUseCases(session, board, new Store(trace) { Loaded = saved });
        Assert.That(await runs.StartAsync(restore: true, contentReady: true, "base:yard").ConfigureAwait(false), Is.True);
        Assert.That(trace, Is.EqualTo(new[] { "load", "prepare:moon:crater", "restore" }));
        Assert.That(session.HasChanges, Is.False);
        Assert.That(session.NeedsDiscardConfirmation(isRunning: true, saved.X, saved.Y), Is.False);
        Assert.That(session.NeedsDiscardConfirmation(isRunning: true, saved.X + 1, saved.Y), Is.True);
    }

    /// <summary>A new run has no saved checkpoint until an explicit save succeeds.</summary>
    [Test]
    public async Task NewRunRequiresDiscardConfirmationUntilSaved()
    {
        var trace = new List<string>();
        var board = new Board(trace);
        var session = new GameSession();
        using var runs = new RunUseCases(session, board, new Store(trace));
        await runs.StartAsync(restore: false, contentReady: true, "base:yard").ConfigureAwait(false);
        Assert.That(trace, Is.EqualTo(new[] { "new-state", "prepare:base:yard", "new-board" }));
        Assert.That(session.NeedsDiscardConfirmation(isRunning: true, 240, 580), Is.True);
        await runs.SaveAsync().ConfigureAwait(false);
        Assert.That(session.NeedsDiscardConfirmation(isRunning: true, 240, 580), Is.False);
    }

    /// <summary>A failed persistent commit never clears dirty state.</summary>
    [Test]
    public async Task SaveFailurePreservesUnsavedChanges()
    {
        var trace = new List<string>();
        var session = new GameSession();
        session.Begin(restored: false);
        using var runs = new RunUseCases(session, new Board(trace), new Store(trace) { SaveFailure = true });
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await runs.SaveAsync().ConfigureAwait(false)).ConfigureAwait(false);
        Assert.That(session.HasChanges, Is.True);
    }

    /// <summary>Death retires the session before deletion so an IO error cannot revive continuation.</summary>
    [Test]
    public async Task DeleteFailureStillRetiresTheEndedRun()
    {
        var trace = new List<string>();
        var session = new GameSession();
        using var runs = new RunUseCases(session, new Board(trace), new Store(trace) { DeleteFailure = true });
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await runs.RetireAsync().ConfigureAwait(false)).ConfigureAwait(false);
        Assert.That(session.IsRetired, Is.True);
        Assert.Throws<InvalidOperationException>(() => session.RequireStartAllowed(contentReady: true, restore: true));
        Assert.DoesNotThrow(() => session.RequireStartAllowed(contentReady: true, restore: false));
    }

    /// <summary>Broken content is rejected before any persistent or native action.</summary>
    [Test]
    public async Task InvalidContentDoesNotTouchStoreOrBoard()
    {
        var trace = new List<string>();
        using var runs = new RunUseCases(new GameSession(), new Board(trace), new Store(trace));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await runs.StartAsync(restore: false, contentReady: false, "base:yard").ConfigureAwait(false)).ConfigureAwait(false);
        Assert.That(trace, Is.Empty);
    }

    /// <summary>A cancelled new-run request cannot replace the active mod state or board.</summary>
    [Test]
    public async Task CancelledStartDoesNotTouchStoreOrBoard()
    {
        var trace = new List<string>();
        using var runs = new RunUseCases(new GameSession(), new Board(trace), new Store(trace));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync(Is.InstanceOf<OperationCanceledException>(), async () =>
            await runs.StartAsync(restore: false, contentReady: true, "base:yard", cancellation.Token).ConfigureAwait(false)).ConfigureAwait(false);
        Assert.That(trace, Is.Empty);
    }

    /// <summary>Domain assembly dependency direction remains enforceable without source-text assertions.</summary>
    [Test]
    public void CoreHasNoEngineOrFrameworkReferences()
    {
        var forbidden = typeof(GameSession).Assembly.GetReferencedAssemblies()
            .Select(static reference => reference.Name ?? string.Empty)
            .Where(static name => name.StartsWith("Godot", StringComparison.Ordinal) || name.StartsWith("GFramework", StringComparison.Ordinal));
        Assert.That(forbidden, Is.Empty);
    }

    /// <summary>Non-finite board values and invalid per-enemy snapshots are rejected.</summary>
    [Test]
    public void SnapshotValidationRejectsInvalidWorldValues()
    {
        var snapshot = Snapshot();
        snapshot.X = float.NaN;
        Assert.Throws<InvalidOperationException>(() => RunRules.ValidateSnapshot(snapshot));
        snapshot = Snapshot();
        snapshot.Enemies.Add(new EnemySnapshot { VelocityX = float.PositiveInfinity });
        Assert.Throws<InvalidOperationException>(() => RunRules.ValidateSnapshot(snapshot));
    }

    /// <summary>Restoring invalid fractional time would change scoring, so it is rejected before engine effects.</summary>
    [TestCase(-0.01)]
    [TestCase(1)]
    [TestCase(double.NaN)]
    public void SnapshotValidationRejectsInvalidScoreFractions(double elapsed)
    {
        var snapshot = Snapshot();
        snapshot.ScoreElapsed = elapsed;
        Assert.Throws<InvalidOperationException>(() => RunRules.ValidateSnapshot(snapshot));
    }

    /// <summary>Preference validation protects the native audio boundary.</summary>
    [TestCase(-0.01f)]
    [TestCase(1.01f)]
    [TestCase(float.NaN)]
    public void SettingsRejectInvalidVolume(float volume) =>
        Assert.Throws<InvalidOperationException>(() => SettingsRules.Validate(new DodgePreferences { Volume = volume }));

    /// <summary>An older save completes before a queued start, so its result cannot clear the new run's changes.</summary>
    [Test]
    public async Task SavingAndStartingAreSerializedAcrossAwait()
    {
        var trace = new List<string>();
        var session = new GameSession();
        session.Begin(restored: false);
        var store = new Store(trace) { SaveCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var runs = new RunUseCases(session, new Board(trace), store);
        var saving = runs.SaveAsync();
        await store.SaveStarted.Task.ConfigureAwait(false);
        var starting = runs.StartAsync(restore: false, contentReady: true, "base:yard");
        Assert.That(starting.IsCompleted, Is.False);
        Assert.That(trace, Does.Not.Contain("new-state"));
        store.SaveCompletion.SetResult();
        await Task.WhenAll(saving, starting).ConfigureAwait(false);
        Assert.That(session.HasChanges, Is.True);
        Assert.That(trace.IndexOf("save"), Is.LessThan(trace.IndexOf("new-state")));
    }

    /// <summary>Changes observed while persistent IO is pending remain dirty after that checkpoint succeeds.</summary>
    [Test]
    public async Task SavingDoesNotEraseChangesObservedDuringWrite()
    {
        var trace = new List<string>();
        var session = new GameSession();
        session.Begin(restored: false);
        var store = new Store(trace) { SaveCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var runs = new RunUseCases(session, new Board(trace), store);
        var saving = runs.SaveAsync();
        await store.SaveStarted.Task.ConfigureAwait(false);
        session.MarkChanged();
        store.SaveCompletion.SetResult();
        await saving.ConfigureAwait(false);
        Assert.That(session.HasChanges, Is.True);
    }

    /// <summary>A save queued for a superseded board cannot silently save a newly started run.</summary>
    [Test]
    public async Task QueuedSaveRejectsAnotherRunGeneration()
    {
        var trace = new List<string>();
        var session = new GameSession();
        session.Begin(restored: false);
        var board = new Board(trace) { PrepareCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var runs = new RunUseCases(session, board, new Store(trace));
        var starting = runs.StartAsync(restore: false, contentReady: true, "base:yard");
        await board.PrepareStarted.Task.ConfigureAwait(false);
        var saving = runs.SaveAsync();
        board.PrepareCompletion.SetResult();
        await starting.ConfigureAwait(false);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await saving.ConfigureAwait(false)).ConfigureAwait(false);
        Assert.That(trace, Does.Not.Contain("save"));
        Assert.That(session.HasChanges, Is.True);
    }

    private static RunSnapshot Snapshot() => new() { RandomState = 42 };

    private sealed class Board(List<string> trace) : IRunBoard
    {
        internal TaskCompletionSource? PrepareCompletion
        {
            get; init;
        }
        internal TaskCompletionSource PrepareStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <inheritdoc />
        public bool IsRunning => true;
        /// <inheritdoc />
        public Task PrepareAsync(string arenaId, CancellationToken cancellationToken)
        {
            trace.Add("prepare:" + arenaId);
            PrepareStarted.TrySetResult();
            return PrepareCompletion?.Task ?? Task.CompletedTask;
        }
        /// <inheritdoc />
        public void StartNew() => trace.Add("new-board");
        /// <inheritdoc />
        public void Restore(RunSnapshot snapshot) => trace.Add("restore");
        /// <inheritdoc />
        public RunSnapshot Capture()
        {
            trace.Add("capture");
            return Snapshot();
        }
        /// <inheritdoc />
        public void Stop() => trace.Add("stop");
        /// <inheritdoc />
        public Task ClearAsync(CancellationToken cancellationToken)
        {
            trace.Add("clear");
            return Task.CompletedTask;
        }
    }

    private sealed class Store(List<string> trace) : IRunStore
    {
        internal TaskCompletionSource? SaveCompletion
        {
            get; init;
        }
        internal TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal RunSnapshot? Loaded
        {
            get; init;
        }
        internal bool SaveFailure
        {
            get; init;
        }
        internal bool DeleteFailure
        {
            get; init;
        }
        /// <inheritdoc />
        public void BeginNewRun() => trace.Add("new-state");
        /// <inheritdoc />
        public Task<RunSnapshot?> LoadAsync(CancellationToken cancellationToken)
        {
            trace.Add("load");
            return Task.FromResult(Loaded);
        }
        /// <inheritdoc />
        public Task SaveAsync(RunSnapshot snapshot, CancellationToken cancellationToken)
        {
            trace.Add("save");
            SaveStarted.TrySetResult();
            return SaveFailure ? Task.FromException(new InvalidOperationException("write failed")) : SaveCompletion?.Task ?? Task.CompletedTask;
        }
        /// <inheritdoc />
        public Task RetireAsync(CancellationToken cancellationToken)
        {
            trace.Add("delete");
            return DeleteFailure ? Task.FromException(new InvalidOperationException("delete failed")) : Task.CompletedTask;
        }
    }
}
