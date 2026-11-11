// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Godot.Data;
using Godot;

namespace GodotIntegration.GettingStarted;

/// <summary>Explicit startup component that validates native resources on their owning thread.</summary>
/// <remarks>The main scene retains the resources; this service owns their lookup index.</remarks>
/// <param name="root">The owner of editor-authored resource references.</param>
/// <param name="dispatcher">The owning Godot thread.</param>
internal sealed class QuickStartResources(QuickStartRoot root, IGameDispatcher dispatcher) : IAsyncInitializable
{
    private GodotResourceRepository<string, QuickStartItem>? _items;

    /// <summary>Resolves a required resource after startup validation has completed.</summary>
    /// <param name="key">The authored business key.</param>
    /// <returns>The resource borrowed from the main scene.</returns>
    internal QuickStartItem Get(string key) =>
        (_items ?? throw new InvalidOperationException("Resource startup is incomplete.")).Get(key);

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default) => dispatcher.InvokeAsync(() =>
    {
        ValidateScene<QuickStartPage>(root.PageScene);
        ValidateScene<QuickStartConfirm>(root.ConfirmScene);
        var index = CreateIndex(root.Items);
        _ = index.Get("welcome");
        _items = index;
        return Task.CompletedTask;
    }, cancellationToken);

    /// <summary>Builds a complete index before publishing it to consumers.</summary>
    /// <param name="items">Borrowed, editor-authored resource references.</param>
    /// <returns>A validated resource index.</returns>
    internal static GodotResourceRepository<string, QuickStartItem> CreateIndex(IEnumerable<QuickStartItem> items)
    {
        var index = new GodotResourceRepository<string, QuickStartItem>();
        foreach (var item in items)
        {
            if (!GodotObject.IsInstanceValid(item) || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Title))
                throw new InvalidDataException("Each resource requires an ID and title.");
            index.Add(item.Key, item);
        }
        return index;
    }

    private static void ValidateScene<T>(PackedScene? scene) where T : Control
    {
        if (!GodotObject.IsInstanceValid(scene))
            throw new InvalidDataException($"A {typeof(T).Name} scene is required.");
        var node = scene!.Instantiate();
        try
        {
            if (node is not T)
                throw new InvalidDataException($"The scene root must be {typeof(T).Name}.");
        }
        finally { node.Free(); }
    }
}
