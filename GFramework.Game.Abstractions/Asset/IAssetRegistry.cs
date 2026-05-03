// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Registries;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Game.Abstractions.Asset;

/// <summary>
///     资源注册表接口，用于管理指定类型T的资源注册和查找
/// </summary>
/// <typeparam name="T">资源的类型</typeparam>
public interface IAssetRegistry<T> : IUtility, IRegistry<string, T>;