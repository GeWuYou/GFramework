// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

// meta-name: 普通节点控制器
// meta-description: 原生节点脚本，由父节点在入树前显式传入项目依赖。
using Godot;

/// <summary>Receives project dependencies from its parent before entering the scene tree.</summary>
public partial class _CLASS_ : _BASE_
{
    /// <summary>Connects native behavior after the parent has supplied this node's dependencies.</summary>
    public override void _Ready()
    {
        // Add a project-specific Configure method for dependencies, then consume them here.
        // [GetNode] and [BindNodeSignal] adoption follows their explicit lifecycle hooks.
    }
}
