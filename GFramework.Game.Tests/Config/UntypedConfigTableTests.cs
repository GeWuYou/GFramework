// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>Checks heterogeneous reference keys through a small explicit capability instead of reflection.</summary>
[TestFixture]
public sealed class UntypedConfigTableTests
{
    /// <summary>Untyped lookup shares the typed comparer and rejects null or incompatible key types.</summary>
    [Test]
    public void UntypedKeysUseTheOriginalTypedTableSemantics()
    {
        IConfigTable strings = new InMemoryConfigTable<string, string>(["Alpha"], value => value,
            StringComparer.OrdinalIgnoreCase);
        Assert.That(strings.ContainsKey("alpha"), Is.True);
        Assert.That(strings.ContainsKey("missing"), Is.False);
        Assert.That(strings.ContainsKey(null), Is.False);
        Assert.That(strings.ContainsKey(1), Is.False);
        IConfigTable numbers = new InMemoryConfigTable<int, int>([1], value => value);
        Assert.That(numbers.ContainsKey(1), Is.True);
        Assert.That(numbers.ContainsKey(1L), Is.False);
        Assert.That(numbers.ContainsKey("1"), Is.False);
    }
}
