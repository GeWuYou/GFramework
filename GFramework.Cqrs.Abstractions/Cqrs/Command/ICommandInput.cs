// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Abstractions.Cqrs.Command;

/// <summary>
///     命令输入接口，定义命令模式中输入数据的契约
///     该接口作为标记接口使用，不包含任何成员定义
/// </summary>
public interface ICommandInput : IInput;
