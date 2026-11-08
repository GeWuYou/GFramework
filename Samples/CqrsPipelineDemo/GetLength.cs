// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace CqrsPipelineDemo;

/// <summary>A request consumed by the generated handler registry.</summary>
/// <param name="Value">The value whose length is requested.</param>
public sealed record GetLength(string Value) : IRequest<int>;
