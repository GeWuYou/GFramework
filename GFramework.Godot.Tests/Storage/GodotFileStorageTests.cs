// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Serializer;
using GFramework.Godot.Storage;
using NUnit.Framework;
using JsonSerializer = GFramework.Game.Serializer.JsonSerializer;

namespace GFramework.Godot.Tests.Storage;

/// <summary>
///     Tests user-data writes through an explicit physical-path resolver without loading the native Godot runtime.
/// </summary>
[TestFixture]
public sealed class GodotFileStorageTests
{
    private string _root = null!;

    /// <summary>Creates an isolated user-data directory.</summary>
    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "gframework-godot-storage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    /// <summary>Removes only this test's temporary data.</summary>
    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_root, recursive: true);
    }

    /// <summary>Writes a complete user path without appending FileStorage's default .dat extension.</summary>
    [Test]
    public async Task WriteAsync_UserPath_ResolvesBeforeWritingAndPreservesFileName()
    {
        string? resolvedKey = null;
        using var storage = new GodotFileStorage(new JsonSerializer(), null, key =>
        {
            resolvedKey = key;
            return Path.Combine(_root, key["user://".Length..]);
        });

        await storage.WriteAsync("user://saves/slot_1/save.json", 42);

        var path = Path.Combine(_root, "saves", "slot_1", "save.json");
        Assert.Multiple(() =>
        {
            Assert.That(resolvedKey, Is.EqualTo("user://saves/slot_1/save.json"));
            Assert.That(File.ReadAllText(path), Is.EqualTo("42"));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 52, 50 }));
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)!), Has.Length.EqualTo(1));
        });
    }

    /// <summary>Ordinary absolute paths retain their root and support a managed round trip without the engine.</summary>
    [Test]
    public async Task WriteAsync_AbsolutePath_PreservesRootAndRoundTrips()
    {
        var path = Path.Combine(_root, "nested", "save.json");
        using var storage = new GodotFileStorage(new JsonSerializer(), null,
            _ => throw new InvalidOperationException("Ordinary paths do not need native resolution"));

        await storage.WriteAsync(path, 42);

        Assert.That(File.Exists(path), Is.True);
        Assert.That(await storage.ReadAsync<int>(path), Is.EqualTo(42));
    }

    /// <summary>Serialization failure preserves an existing save and does not create temporary output.</summary>
    [Test]
    public async Task WriteAsync_WhenSerializationFails_PreservesPreviousSave()
    {
        var path = Path.Combine(_root, "save.json");
        await File.WriteAllTextAsync(path, "old valid save");
        using var storage = new GodotFileStorage(new FailingSerializer(), null, _ => path);

        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.WriteAsync("user://save.json", 42));

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(path), Is.EqualTo("old valid save"));
            Assert.That(Directory.GetFiles(_root), Has.Length.EqualTo(1));
        });
    }

    /// <summary>Resource writes fail explicitly without consulting a native resolver.</summary>
    [Test]
    public async Task WriteAsync_ResourcePath_IsRejectedBeforeNativeResolution()
    {
        using var storage = new GodotFileStorage(new JsonSerializer(), null,
            _ => throw new InvalidOperationException("Resolver must not be called"));

        await Assert.ThrowsAsync<NotSupportedException>(() => storage.WriteAsync("res://save.json", 42));
    }

    /// <summary>Provides a deterministic serialization failure before any file is opened.</summary>
    private sealed class FailingSerializer : ISerializer
    {
        /// <inheritdoc />
        public string Serialize<T>(T value)
        {
            throw new InvalidOperationException("Cannot serialize this save");
        }

        /// <inheritdoc />
        public T Deserialize<T>(string data)
        {
            throw new NotSupportedException();
        }
    }
}
