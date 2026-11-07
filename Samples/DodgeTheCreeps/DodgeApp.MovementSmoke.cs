// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using DodgeTheCreeps.Gameplay;
using Godot;

namespace DodgeTheCreeps;

/// <summary>Exercises eight-way movement and its actual native presentation without enemy or timer noise.</summary>
public partial class DodgeApp
{
    private async Task RunMovementSmokeAsync()
    {
        var player = new DodgePlayer { Name = "MovementProbe" };
        // Save restoration is allowed before Ready as well as after a node already owns its sprite.
        player.RestoreFacingDirection(2);
        AddChild(player);
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            CheckPlayerFacing(player, 2);
            var origin = new Vector2(240, 360);
            const double step = 0.05;
            var distances = new float[8];
            for (var direction = 0; direction < 8; direction++)
            {
                var heading = Vector2.Right.Rotated(direction * Mathf.Pi / 4);
                player.Position = origin;
                PressMovementDirection(heading);
                player.Advance(step);
                distances[direction] = player.Position.DistanceTo(origin);
                var expected = origin + heading * (float)(330 * step);
                Require(player.Position.DistanceTo(expected) < 0.001f, "Movement heading or speed differs in sector " + direction);
                CheckPlayerFacing(player, direction);
                var sprite = player.GetNode<AnimatedSprite2D>("Sprite");
                Require(sprite.IsPlaying(), "Nonzero movement did not start the facing animation.");
                ReleaseMovementActions();
                var stoppedPosition = player.Position;
                player.Advance(step);
                Require(player.Position == stoppedPosition && !sprite.IsPlaying(), "Idle input moved or animated the player.");
                CheckPlayerFacing(player, direction);
            }
            foreach (var distance in distances)
                Require(Mathf.Abs(distance - distances[0]) < 0.001f, "Diagonal movement is faster than cardinal movement.");
            CheckRestoredFacings(player);
        }
        finally
        {
            ReleaseMovementActions();
            RemoveChild(player);
            player.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        GD.Print("DODGE_MOVEMENT_RESULT PASS");
        FinishSmoke(true);
    }

    private static void PressMovementDirection(Vector2 direction)
    {
        ReleaseMovementActions();
        if (direction.X > 0.1f) Input.ActionPress("dodge_right");
        else if (direction.X < -0.1f) Input.ActionPress("dodge_left");
        if (direction.Y > 0.1f) Input.ActionPress("dodge_down");
        else if (direction.Y < -0.1f) Input.ActionPress("dodge_up");
    }

    private static void ReleaseMovementActions()
    {
        foreach (var action in new[] { "dodge_left", "dodge_right", "dodge_up", "dodge_down" })
            Input.ActionRelease(action);
    }

    private static void CheckPlayerFacing(DodgePlayer player, int direction)
    {
        var names = new[] { "east", "south-east", "south", "south-west", "west", "north-west", "north", "north-east" };
        var sprite = player.GetNode<AnimatedSprite2D>("Sprite");
        Require(player.FacingDirection == direction, "The player did not retain facing sector " + direction);
        Require(string.Equals(sprite.Animation.ToString(), names[direction], StringComparison.Ordinal), "Facing selected the wrong native animation.");
        Require(sprite.FlipH == (direction is 3 or 4 or 5) && sprite.FlipV == (direction == 2), "A previous facing left stale sprite flips.");
        var expectedRotation = direction switch { 1 or 5 => 45, 3 or 7 => -45, _ => 0 };
        Require(Mathf.Abs(sprite.RotationDegrees - expectedRotation) < 0.001f, "A previous facing left stale sprite rotation.");

        // Reconstruct the rendered heading from the original art's orientation and the
        // actual native transforms, rather than accepting a changed animation name alone.
        var rendered = direction is 2 or 6 ? Vector2.Up : Vector2.Right;
        if (sprite.FlipH) rendered.X = -rendered.X;
        if (sprite.FlipV) rendered.Y = -rendered.Y;
        rendered = rendered.Rotated(sprite.Rotation);
        var expected = Vector2.Right.Rotated(direction * Mathf.Pi / 4);
        Require(rendered.DistanceTo(expected) < 0.001f, "Sprite presentation does not point along its movement sector.");
    }

    private static void CheckRestoredFacings(DodgePlayer player)
    {
        foreach (var direction in new[] { 5, 2, 7, 4, 1, 6, 3, 0 })
        {
            player.RestoreFacingDirection(direction);
            CheckPlayerFacing(player, direction);
            Require(!player.GetNode<AnimatedSprite2D>("Sprite").IsPlaying(), "Restoring an idle heading started movement animation.");
        }
        foreach (var invalid in new[] { -1, 8 })
        {
            var rejected = false;
            try { player.RestoreFacingDirection(invalid); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected && player.FacingDirection == 0, "Invalid saved facing was accepted or changed the current pose.");
        }
    }
}
