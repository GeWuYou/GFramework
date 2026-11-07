// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>Describes the business outcome of a closed dialog.</summary>
/// <typeparam name="TResult">Accepted result type.</typeparam>
/// <param name="Accepted">Whether the view submitted a business result.</param>
/// <param name="Value">Submitted value; inspect Accepted before using it.</param>
public readonly record struct DialogResult<TResult>(bool Accepted, TResult? Value);
