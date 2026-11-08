// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Globalization;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;
using Godot;

namespace GodotIntegration;

/// <summary>
/// Minimal gameplay: move toward a circle and collect it. Godot owns frame processing;
/// the framework owns the node-bound heartbeat coroutine and Scene behavior.
/// </summary>
public partial class DemoWorld : Node2D
{
    private Vector2 _target = new(700, 320);

    /// <summary>Gets or sets the instance-specific composition root before entering the tree.</summary>
    internal DemoRoot Host { get; init; } = null!;

    /// <summary>Gets the number of unpaused seconds observed by the framework coroutine.</summary>
    internal int ActiveSeconds
    {
        get; private set;
    }

    /// <summary>Starts a coroutine that terminates automatically when this scene leaves the tree.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Pausable;
        Timing.RunOwnedCoroutine(this, CountSeconds(), tag: "sample-world-heartbeat");
    }

    /// <summary>Updates movement and the save DTO without reading input while a modal owns gameplay actions.</summary>
    /// <param name="delta">Unpaused frame duration.</param>
    public override void _Process(double delta)
    {
        var position = new Vector2(Host.Save.X, Host.Save.Y);
        var direction = Input.GetVector("demo_left", "demo_right", "demo_up", "demo_down");
        position += direction * (float)(Host.Gameplay.Speed * delta);
        position = position.Clamp(new Vector2(30, 100), new Vector2(930, 610));
        Host.Save.X = position.X;
        Host.Save.Y = position.Y;
        if (position.DistanceTo(_target) < 30)
        {
            Host.Save.Collected++;
            _target = new Vector2((float)GD.RandRange(50, 910), (float)GD.RandRange(120, 590));
        }

        QueueRedraw();
        Host.SetHud(string.Create(CultureInfo.InvariantCulture, $"Circles: {Host.Save.Collected}    Active seconds: {ActiveSeconds}    WASD to move"));
    }

    /// <summary>Draws simple geometry so the example needs no external assets.</summary>
    public override void _Draw()
    {
        if (Host.Preferences.ShowGrid)
        {
            var grid = new Color(0.12f, 0.18f, 0.25f);
            for (var x = 0; x < 960; x += 40)
                DrawLine(new Vector2(x, 90), new Vector2(x, 640), grid);
            for (var y = 120; y < 640; y += 40)
                DrawLine(new Vector2(0, y), new Vector2(960, y), grid);
        }

        DrawCircle(_target, 14, new Color(1, 0.72f, 0.25f));
        DrawCircle(new Vector2(Host.Save.X, Host.Save.Y), 18, new Color(0.3f, 0.8f, 0.9f));
    }

    /// <summary>Uses the ordinary Process segment, so the heartbeat pauses with the game.</summary>
    private IEnumerator<IYieldInstruction> CountSeconds()
    {
        while (true)
        {
            yield return new Delay(1);
            ActiveSeconds++;
        }
    }
}
