// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DodgeTheCreeps.Gameplay;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Data;
using Godot;
using PortableSerializer = GFramework.Game.Serializer.JsonSerializer;

namespace DodgeTheCreeps;

/// <summary>Checks exact board persistence and future RNG continuation in native and freshly restarted processes.</summary>
public partial class DodgeApp
{
    /// <summary>Uses the existing isolated smoke namespace; no normal player save or settings are touched.</summary>
    private async Task RunPersistenceSmokeAsync(bool continuation)
    {
        using var paused = _pause.PauseScope("Complete-board persistence acceptance");
        foreach (var action in new[] { "dodge_left", "dodge_right", "dodge_up", "dodge_down" }) Input.ActionRelease(action);
        if (continuation)
        {
            var expected = await _storage.ReadAsync<DodgeProgress>(_dataRoot + "/persistence-expected.json").ConfigureAwait(true);
            await StartGameAsync(true).ConfigureAwait(true);
            Require(_world is { IsRunning: true }, "Fresh-process continue did not create a live board.");
            RequireSameProgress(expected, _world!.CaptureProgress(), "Fresh-process board");
            CheckFutureSpawnContinuation(expected);
        }
        else
        {
            await StartGameAsync(false).ConfigureAwait(true);
            var expected = CreatePersistenceFixture(_world!.CaptureProgress());
            _world.RestoreProgress(expected);
            Require(_world.EnemyCount >= 2, "The persistence fixture must contain a nonempty enemy board.");
            RequireSameProgress(expected, _world.CaptureProgress(), "Prepared board");
            await SaveGameAsync().ConfigureAwait(true);
            var saved = await _saves.LoadAsync(1, _lifetime.Token).ConfigureAwait(true);
            RequireSameProgress(expected, saved, "Persisted file");
            _world.StartRun(seed: 77);
            Require(_world.EnemyCount == 0, "A new run did not clear the old board.");
            _world.RestoreProgress(saved);
            RequireSameProgress(expected, _world.CaptureProgress(), "Restored board");
            CheckInvalidSnapshotsLeaveBoardIntact(expected);
            CheckFutureSpawnContinuation(expected);
            await CheckSparseLegacyImportAsync().ConfigureAwait(true);
            await CheckMissingEnemyRecoveryAsync(expected).ConfigureAwait(true);
            _world.RestoreProgress(expected);
            await SaveGameAsync().ConfigureAwait(true);
            await _storage.WriteAsync(_dataRoot + "/persistence-expected.json", expected).ConfigureAwait(true);
            GD.Print("DODGE_PERSISTENCE_NEXT " + _smokeId);
        }
        GD.Print("DODGE_PERSISTENCE_RESULT PASS " + (continuation ? "continue" : "seed"));
        FinishSmoke(true);
    }

    /// <summary>Produces a deterministic nonempty board with fractional timing and already-consumed random draws.</summary>
    private static DodgeProgress CreatePersistenceFixture(DodgeProgress template)
    {
        using var random = new RandomNumberGenerator { Seed = 0x5A9ED };
        _ = random.Randi();
        _ = random.Randf();
        template.Score = 37;
        template.X = 140;
        template.Y = 500;
        template.PlayerFacing = 7;
        template.ScoreElapsed = 0.375;
        template.SpawnRemaining = 0.1;
        template.GraceRemaining = 0.75;
        template.RandomSeed = random.Seed;
        template.RandomState = random.State;
        template.Enemies = new List<DodgeMobProgress>()
        {
            new DodgeMobProgress
            {
                EnemyId = "base:walker", X = 60, Y = 130, VelocityX = 80, VelocityY = 45,
                Appearance = 0, AnimationFrame = 1, AnimationProgress = 0.25f
            },
            new DodgeMobProgress
            {
                EnemyId = "base:swimmer", X = 400, Y = 210, VelocityX = -95, VelocityY = 30,
                Appearance = 1, AnimationFrame = 0, AnimationProgress = 0.75f
            }
        };
        return template;
    }

    /// <summary>The next spawn after restoring consumes exactly the same random sequence and timers.</summary>
    private void CheckFutureSpawnContinuation(DodgeProgress snapshot)
    {
        _world!.RestoreProgress(snapshot);
        _world._PhysicsProcess(0.125);
        var expected = _world.CaptureProgress();
        Require(expected.Enemies.Count == snapshot.Enemies.Count + 1, "The RNG regression did not cross a spawn boundary.");
        _world.RestoreProgress(snapshot);
        _world._PhysicsProcess(0.125);
        RequireSameProgress(expected, _world.CaptureProgress(), "Future spawn continuation");
        _world.RestoreProgress(snapshot);
    }

