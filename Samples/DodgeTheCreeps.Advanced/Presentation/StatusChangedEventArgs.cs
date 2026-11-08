// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;

namespace DodgeTheCreeps.Advanced;

/// <summary>Supplies a deferred localized message without retaining a native view.</summary>
/// <param name="message">The message evaluated in the active language when a view refreshes.</param>
internal sealed class StatusChangedEventArgs(Func<string> message) : EventArgs
{
    /// <summary>The current message formatter.</summary>
    internal Func<string> Message { get; } = message;
}
