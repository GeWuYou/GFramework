// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Godot;

namespace DodgeTheCreeps.Gameplay;

/// <summary>
/// Native survival gameplay inspired by Godot's Dodge the Creeps tutorial.
/// Godot drives one pausable physics update; the sample's application owns menus,
/// settings, persistence, and framework navigation outside this node.
/// </summary>
/// <remarks>
/// Attach the world before starting a run. The arena is 480 by 720 logical pixels.
/// Declare dodge_left/right/up/down actions in the project InputMap. Restarting or
/// restoring clears every enemy and grants a short collision-free preparation period.
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
        _player = new DodgePlayer { Name = "Player", Position = new Vector2(240, 580) };
        AddChild(_player);
        _music = new AudioStreamPlayer
        {
            Name = "Music",
            Stream = GD.Load<AudioStream>("res://art/House In a Forest Loop.ogg"),
            VolumeDb = -15
        };
        _gameOverSound = new AudioStreamPlayer
        {
            Name = "GameOverSound",
            Stream = GD.Load<AudioStream>("res://art/gameover.wav"),
            VolumeDb = -9
        };
        AddChild(_music);
        AddChild(_gameOverSound);
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

    /// <summary>Restores explicitly saved progress into a fresh, briefly protected board.</summary>
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
            _spawnRemaining = Math.Max(0.30, 0.68 - Score * 0.003);
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
        DrawRect(new Rect2(0, 0, ArenaWidth, ArenaHeight), new Color(0.035f, 0.065f, 0.12f));
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
            Score++;
            ScoreChanged?.Invoke(this, new ScoreChangedEventArgs(Score));
        }
    }

    // #region tutorial-spawn
    private void SpawnCreep()
    {
        var position = _random.RandiRange(0, 3) switch
        {
            0 => new Vector2(_random.RandfRange(0, ArenaWidth), -70),
            1 => new Vector2(ArenaWidth + 70, _random.RandfRange(68, ArenaHeight)),
            2 => new Vector2(_random.RandfRange(0, ArenaWidth), ArenaHeight + 70),
            _ => new Vector2(-70, _random.RandfRange(68, ArenaHeight))
        };
        var target = new Vector2(_random.RandfRange(90, 390), _random.RandfRange(160, 620));
        var direction = (target - position).Normalized().Rotated(_random.RandfRange(-0.2f, 0.2f));
        var speed = _random.RandfRange(125, 205) + Math.Min(Score * 0.8f, 65);
        var mob = new DodgeMob
        {
            Name = "Creep",
            Position = position,
            Velocity = direction * speed,
            Appearance = _random.RandiRange(0, 2),
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
