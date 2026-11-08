// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Properties;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>Optional instance configuration; container and lifecycle safety cannot be disabled.</summary>
public interface IArchitectureConfiguration
{
    /// <summary>The externally owned logger factory, or null for a framework-owned instance factory.</summary>
    LoggerProperties LoggerProperties
    {
        get; set;
    }
}
