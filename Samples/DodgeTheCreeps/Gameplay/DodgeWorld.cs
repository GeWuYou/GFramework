// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using DodgeTheCreeps.Content;
using DodgeTheCreeps.Modding;
using GFramework.Game.Abstractions.Content;
using Godot;

namespace DodgeTheCreeps.Gameplay;

/// <summary>
/// Native survival gameplay inspired by Godot's Dodge the Creeps tutorial.
/// Godot drives one pausable physics update; the sample's application owns menus,
/// settings, persistence, and framework navigation outside this node.
/// </summary>
/// <remarks>
/// Attach the world before starting a run. The arena is 480 by 720 logical pixels.
/// Declare dodge_left/right/up/down actions in the project InputMap. Fresh runs and explicit
/// sparse-v1 migration start an empty protected board. Complete v2 restoration preserves every
/// enemy, fractional timer and RNG state and grants no additional collision grace.
/// </remarks>
public partial class DodgeWorld : Node2D
{
    private const float ArenaWidth = 480;
    private const float ArenaHeight = 720;
    private readonly List<DodgeMob> _mobs = new();
    private readonly RandomNumberGenerator _random = new();
    private DodgePlayer _player = null!;
    private AudioStreamPlayer _music = null!;
    private AudioStreamPlayer _gameOverSound = null!;
    private double _scoreElapsed;
    private double _spawnRemaining;
    private double _graceRemaining;
    private bool _ready;
    private int _hostThreadId;
    private DodgeContent? _content;
    private DodgeArenaDefinition? _arena;
    private Func<int, int>? _modifyScore;

    /// <summary>Assigns the game's ordered API modifiers before gameplay begins.</summary>
    /// <param name="modifier">Applies registrations without retaining native nodes in external mods.</param>
    internal void ConfigureScoring(Func<int, int> modifier) => _modifyScore = modifier;

    /// <summary>Assigns one immutable content session before this world enters the scene tree.</summary>
    internal void ConfigureContent(DodgeContent content, DodgeArenaDefinition arena)
    {
        if (_ready) throw new InvalidOperationException("Configure the arena before attaching the world.");
        _content = content;
        _arena = arena;
    }

    /// <summary>Raised on the Godot thread when the displayed survival score changes.</summary>
    public event EventHandler<ScoreChangedEventArgs>? ScoreChanged;

    /// <summary>Raised once on the Godot thread after an active run ends from a collision.</summary>
    public event EventHandler? RunEnded;

    /// <summary>Gets the number of unpaused seconds survived in the current run.</summary>
    public int Score { get; private set; }

    /// <summary>Gets whether movement, spawning, scoring, and collision checks are enabled.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Gets the player position in logical arena pixels for an explicit save operation.</summary>
    public Vector2 PlayerPosition => _ready ? _player.Position : new Vector2(240, 580);

    /// <summary>Gets the number of live enemies currently owned by this world.</summary>
    public int EnemyCount => _mobs.Count;

