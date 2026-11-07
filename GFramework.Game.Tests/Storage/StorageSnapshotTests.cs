// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using GFramework.Game.Abstractions.Storage;

namespace GFramework.Game.Tests.Storage;

/// <summary>Protects the lossless bytes and fingerprint used in recovery approvals.</summary>
[TestFixture]
public sealed class StorageSnapshotTests
{
    /// <summary>Changing the original input or a returned buffer cannot change an approved backup.</summary>
    [Test]
    public void Snapshot_OwnsItsBytesAndUsesStableSha256()
    {
        var bytes = Encoding.UTF8.GetBytes("abc");
        var snapshot = new StorageSnapshot(bytes);
        bytes[0] = 0;
        var copy = snapshot.ToArray();
        copy[1] = 0;
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.ReadText(), Is.EqualTo("abc"));
            Assert.That(snapshot.Fingerprint, Is.EqualTo("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD"));
        });
    }

    /// <summary>Text decoding consumes a BOM without altering the bytes preserved in the backup.</summary>
    [Test]
    public void Snapshot_DecodesBomButRetainsOriginalEncoding()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("old data")).ToArray();
        var snapshot = new StorageSnapshot(bytes);
        Assert.That(snapshot.ReadText(), Is.EqualTo("old data"));
        Assert.That(snapshot.ToArray(), Is.EqualTo(bytes));
    }
}
