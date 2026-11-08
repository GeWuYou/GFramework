// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Modding;

/// <summary>One optional extension's mod-local services, disposed after the mod stops.</summary>
public interface IGameModServiceScope : IServiceProvider, IAsyncDisposable;
