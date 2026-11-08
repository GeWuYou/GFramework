// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Properties;

namespace GFramework.Core.Architectures;

/// <summary>Instance-specific logging options for the optional architecture owner.</summary>
public sealed class ArchitectureConfiguration : IArchitectureConfiguration
{
    /// <inheritdoc />
    public LoggerProperties LoggerProperties { get; set; } = new();
}
