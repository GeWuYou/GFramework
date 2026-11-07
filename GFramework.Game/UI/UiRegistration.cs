// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;

namespace GFramework.Game.UI;

/// <summary>Erases factory argument types only inside the runtime after typed route identity is checked.</summary>
/// <param name="Key">Stable route key.</param>
/// <param name="Definition">Exact registered definition object.</param>
/// <param name="Options">Immutable route policies.</param>
/// <param name="Create">Typed factory adapter.</param>
/// <param name="CreateDialog">Optional typed dialog completion factory.</param>
internal sealed record UiRegistration(string Key, object Definition, UiRouteOptions Options,
    Func<object?, UiInstanceHandle, UiService, Func<object?, CancellationToken, Task<UiOperationResult>>,
        CancellationToken, Task<IUiView>> Create,
    Func<IUiDialogCompletion>? CreateDialog = null);
