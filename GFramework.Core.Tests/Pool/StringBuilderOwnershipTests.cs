// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using GFramework.Core.Pool;
using NUnit.Framework;

namespace GFramework.Core.Tests.Pool;

/// <summary>Checks that scoped builder aliases return one borrow exactly once.</summary>
[TestFixture]
public sealed class StringBuilderOwnershipTests
{
    /// <summary>Repeated disposal cannot hand the same mutable instance to two borrowers.</summary>
    [Test]
    public void Dispose_AliasTwice_DoesNotDuplicateAvailableBuilder()
    {
        var scope = StringBuilderPool.GetScoped();
        var alias = scope;
        scope.Dispose();
        alias.Dispose();

        using var first = StringBuilderPool.GetScoped();
        using var second = StringBuilderPool.GetScoped();
        Assert.That(first.Value, Is.Not.SameAs(second.Value));
        Assert.Throws<ObjectDisposedException>(() => _ = scope.Value);
        Assert.Throws<ObjectDisposedException>(() => _ = alias.Value);
    }

    /// <summary>An old token must not clear or return a builder that has already been borrowed again.</summary>
    [Test]
    public void Dispose_AfterNextBorrow_PreservesNextBorrowerContent()
    {
        var scope = StringBuilderPool.GetScoped();
        var original = scope.Value;
        scope.Dispose();
        using var next = StringBuilderPool.GetScoped();
        Assert.That(next.Value, Is.SameAs(original));
        next.Value.Append("owned by next borrower");

        scope.Dispose();

        Assert.That(next.Value.ToString(), Is.EqualTo("owned by next borrower"));
        using var other = StringBuilderPool.GetScoped();
        Assert.That(other.Value, Is.Not.SameAs(next.Value));
    }

    /// <summary>Concurrent disposal aliases share one atomic return without requiring concurrent builder use.</summary>
    [Test]
    public async Task Dispose_ConcurrentAliases_ReturnBuilderOnce()
    {
        var scope = StringBuilderPool.GetScoped();
        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(scope.Dispose)));

        var borrowed = new List<StringBuilder>();
        try
        {
            for (var i = 0; i < 32; i++)
                borrowed.Add(StringBuilderPool.Rent());
            Assert.That(borrowed.Distinct(ReferenceEqualityComparer.Instance).Count(), Is.EqualTo(borrowed.Count));
            Assert.Throws<ObjectDisposedException>(() => _ = scope.Value);
        }
        finally
        {
            foreach (var builder in borrowed)
                StringBuilderPool.Return(builder);
        }
    }
}
