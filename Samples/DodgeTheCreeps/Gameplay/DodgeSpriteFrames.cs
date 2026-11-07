// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Godot;

namespace DodgeTheCreeps.Gameplay;

/// <summary>
/// Builds small native animations from the unmodified official tutorial textures.
/// The nodes own their SpriteFrames; Godot's resource cache owns shared texture loads.
/// </summary>
internal static class DodgeSpriteFrames
{
    /// <summary>Creates the horizontal and vertical player animations.</summary>
    /// <returns>A native resource ready for one player's AnimatedSprite2D.</returns>
    internal static SpriteFrames CreatePlayer()
    {
        var frames = CreateEmpty();
        AddAnimation(frames, "walk", 7,
            "playerGrey_walk1.png", "playerGrey_walk2.png");
        AddAnimation(frames, "up", 7,
            "playerGrey_up1.png", "playerGrey_up2.png");
        return frames;
    }

    /// <summary>Creates one of the tutorial's walking, swimming, or flying creeps.</summary>
    /// <param name="appearance">A value from zero through two selecting the texture pair.</param>
    /// <returns>A native resource ready for one creep's AnimatedSprite2D.</returns>
    internal static SpriteFrames CreateCreep(int appearance)
    {
        var frames = CreateEmpty();
        var prefix = appearance switch
        {
            1 => "enemySwimming",
            2 => "enemyFlyingAlt",
            _ => "enemyWalking"
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
