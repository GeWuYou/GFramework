// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Abstractions.Cqrs.Command;

/// <summary>
///     表示一个 CQRS 命令。
///     命令通常用于修改系统状态。
/// </summary>
/// <typeparam name="TResponse">命令响应类型。</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>
///     表示一个无显式返回值的 CQRS 命令。
/// </summary>
public interface ICommand : ICommand<Unit>;
