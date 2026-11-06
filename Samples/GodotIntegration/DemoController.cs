// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.SourceGenerators.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using Godot;

namespace GodotIntegration;

/// <summary>
///     Godot 脚本接入路径示例：节点脚本通过 [ContextAware] 绑定架构上下文，
///     [Log] 生成经由上下文解析的标准日志属性；节点不进入进程级解析器。
/// </summary>
[ContextAware]
[Log]
public partial class DemoController : Node
{
    /// <summary>就绪时通过生成的日志属性输出一条调试日志。</summary>
    public override void _Ready()
    {
        _log.LogDebug("DemoController ready");
    }
}