    /// <summary>Builds native children without starting gameplay before the application chooses a flow.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Pausable;
        _hostThreadId = System.Environment.CurrentManagedThreadId;
        _player = new DodgePlayer { Name = "Player", Position = new Vector2(240, 580) };
        AddChild(_player);
        _music = new AudioStreamPlayer
        {
            Name = "Music",
            Bus = "BGM",
            Stream = _content?.Music(_arena?.Music ?? string.Empty) ?? GD.Load<AudioStream>("res://art/House In a Forest Loop.ogg"),
            VolumeDb = -15
        };
        _gameOverSound = new AudioStreamPlayer
        {
            Name = "GameOverSound",
            Bus = "SFX",
            Stream = GD.Load<AudioStream>("res://art/gameover.wav"),
            VolumeDb = -9
        };
        AddChild(_music);
        AddChild(_gameOverSound);
        var decoration = _content?.CreateOfficialScene(_arena?.OfficialScene ?? string.Empty);
        if (decoration is not null) AddChild(decoration);
        _music.Finished += OnMusicFinished;
        _ready = true;
        QueueRedraw();
    }

    /// <summary>Starts a fresh run after the world has entered the tree.</summary>
    /// <param name="seed">A nonzero value selects reproducible spawns; zero randomizes the run.</param>
    public void StartRun(int seed = 0) => BeginRun(0, new Vector2(240, 580), seed);

    /// <summary>Stops a run without reporting a loss; used when leaving gameplay or shutting down.</summary>
    public void StopRun()
    {
        IsRunning = false;
        if (!_ready)
            return;
        _music.Stop();
        _gameOverSound.Stop();
        _player.SetInvulnerable(false);
        _player.StopMoving();
        foreach (var mob in _mobs)
            mob.StopMoving();
    }

    /// <summary>Creates an explicitly sparse board for legacy examples; complete saves use RestoreProgress.</summary>
    /// <param name="score">The saved nonnegative survival score.</param>
    /// <param name="position">The saved finite logical player position, clamped inside the arena.</param>
    /// <exception cref="ArgumentOutOfRangeException">The score is negative or the position is not finite.</exception>
    public void RestoreRun(int score, Vector2 position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(score);
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
            throw new ArgumentOutOfRangeException(nameof(position), "Player position must be finite.");
        BeginRun(score, position, seed: 0);
    }

    /// <summary>Captures the current live board on the Godot thread; pausing does not change the captured state.</summary>
    /// <returns>A complete portable v2 snapshot with stable content references and independent enemy DTOs.</returns>
    public DodgeProgress CaptureProgress()
    {
        RequireHostThread();
        if (!IsRunning) throw new InvalidOperationException("Only a live run can be saved.");
        var save = new DodgeProgress
        {
            Score = Score, X = _player.Position.X, Y = _player.Position.Y,
            ArenaId = _arena?.Id ?? "base:yard", PlayerFacing = _player.FacingDirection,
            ScoreElapsed = _scoreElapsed, SpawnRemaining = _spawnRemaining, GraceRemaining = _graceRemaining,
            RandomSeed = _random.Seed, RandomState = _random.State
        };
        foreach (var enemy in _mobs) save.Enemies.Add(enemy.CaptureProgress());
        DodgeProgress.ValidateSnapshot(save);
        return save;
    }

    /// <summary>
    /// Restores a complete board without rerolling enemies or granting grace. Portable state, definition references
    /// and all native enemy presentation are prepared before stopping or removing any existing board object.
    /// </summary>
    /// <param name="save">A fully migrated and content-validated v2 snapshot.</param>
    public void RestoreProgress(DodgeProgress save)
    {
        RequireHostThread();
        DodgeProgress.ValidateSnapshot(save);
        if (!string.Equals(save.ArenaId, _arena?.Id ?? "base:yard", StringComparison.Ordinal))
            throw new InvalidOperationException("Prepare the saved arena before restoring its board.");
        var prepared = PrepareSavedEnemies(save.Enemies);
        StopRun();
        ClearCreeps();
        // Godot requires restoring Seed before a previously captured State, since setting Seed resets State.
        _random.Seed = save.RandomSeed;
        _random.State = save.RandomState!.Value;
        Score = save.Score;
        _scoreElapsed = save.ScoreElapsed;
        _spawnRemaining = save.SpawnRemaining;
        _graceRemaining = save.GraceRemaining;
        _player.Position = new Vector2(save.X, save.Y);
        _player.RestoreFacingDirection(save.PlayerFacing);
        _player.SetInvulnerable(_graceRemaining > 0);
        _player.Show();
        foreach (var enemy in prepared)
        {
            AddChild(enemy.Node);
            enemy.Node.RestorePresentation(enemy.Progress);
            _mobs.Add(enemy.Node);
        }
        IsRunning = true;
        _music.Play();
        ScoreChanged?.Invoke(this, new ScoreChangedEventArgs(Score));
    }

    /// <summary>Creates only detached, validated native nodes so preparation failure leaves the current board intact.</summary>
    private List<(DodgeMob Node, DodgeMobProgress Progress)> PrepareSavedEnemies(IList<DodgeMobProgress> enemies)
    {
        var prepared = new List<(DodgeMob, DodgeMobProgress)>(enemies.Count);
        try
        {
            foreach (var progress in enemies)
            {
                DodgeEnemyDefinition? definition = null;
                if (_content is not null)
                    definition = _content.Catalog.GetTable<DodgeEnemyDefinition>("enemies").Get(new ContentId(progress.EnemyId));
                var mob = new DodgeMob
                {
                    Name = "Creep", EnemyId = progress.EnemyId, Position = new Vector2(progress.X, progress.Y),
                    Velocity = new Vector2(progress.VelocityX, progress.VelocityY), Appearance = progress.Appearance,
                    Texture = _content?.Texture(definition?.Texture ?? string.Empty), ZIndex = 2
                };
                prepared.Add((mob, progress));
                mob.PreparePresentation();
                mob.ValidatePresentation(progress);
            }
            return prepared;
        }
        catch
        {
            foreach (var enemy in prepared) enemy.Item1.Free();
            throw;
        }
    }

    /// <summary>Rejects capture and restore away from the native world's owning thread or before Ready.</summary>
    private void RequireHostThread()
    {
        if (!_ready || System.Environment.CurrentManagedThreadId != _hostThreadId)
            throw new InvalidOperationException("Capture and restore require a ready world on its Godot thread.");
    }

    // #region tutorial-loop
    /// <summary>Advances native movement, enemy ownership, scoring, and collision detection in one frame.</summary>
    /// <param name="delta">Elapsed unpaused physics-frame time.</param>
    public override void _PhysicsProcess(double delta)
    {
        if (!IsRunning)
            return;

        _graceRemaining = Math.Max(0, _graceRemaining - delta);
        _player.SetInvulnerable(_graceRemaining > 0);
        _player.Advance(delta);
        AdvanceScore(delta);
        if (!IsRunning)
            return;

        _spawnRemaining -= delta;
        if (_spawnRemaining <= 0)
        {
            SpawnCreep();
            _spawnRemaining = Math.Max(0.30, (_arena?.SpawnInterval ?? 0.68) - Score * 0.003);
        }

        AdvanceCreeps(delta);
    }
    // #endregion tutorial-loop

    /// <summary>Removes the music subscription when Godot removes this world.</summary>
    public override void _ExitTree()
    {
        IsRunning = false;
        if (_ready)
        {
            StopRun();
            _music.Finished -= OnMusicFinished;
            _music.Stream = null;
            _gameOverSound.Stream = null;
        }
        _mobs.Clear();
        _random.Dispose();
        _ready = false;
    }

    /// <summary>Draws the arena beneath the original tutorial character and enemy art.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, ArenaWidth, ArenaHeight), _arena is null ? new Color(0.035f, 0.065f, 0.12f) : new Color(_arena.Background));
        DrawRect(new Rect2(12, 68, 456, 640), new Color(0.055f, 0.105f, 0.16f));
        var grid = new Color(0.18f, 0.36f, 0.44f, 0.14f);
        for (var x = 28; x < ArenaWidth; x += 32)
        {
            for (var y = 84; y < ArenaHeight - 16; y += 32)
                DrawCircle(new Vector2(x, y), 1, grid);
        }

        DrawRect(new Rect2(12, 68, 456, 640), new Color(0.2f, 0.55f, 0.62f, 0.25f), false, 1);
    }

    private void BeginRun(int score, Vector2 position, int seed)
    {
        if (!_ready)
            throw new InvalidOperationException("Attach DodgeWorld to the scene tree before starting a run.");

        StopRun();
        ClearCreeps();
        if (seed == 0)
            _random.Randomize();
        else
            _random.Seed = unchecked((ulong)(uint)seed);

        Score = score;
        _scoreElapsed = 0;
        _spawnRemaining = 1.5;
        _graceRemaining = 1.5;
        _player.Position = position.Clamp(new Vector2(26, 82), new Vector2(454, 686));
        _player.RestoreFacingDirection(0);
        _player.SetInvulnerable(true);
        _player.Show();
        IsRunning = true;
        _music.Play();
        ScoreChanged?.Invoke(this, new ScoreChangedEventArgs(Score));
    }

    private void AdvanceScore(double delta)
    {
        _scoreElapsed += delta;
        while (_scoreElapsed >= 1 && IsRunning)
        {
            _scoreElapsed -= 1;
            var increment = _modifyScore?.Invoke(DodgeScoreRules.Calculate()) ?? DodgeScoreRules.Calculate();
            if (increment < 1 || increment > 1000) throw new InvalidOperationException("A score mod returned an invalid increment.");
            Score = checked(Score + increment);
            ScoreChanged?.Invoke(this, new ScoreChangedEventArgs(Score));
        }
    }

    // #region tutorial-spawn
    private void SpawnCreep()
    {
        // Keep the live board within the same bounded allocation contract as durable snapshots.
        if (_mobs.Count >= DodgeProgress.MaxSavedEnemies) return;
        var position = _random.RandiRange(0, 3) switch
        {
            0 => new Vector2(_random.RandfRange(0, ArenaWidth), -70),
            1 => new Vector2(ArenaWidth + 70, _random.RandfRange(68, ArenaHeight)),
            2 => new Vector2(_random.RandfRange(0, ArenaWidth), ArenaHeight + 70),
            _ => new Vector2(-70, _random.RandfRange(68, ArenaHeight))
        };
        var target = new Vector2(_random.RandfRange(90, 390), _random.RandfRange(160, 620));
        var direction = (target - position).Normalized().Rotated(_random.RandfRange(-0.2f, 0.2f));
        DodgeEnemyDefinition? definition = null;
        if (_content is not null && _arena is not null)
        {
            var id = _arena.Enemies[_random.RandiRange(0, _arena.Enemies.Length - 1)];
            definition = _content.Catalog.GetTable<DodgeEnemyDefinition>("enemies").Get(new ContentId(id));
        }
        var speed = _random.RandfRange(definition?.SpeedMin ?? 125, definition?.SpeedMax ?? 205) + Math.Min(Score * 0.8f, 65);
        var mob = new DodgeMob
        {
            Name = "Creep",
            EnemyId = definition?.Id ?? "base:walker",
            Position = position,
            Velocity = direction * speed,
            Appearance = definition?.Appearance ?? _random.RandiRange(0, 2),
            Texture = _content?.Texture(definition?.Texture ?? string.Empty),
            ZIndex = 2
        };
        AddChild(mob);
        _mobs.Add(mob);
    }
    // #endregion tutorial-spawn

    // #region tutorial-collision
    private void AdvanceCreeps(double delta)
    {
        var hitDistance = DodgePlayer.HitRadius + DodgeMob.HitRadius;
        for (var i = _mobs.Count - 1; i >= 0; i--)
        {
            var mob = _mobs[i];
            mob.Advance(delta);
            if (mob.Position.X < -125 || mob.Position.X > ArenaWidth + 125 ||
                mob.Position.Y < -125 || mob.Position.Y > ArenaHeight + 125)
            {
                _mobs.RemoveAt(i);
                mob.QueueFree();
                continue;
            }

            if (_graceRemaining <= 0 &&
                mob.Position.DistanceSquaredTo(_player.Position) < hitDistance * hitDistance)
            {
                EndRun();
                return;
            }
        }
    }
    // #endregion tutorial-collision

    private void EndRun()
    {
        if (!IsRunning)
            return;
        StopRun();
        _gameOverSound.Play();
        RunEnded?.Invoke(this, EventArgs.Empty);
    }

    private void ClearCreeps()
    {
        // Detach synchronously so a same-frame restart cannot see the old enemies
        // through tree traversal while QueueFree waits for the frame to finish.
        foreach (var mob in _mobs)
        {
            RemoveChild(mob);
            mob.QueueFree();
        }
        _mobs.Clear();
    }

    private void OnMusicFinished()
    {
        if (IsRunning)
            _music.Play();
    }
}
