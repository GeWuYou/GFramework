// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using Godot;

namespace DodgeTheCreeps.Gameplay;

/// <summary>
/// One native enemy. The world owns movement, removal, and collision checks;
/// this node owns only its original tutorial animation and appearance.
/// </summary>
internal sealed partial class DodgeMob : Node2D
{
    private AnimatedSprite2D _sprite = null!;

    /// <summary>Gets the forgiving circular hit radius used for every enemy appearance.</summary>
    internal const float HitRadius = 19;

    /// <summary>Gets the persistent definition identity selected by the spawning content table.</summary>
    internal string EnemyId { get; init; } = "base:walker";

    /// <summary>Gets the velocity selected once by the spawning world.</summary>
    internal Vector2 Velocity
    {
        get; init;
    }

    /// <summary>Gets the texture-pair selection, assigned before entering the tree.</summary>
    internal int Appearance
    {
        get; init;
    }
    /// <summary>Gets an optional safely decoded texture owned by the active content session.</summary>
    internal Texture2D? Texture
    {
        get; init;
    }

    /// <summary>Builds the enemy animation and makes native smoke inspection possible through a stable group.</summary>
    public override void _Ready()
    {
        AddToGroup("dodge_creeps");
        PreparePresentation();
        _sprite.Play("move");
    }

    /// <summary>Prepares native media before a restore commits changes to the existing board.</summary>
    internal void PreparePresentation()
    {
        if (_sprite is not null)
            return;
        _sprite = new AnimatedSprite2D
        {
            Name = "Sprite",
            SpriteFrames = DodgeSpriteFrames.CreateCreep(Appearance, Texture),
            Animation = "move",
            Scale = new Vector2(Appearance == 2 ? 0.35f : 0.46f, Appearance == 2 ? 0.35f : 0.46f),
            Modulate = Appearance switch
            {
                1 => new Color(0.95f, 0.75f, 1),
                2 => new Color(1, 0.85f, 0.65f),
                _ => Colors.White,
            },
        };
        Rotation = Velocity.Angle();
        AddChild(_sprite);
    }

    /// <summary>Captures only portable movement and presentation values, never a native object reference.</summary>
    /// <returns>The enemy's exact state on the host thread.</returns>
    internal DodgeMobProgress CaptureProgress() => new()
    {
        EnemyId = EnemyId,
        X = Position.X,
        Y = Position.Y,
        VelocityX = Velocity.X,
        VelocityY = Velocity.Y,
        Appearance = Appearance,
        AnimationFrame = _sprite.Frame,
        AnimationProgress = _sprite.FrameProgress,
    };

    /// <summary>Rejects a saved frame that the current compatible definition cannot render.</summary>
    /// <param name="progress">Already structurally validated portable state.</param>
    internal void ValidatePresentation(DodgeMobProgress progress)
    {
        if (progress.AnimationFrame >= _sprite.SpriteFrames.GetFrameCount("move"))
            throw new InvalidOperationException("The saved enemy animation no longer exists in the current content.");
    }

    /// <summary>Restores the saved frame after Ready starts the sprite, without advancing the board.</summary>
    /// <param name="progress">Already prepared saved presentation state.</param>
    internal void RestorePresentation(DodgeMobProgress progress) =>
        _sprite.SetFrameAndProgress(progress.AnimationFrame, progress.AnimationProgress);

    /// <summary>Moves exactly once during the world's active physics frame.</summary>
    /// <param name="delta">Elapsed unpaused physics-frame time.</param>
    internal void Advance(double delta) => Position += Velocity * (float)delta;

    /// <summary>Stops the sprite when the run ends, keeping the final board visible.</summary>
    internal void StopMoving() => _sprite.Stop();

    /// <summary>Draws an understated shadow that separates the art from the field.</summary>
    public override void _Draw() =>
        DrawCircle(Vector2.Zero, 22, new Color(0, 0, 0, 0.15f));
}
