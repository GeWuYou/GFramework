// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Godot;

namespace DodgeTheCreeps.Gameplay;

/// <summary>
/// Native player presentation and movement. DodgeWorld advances it on the physics
/// thread, so gameplay movement and hit detection share the same frame and pause boundary.
/// </summary>
internal sealed partial class DodgePlayer : Node2D
{
    private AnimatedSprite2D _sprite = null!;
    private bool _invulnerable;
    private double _animationTime;

    /// <summary>Gets the forgiving collision radius used by the sample's circle hit test.</summary>
    internal const float HitRadius = 17;

    /// <summary>Builds animations after the player has entered the native scene tree.</summary>
    public override void _Ready()
    {
        ZIndex = 10;
        _sprite = new AnimatedSprite2D
        {
            Name = "Sprite",
            SpriteFrames = DodgeSpriteFrames.CreatePlayer(),
            Animation = "walk",
            Scale = new Vector2(0.42f, 0.42f)
        };
        AddChild(_sprite);
    }

    /// <summary>Moves from the declared gameplay actions and updates the matching animation.</summary>
    /// <param name="delta">Elapsed unpaused physics-frame time.</param>
    internal void Advance(double delta)
    {
        _animationTime += delta;
        var direction = Input.GetVector("dodge_left", "dodge_right", "dodge_up", "dodge_down");
        Position = (Position + direction * (float)(330 * delta)).Clamp(
            new Vector2(26, 82), new Vector2(454, 686));

        if (direction.LengthSquared() <= 0)
        {
            _sprite.Stop();
        }
        else if (Mathf.Abs(direction.X) >= Mathf.Abs(direction.Y))
        {
            _sprite.Play("walk");
            _sprite.FlipH = direction.X < 0;
            _sprite.FlipV = false;
        }
        else
        {
            _sprite.Play("up");
            _sprite.FlipH = false;
            _sprite.FlipV = direction.Y > 0;
        }

        _sprite.Modulate = new Color(1, 1, 1,
            _invulnerable ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin((float)_animationTime * 8)) : 1);
    }

    /// <summary>Sets the restart grace indicator without changing native process settings.</summary>
    /// <param name="enabled">Whether the world currently ignores collisions.</param>
    internal void SetInvulnerable(bool enabled)
    {
        if (_invulnerable == enabled)
            return;
        _invulnerable = enabled;
        QueueRedraw();
    }

    /// <summary>Stops animation while leaving the final player pose visible.</summary>
    internal void StopMoving()
    {
        _sprite.Stop();
        _sprite.Modulate = Colors.White;
    }

    /// <summary>Draws a subtle player marker and an explicit restart-grace ring.</summary>
    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 21, new Color(0.12f, 0.88f, 0.82f, 0.16f));
        if (_invulnerable)
            DrawArc(Vector2.Zero, 28, 0, Mathf.Tau, 40, new Color(0.3f, 0.95f, 0.86f, 0.65f), 2, true);
    }
}
