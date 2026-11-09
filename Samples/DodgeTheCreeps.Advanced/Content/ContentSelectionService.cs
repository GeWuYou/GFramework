// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.UI;

using DodgeTheCreeps.Advanced.Composition;

namespace DodgeTheCreeps.Advanced.Content;

/// <summary>Coordinates content selection and explicit executable-code approval without owning game flow or saves.</summary>
internal sealed class ContentSelectionService
{
    private Content.DodgeContent _content;
    private readonly IUiService _ui;
    private readonly DodgeLocalization Text;
    private readonly CancellationToken _cancellationToken;

    /// <summary>Creates a bounded content-dialog controller.</summary>
    internal ContentSelectionService(Content.DodgeContent content, IUiService ui, DodgeLocalization text, GameLifetime lifetime)
    {
        _content = content;
        _ui = ui;
        Text = text;
        _cancellationToken = lifetime.Token;
    }

    /// <summary>Switches an explicitly injected content instance for isolated native fixture ownership.</summary>
    internal void SetContent(Content.DodgeContent content) => _content = content;

    /// <summary>Opens installed-content choices on the Godot thread without changing the active content session.</summary>
    /// <returns>Completion of content-view activation; application shutdown cancels the request.</returns>
    internal Task OpenContentAsync() => _ui.ShowAsync(DodgeRoutes.Content, args: true, _cancellationToken);

    /// <summary>Requires exact installed-code approval before recording an enabled next-start choice.</summary>
    internal async Task ToggleContentAsync(string id)
    {
        var pack = _content.Packages.SingleOrDefault(item => string.Equals(item.Manifest.Id, id, StringComparison.Ordinal));
        if (pack?.Manifest.Code is not null && !_content.RequestedProfile.EnabledPackages.Contains(id, StringComparer.Ordinal))
        {
            var stamp = new ContentPackStamp(id, pack.Manifest.Version, pack.Fingerprint);
            if (!_content.IsCodeApproved(stamp))
            {
                var decision = await _ui.ShowDialogAsync(DodgeRoutes.CodePermission,
                    () => Text.Get("code_execution_question", ("name", Text.PackageName(id, pack.Manifest.Name)), ("version", pack.Manifest.Version)),
                    _cancellationToken).ConfigureAwait(true);
                if (!decision.Accepted || !decision.Value)
                    return;
                await _content.ApproveCodeAsync(id, _cancellationToken).ConfigureAwait(true);
            }
        }
        await _content.ToggleAsync(id, _cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Lets an already enabled but updated package obtain fresh exact-byte approval for the next start.</summary>
    internal async Task ReapproveCodeAsync(string id)
    {
        var pack = _content.Packages.Single(item => string.Equals(item.Manifest.Id, id, StringComparison.Ordinal));
        var decision = await _ui.ShowDialogAsync(DodgeRoutes.CodePermission,
            () => Text.Get("code_execution_question", ("name", Text.PackageName(id, pack.Manifest.Name)), ("version", pack.Manifest.Version)),
            _cancellationToken).ConfigureAwait(true);
        if (decision.Accepted && decision.Value)
            await _content.ApproveCodeAsync(id, _cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Records ordering without mutating the active content session.</summary>
    internal Task MoveContentAsync(string id, int direction) => _content.MoveAsync(id, direction, _cancellationToken);
    /// <summary>Explicitly replaces broken selection preferences with base-only choices.</summary>
    internal Task ResetContentAsync() => _content.ResetAsync(_cancellationToken);
    /// <summary>Selects an already loaded arena.</summary>
    internal Task CycleArenaAsync() => _content.CycleArenaAsync(_cancellationToken);

}
