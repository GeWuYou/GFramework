// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

// meta-name: 控制器类模板
// meta-description: 负责管理场景的生命周期和架构关联
using Godot;
using GFramework.Core.Abstractions.Controller;
using GFramework.Godot.SourceGenerators.Abstractions;
using GFramework.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;


[ContextAware]
[Log]
public partial class _CLASS_ :_BASE_,IController
{
    /// <summary>
    /// 节点准备就绪时的回调方法
    /// 在节点添加到场景树后调用
    /// </summary>
    public override void _Ready()
    {
        __InjectGetNodes_Generated();
        OnReadyAfterGetNode();
    }

    /// <summary>
    /// 节点注入完成后的初始化钩子。
    /// </summary>
    private void OnReadyAfterGetNode()
    {
    }
}
