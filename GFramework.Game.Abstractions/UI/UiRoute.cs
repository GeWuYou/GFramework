// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>Identifies a registered view and its strongly typed opening arguments.</summary>
/// <typeparam name="TArgs">Arguments understood by this route's factory.</typeparam>
public class UiRoute<TArgs>
{
    /// <summary>Creates a stable route definition to register and reuse at call sites.</summary>
    /// <param name="key">Stable diagnostic and registration identifier.</param>
    /// <param name="options">Presentation policy; defaults to a page with normal navigation history.</param>
    public UiRoute(string key, UiRouteOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A UI route key cannot be empty.", nameof(key));
        Key = key;
        Options = options ?? new UiRouteOptions();
        if (!Enum.IsDefined(typeof(UiRouteKind), Options.Kind))
            throw new ArgumentOutOfRangeException(nameof(options), "Unknown UI route kind.");
    }

    /// <summary>Gets the stable registration identifier.</summary>
    public string Key
    {
        get;
    }
    /// <summary>Gets immutable presentation and interaction policy.</summary>
    public UiRouteOptions Options
    {
        get;
    }
}
