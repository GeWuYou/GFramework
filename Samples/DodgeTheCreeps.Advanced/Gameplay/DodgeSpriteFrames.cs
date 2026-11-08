// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using Godot;

namespace DodgeTheCreeps.Advanced.Gameplay;

/// <summary>
/// Builds small native animations from the unmodified official tutorial textures.
/// The nodes own their SpriteFrames; Godot's resource cache owns shared texture loads.
/// </summary>
internal static class DodgeSpriteFrames
{
    /// <summary>Creates the official horizontal and upward animation pairs, flipped for the opposite directions.</summary>
    /// <returns>A native resource ready for one player's AnimatedSprite2D.</returns>
    internal static SpriteFrames CreatePlayer()
    {
        var frames = CreateEmpty();
        AddAnimation(frames, "walk", 7, "playerGrey_walk1.png", "playerGrey_walk2.png");
        AddAnimation(frames, "up", 7, "playerGrey_up1.png", "playerGrey_up2.png");
        return frames;
    }

    /// <summary>Maps persisted clockwise facing sectors to stable native animation names.</summary>
    /// <param name="direction">A sector from zero (east) through seven (north-east).</param>
    /// <returns>The animation populated by <see cref="CreatePlayer"/>.</returns>
    internal static string PlayerAnimationName(int direction) => direction switch
    {
        2 or 6 => "up",
        0 or 1 or 3 or 4 or 5 or 7 => "walk",
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown player facing sector."),
    };

    /// <summary>Creates one of the tutorial's walking, swimming, or flying creeps.</summary>
    /// <param name="appearance">A value from zero through two selecting the texture pair.</param>
    /// <param name="texture">An optional session-owned data-mod image, decoded without script loading.</param>
    /// <returns>A native resource ready for one creep's AnimatedSprite2D.</returns>
    internal static SpriteFrames CreateCreep(int appearance, Texture2D? texture = null)
    {
        var frames = CreateEmpty();
        if (texture is not null)
        {
            frames.AddAnimation("move");
            frames.AddFrame("move", texture);
            return frames;
        }
        var prefix = appearance switch
        {
            1 => "enemySwimming",
            2 => "enemyFlyingAlt",
            _ => "enemyWalking",
        };
        AddAnimation(frames, "move", 6, prefix + "_1.png", prefix + "_2.png");
        return frames;
    }

    private static SpriteFrames CreateEmpty()
    {
        var frames = new SpriteFrames();
        frames.RemoveAnimation("default");
        return frames;
    }

    private static void AddAnimation(SpriteFrames frames, string name, double speed, params string[] files)
    {
        frames.AddAnimation(name);
        frames.SetAnimationLoopMode(name, SpriteFrames.LoopMode.Linear);
        frames.SetAnimationSpeed(name, speed);
        foreach (var file in files)
            frames.AddFrame(name, GD.Load<Texture2D>("res://art/" + file));
    }
}
