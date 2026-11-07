// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GFramework.Game.Storage;
using NUnit.Framework;

namespace GFramework.Game.Tests.Storage;

/// <summary>
///     Exercises real file replacement and failures after the temporary file has already received data.
/// </summary>
[TestFixture]
public sealed class AtomicFileWriterTests
{
    private string _root = null!;

    /// <summary>Creates an isolated directory for each test.</summary>
    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "gframework-atomic-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    /// <summary>Removes this test's own files.</summary>
    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_root, recursive: true);
    }

    /// <summary>Replaces an existing file and leaves no temporary files after success.</summary>
    [Test]
    public async Task WriteAllTextAsync_ReplacesExistingFileAndCleansTemporaryFile()
    {
        var path = Path.Combine(_root, "save.json");
        await File.WriteAllTextAsync(path, "old");

        await AtomicFileWriter.WriteAllTextAsync(path, "new");

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(path), Is.EqualTo("new"));
            Assert.That(Directory.GetFiles(_root), Has.Length.EqualTo(1));
        });
    }

    /// <summary>A failed write after partial output never truncates or replaces the previous save.</summary>
    [Test]
    public async Task WriteAsync_WhenWriterFailsAfterPartialOutput_PreservesOriginalFile()
    {
        var path = Path.Combine(_root, "save.json");
        await File.WriteAllTextAsync(path, "old valid save");
        var failure = new IOException("Simulated interrupted write");

        var thrown = await Assert.ThrowsAsync<IOException>(() =>
            AtomicFileWriter.WriteAsync(path, async stream =>
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes("incomplete")).ConfigureAwait(false);
                throw failure;
            }));

        Assert.Multiple(() =>
        {
            Assert.That(thrown, Is.SameAs(failure));
            Assert.That(File.ReadAllText(path), Is.EqualTo("old valid save"));
            Assert.That(Directory.GetFiles(_root), Has.Length.EqualTo(1));
        });
    }

    /// <summary>A replacement failure cleans the temporary file without deleting the destination directory.</summary>
    [Test]
    public async Task WriteAllTextAsync_WhenReplacementFails_CleansTemporaryFile()
    {
        var path = Path.Combine(_root, "save.json");
        Directory.CreateDirectory(path);

        await Assert.ThrowsAsync<IOException>(() => AtomicFileWriter.WriteAllTextAsync(path, "new"));

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(path), Is.True);
            Assert.That(Directory.GetFiles(_root), Is.Empty);
        });
    }
}
