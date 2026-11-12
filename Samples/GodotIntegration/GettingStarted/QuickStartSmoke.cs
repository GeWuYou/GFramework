// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Runtime;
using Godot;

namespace GodotIntegration.GettingStarted;

/// <summary>Bounded native checks for resource validation, typed dialog results and pending-consent shutdown.</summary>
internal static class QuickStartSmoke
{
    /// <summary>Exercises the real scene tree before the host performs its shared close operation.</summary>
    /// <param name="root">The active, isolated example host.</param>
    internal static async Task RunAsync(QuickStartRoot root)
    {
        var item = root.Items[0];
        ExpectRejected(() => QuickStartResources.CreateIndex([item, item]));
        ExpectRejected(() => QuickStartResources.CreateIndex([null!]));
        await CheckFailedStartupAsync(root).ConfigureAwait(true);
        var title = root.FindChild("Title", recursive: true, owned: false) as Label;
        Require(string.Equals(title?.Text, item.Title, StringComparison.Ordinal), "The page did not receive the registered resource.");

        Click(root, "OpenConfirm");
        await WaitAsync(root, () => root.FindChild("Accept", recursive: true, owned: false) is not null).ConfigureAwait(true);
        Click(root, "Accept");
        await WaitAsync(root, () => !root.IsBusy).ConfigureAwait(true);
        Require(string.Equals(root.GetNode<Label>("Status").Text, "Confirmed", StringComparison.Ordinal), "The typed dialog result was lost.");

        Click(root, "OpenConfirm");
        await WaitAsync(root, () => root.FindChild("Cancel", recursive: true, owned: false) is not null).ConfigureAwait(true);
        Click(root, "Cancel");
        await WaitAsync(root, () => !root.IsBusy).ConfigureAwait(true);
        Require(string.Equals(root.GetNode<Label>("Status").Text, "Cancelled", StringComparison.Ordinal), "The dialog dismissal was lost.");

        Click(root, "OpenConfirm");
        await WaitAsync(root, () => root.FindChild("Cancel", recursive: true, owned: false) is not null).ConfigureAwait(true);
        // The host's outer self-check joins shutdown after this method returns, avoiding a startup/stop cycle.
    }

    private static async Task CheckFailedStartupAsync(QuickStartRoot root)
    {
        var invalid = new QuickStartRoot { PageScene = root.PageScene, Items = root.Items };
        var dispatcher = new SynchronizationContextGameDispatcher(SynchronizationContext.Current
            ?? throw new InvalidOperationException("Native checks require the owning context."));
        var architecture = new QuickStartArchitecture(invalid, dispatcher);
        await using var lifetime = architecture.ConfigureAwait(true);
        try
        {
            try
            {
                await architecture.InitializeAsync().ConfigureAwait(true);
                throw new InvalidOperationException("Missing scene reference was accepted.");
            }
            catch (InvalidDataException)
            {
            }
            Require(!architecture.IsReady && invalid.GetChildCount() == 0, "Failed startup retained native children.");
        }
        finally
        {
            invalid.Free();
        }
    }

    private static void ExpectRejected(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            return;
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new InvalidOperationException("Invalid resource registration was accepted.");
    }

    private static void Click(QuickStartRoot root, string name)
    {
        var button = root.FindChild(name, recursive: true, owned: false) as Button
            ?? throw new InvalidOperationException("Missing native button: " + name);
        button.EmitSignal(BaseButton.SignalName.Pressed);
    }

    private static async Task WaitAsync(QuickStartRoot root, Func<bool> condition)
    {
        for (var frame = 0; frame < 300 && !condition(); frame++)
            await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(condition(), "Native action did not complete.");
        await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