    /// <summary>Rejects malformed DTOs and unresolved definitions before replacing any current native enemy.</summary>
    private void CheckInvalidSnapshotsLeaveBoardIntact(DodgeProgress snapshot)
    {
        foreach (var mutate in new Action<DodgeProgress>[]
        {
            static value => value.Version = DodgeProgress.CurrentVersion + 1,
            static value => value.PlayerFacing = 8,
            static value => value.RandomState = null,
            static value => value.Enemies[0].X = float.NaN,
            static value => value.Enemies[1].EnemyId = "base:missing-enemy",
            static value => value.Enemies = Enumerable.Repeat(value.Enemies[0], DodgeProgress.MaxSavedEnemies + 1).ToList()
        })
        {
            var invalid = CloneProgress(snapshot);
            mutate(invalid);
            var rejected = false;
            try { _world!.RestoreProgress(invalid); }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or System.Collections.Generic.KeyNotFoundException)
            {
                rejected = true;
            }
            Require(rejected, "Invalid complete-board state was accepted.");
            RequireSameProgress(snapshot, _world!.CaptureProgress(), "Board after rejected restore");
        }
    }

    /// <summary>Verifies that a known v1 save is explicitly imported with backup and honestly marked sparse.</summary>
    private async Task CheckSparseLegacyImportAsync()
    {
        const int slot = 88;
        var key = _dataRoot + "/saves/slot_88/save.json";
        await _storage.WriteAsync(key, new { Version = 1, Score = 21, X = 140f, Y = 500f }).ConfigureAwait(true);
        var original = await _storage.ReadSnapshotAsync(key, _lifetime.Token).ConfigureAwait(true);
        var report = await _saves.InspectContentAsync(slot, _lifetime.Token).ConfigureAwait(true);
        Require(report.IsLegacy && report.RequiresRecovery, "Sparse legacy import was silently accepted as an exact board.");
        var imported = await _saves.ImportLegacyAsync(slot, report, _lifetime.Token).ConfigureAwait(true);
        Require(imported.Data.Version == 2 && imported.Data.LegacyLayoutReset && imported.Data.Enemies.Count == 0 &&
            imported.Data.Score == 21 && imported.Data.GraceRemaining == 1.5 && imported.Data.RandomState is not null,
            "The v1 migration did not explicitly initialize its missing board state.");
        var backup = await _storage.ReadSnapshotAsync(_dataRoot + "/saves/slot_88/" + imported.BackupKey, _lifetime.Token).ConfigureAwait(true);
        Require(string.Equals(original.Fingerprint, backup.Fingerprint, StringComparison.Ordinal), "Legacy import backup changed original bytes.");
    }

    /// <summary>Game-owned recovery removes only unavailable enemy definitions and retains valid enemy state.</summary>
    private async Task CheckMissingEnemyRecoveryAsync(DodgeProgress expected)
    {
        const int slot = 89;
        var missing = CloneProgress(expected);
        missing.Content = new ContentSnapshot(expected.Content!.Packs.Concat(
            new[] { new ContentPackStamp("removed", "1.0.0", "previously-installed-mod") }).ToArray());
        missing.Enemies.Add(new DodgeMobProgress
        {
            EnemyId = "removed:creep", X = 100, Y = 110, VelocityX = 25, VelocityY = 10, Appearance = 0
        });
        var key = _dataRoot + "/saves/slot_89/save.json";
        await _storage.WriteAsync(key, missing).ConfigureAwait(true);
        var original = await _storage.ReadSnapshotAsync(key, _lifetime.Token).ConfigureAwait(true);
        var report = await _saves.InspectContentAsync(slot, _lifetime.Token).ConfigureAwait(true);
        Require(report.RequiresRecovery && report.Issues.Any(static issue => issue.Kind == ContentSaveIssueKind.MissingContent),
            "A removed enemy mod did not produce an explicit content recovery requirement.");
        var repaired = await _saves.RecoverContentAsync(slot, report, _lifetime.Token).ConfigureAwait(true);
        RequireSameProgress(expected, repaired.Data, "Missing-enemy recovery");
        var backup = await _storage.ReadSnapshotAsync(_dataRoot + "/saves/slot_89/" + repaired.BackupKey, _lifetime.Token).ConfigureAwait(true);
        Require(string.Equals(original.Fingerprint, backup.Fingerprint, StringComparison.Ordinal), "Enemy recovery backup changed original bytes.");
    }

    /// <summary>Clones only serialized values so invalid-state checks never modify the expected snapshot.</summary>
    private static DodgeProgress CloneProgress(DodgeProgress progress)
    {
        var serializer = new PortableSerializer();
        return serializer.Deserialize<DodgeProgress>(serializer.Serialize(progress));
    }

    /// <summary>Compares all gameplay-affecting fields and enemy presentation, excluding only wall-clock metadata.</summary>
    private static void RequireSameProgress(DodgeProgress expected, DodgeProgress actual, string subject)
    {
        Require(expected.Version == actual.Version && expected.Score == actual.Score && expected.X == actual.X && expected.Y == actual.Y &&
            string.Equals(expected.ArenaId, actual.ArenaId, StringComparison.Ordinal) && expected.PlayerFacing == actual.PlayerFacing &&
            expected.ScoreElapsed == actual.ScoreElapsed && expected.SpawnRemaining == actual.SpawnRemaining &&
            expected.GraceRemaining == actual.GraceRemaining && expected.RandomSeed == actual.RandomSeed &&
            expected.RandomState == actual.RandomState && expected.Enemies.Count == actual.Enemies.Count,
            subject + " did not preserve score, pose, timers, RNG and enemy count.");
        for (var index = 0; index < expected.Enemies.Count; index++)
        {
            var left = expected.Enemies[index];
            var right = actual.Enemies[index];
            Require(string.Equals(left.EnemyId, right.EnemyId, StringComparison.Ordinal) && left.X == right.X && left.Y == right.Y &&
                left.VelocityX == right.VelocityX && left.VelocityY == right.VelocityY && left.Appearance == right.Appearance &&
                left.AnimationFrame == right.AnimationFrame && Math.Abs(left.AnimationProgress - right.AnimationProgress) < 0.00001f,
                subject + " changed enemy " + index + ".");
        }
    }
}
