// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Routing;

namespace GFramework.Game.Abstractions.UI;

/// <summary>
/// UI页面进入参数接口
/// 该接口用于定义UI页面跳转时传递的参数数据结构
/// </summary>
public interface IUiPageEnterParam : IRouteContext;