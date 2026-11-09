// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Core.Events;
using Godot;

namespace GFramework.Godot.Extensions;

/// <summary>Explicitly attach a subscription lifetime to a Godot node's current tree membership.</summary>
public static class SubscriptionExtensions
{
    /// <summary>Dispose a token on tree exit, or detach early through the returned token.</summary>
    /// <param name="subscription">Owned subscription.</param>
    /// <param name="node">Node whose current tree membership bounds the subscription.</param>
    /// <returns>A token that detaches both the node hook and the owned subscription once.</returns>
    public static IDisposable DisposeWhenNodeExits(this IDisposable subscription, Node node)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(node);
        Subscription? lifetime = null;
        void Detach() => lifetime!.Dispose();
        lifetime = new Subscription(() =>
        {
            node.TreeExiting -= Detach;
            subscription.Dispose();
        });
        node.TreeExiting += Detach;
        return lifetime;
    }
}
