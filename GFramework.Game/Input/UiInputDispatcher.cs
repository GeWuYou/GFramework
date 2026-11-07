// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Input;
using GFramework.Game.Abstractions.UI;

namespace GFramework.Game.Input;

/// <summary>
///     Bridges logical input mappings to the standalone UI service's ordered asynchronous dispatch.
/// </summary>
public sealed class UiInputDispatcher : IUiInputDispatcher
{
    private readonly IUiInputActionMap _actionMap;
    private readonly IUiService _service;

    /// <summary>
    ///     Creates an explicit input bridge; neither dependency requires an Architecture.
    /// </summary>
    /// <param name="actionMap">Logical-to-semantic action map.</param>
    /// <param name="service">Target UI service.</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="actionMap" /> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="ArgumentNullException">The service is null.</exception>
    public UiInputDispatcher(IUiInputActionMap actionMap, IUiService service)
    {
        _actionMap = actionMap ?? throw new ArgumentNullException(nameof(actionMap));
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <inheritdoc />
    public Task<bool> DispatchAsync(string actionName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_actionMap.TryMap(actionName, out var action))
        {
            return Task.FromResult(false);
        }

        return _service.DispatchActionAsync(action, cancellationToken);
    }
}
