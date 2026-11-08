// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;

namespace CqrsPipelineDemo;

/// <summary>Shared scope state with an observable final release.</summary>
public sealed class RequestScope : IDisposable
{
    /// <summary>Observed behavior invocations.</summary>
    public int Calls
    {
        get; set;
    }
    /// <summary>Whether the scope disposed this instance.</summary>
    public bool Disposed
    {
        get; private set;
    }
    /// <inheritdoc />
    public void Dispose() => Disposed = true;
}
