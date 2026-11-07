// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
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

    /// <summary>Gets the last nonzero input heading, clockwise from east in eight screen-space sectors.</summary>
    internal int FacingDirection { get; private set; }

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
            Animation = DodgeSpriteFrames.PlayerAnimationName(FacingDirection),
            Scale = new Vector2(0.42f, 0.42f)
        };
        AddChild(_sprite);
        ApplyFacing();
    }

    // #region tutorial-movement
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
        else
        {
            // Input.GetVector limits the vector length to one, preserving equal maximum
            // speed on cardinal and diagonal input while still allowing analog strengths.
            FacingDirection = (Mathf.RoundToInt(direction.Angle() / (Mathf.Pi / 4)) + 8) % 8;
            ApplyFacing();
            _sprite.Play();
        }

        _sprite.Modulate = new Color(1, 1, 1,
            _invulnerable ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin((float)_animationTime * 8)) : 1);
    }
    // #endregion tutorial-movement

    /// <summary>Restores a stable idle heading without synthesizing input or moving the player.</summary>
    /// <param name="direction">Clockwise sector from zero (east) through seven (north-east).</param>
    /// <remarks>May be called before the node enters the tree; the same pose is applied during Ready.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The direction is outside the eight supported sectors.</exception>
    internal void RestoreFacingDirection(int direction)
    {
        if (direction is < 0 or > 7)
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Facing must be a sector from zero through seven.");
        FacingDirection = direction;
        if (_sprite is null) return;
        ApplyFacing();
        _sprite.Stop();
    }

    private void ApplyFacing()
    {
        // Preserve eight-way movement/save headings while presenting the official
        // horizontal/upward art without rotating the character on diagonal input.
        _sprite.Animation = DodgeSpriteFrames.PlayerAnimationName(FacingDirection);
        _sprite.FlipH = FacingDirection is 3 or 4 or 5;
        _sprite.FlipV = FacingDirection == 2;
        _sprite.Rotation = 0;
    }

    /// <summary>Controls the sprite's protection blink without drawing collision or status overlays.</summary>
    /// <param name="enabled">Whether the world currently ignores collisions.</param>
    internal void SetInvulnerable(bool enabled)
    {
        if (_invulnerable == enabled)
            return;
        _invulnerable = enabled;
    }

    /// <summary>Stops animation while leaving the final player pose visible.</summary>
    internal void StopMoving()
    {
        _sprite.Stop();
        _sprite.Modulate = Colors.White;
    }

}
