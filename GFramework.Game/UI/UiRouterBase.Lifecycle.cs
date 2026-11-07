// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;

namespace GFramework.Game.UI;

/// <summary>Owns best-effort cleanup of UI pages, floating layers and pause requests.</summary>
public abstract partial class UiRouterBase
{
    /// <summary>Releases stack, floating and retained pages once, even if one callback fails.</summary>
    /// <returns>The completed cleanup task, or all cleanup errors.</returns>
    protected override Task OnStopAsync()
    {
        var pages = Stack.Concat(_layers.Values.SelectMany(layer => layer.Values))
            .Concat(_retainedPages).Distinct(ReferenceEqualityComparer.Instance).Cast<IUiPageBehavior>().ToArray();
        Stack.Clear();
        _layers.Clear();
        _retainedPages.Clear();
        var errors = new List<Exception>();
        foreach (var page in pages)
            CleanupPage(page, errors);

        // A failed callback may have left a pause token without an attached page.
        foreach (var page in _pauseTokens.Keys.ToArray())
            AttemptCleanup(() => ReleasePauseRequest(page), errors);
        if (errors.Count != 0)
            throw new AggregateException("One or more UI cleanup operations failed.", errors);
        return Task.CompletedTask;
    }

    /// <summary>Checks all stack guards before clearing any page.</summary>
    private async Task<bool> CanLeavePagesAsync()
    {
        foreach (var page in Stack)
        {
            if (!await ExecuteLeaveGuardsAsync(page.Key).ConfigureAwait(true))
                return false;
        }
        return true;
    }

    /// <summary>Attempts exit, root removal and pause release independently.</summary>
    private void CleanupPage(IUiPageBehavior page, List<Exception> errors)
    {
        _retainedPages.Remove(page);
        AttemptCleanup(page.OnExit, errors);
        AttemptCleanup(() => _uiRoot.RemoveUiPage(page), errors);
        AttemptCleanup(() => ReleasePauseRequest(page), errors);
    }

    /// <summary>Records errors while allowing cleanup of remaining pages and pause requests.</summary>
    private static void AttemptCleanup(Action cleanup, List<Exception> errors)
    {
        try
        {
            cleanup();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
    }

}
