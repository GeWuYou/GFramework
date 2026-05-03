// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Tests.Extensions;

/// <summary>
///     为 <see cref="UnRegisterListExtensionTests" /> 提供可观察的 <see cref="IUnRegisterList" /> 测试替身。
/// </summary>
public class TestUnRegisterList : IUnRegisterList
{
    /// <summary>
    ///     获取当前测试收集到的注销项列表，供断言扩展方法是否正确追加和清空元素。
    /// </summary>
    public IList<IUnRegister> UnregisterList { get; } = new List<IUnRegister>();
}
