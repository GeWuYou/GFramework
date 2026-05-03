// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Systems;

namespace GFramework.Core.Abstractions.State;

/// <summary>
///     状态机系统接口，继承自ISystem和IStateMachine接口
///     提供状态机系统的功能定义，结合了系统管理和状态机管理的能力
/// </summary>
public interface IStateMachineSystem : ISystem, IStateMachine;