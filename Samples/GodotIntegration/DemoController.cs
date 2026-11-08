// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using Microsoft.Extensions.Logging;
using Godot;

namespace GodotIntegration;

/// <summary>An engine-created node with explicit dependencies supplied before it joins the scene tree.</summary>
public partial class DemoController : Node
{
    private ILogger<DemoController>? _logger;

    /// <summary>Attaches only the capability this node actually needs.</summary>
    /// <param name="logger">The host's instance logger.</param>
    public void Initialize(ILogger<DemoController> logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public override void _Ready() => _logger?.LogDebug("DemoController ready");
}
