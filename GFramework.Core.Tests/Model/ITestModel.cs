// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Model;

namespace GFramework.Core.Tests.Model;

public interface ITestModel : IModel
{
    int GetCurrentXp { get; }
}