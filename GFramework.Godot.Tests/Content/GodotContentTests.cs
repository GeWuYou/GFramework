// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using GFramework.Godot.Content;
using NUnit.Framework;

namespace GFramework.Godot.Tests.Content;

/// <summary>Exercises native-content admission without requiring a native Godot process.</summary>
[TestFixture]
public sealed class GodotContentTests
{
    private string _root = null!;

    /// <summary>Creates one test-owned directory.</summary>
    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "gframework-native-content-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    /// <summary>Removes only this test's isolated files.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    /// <summary>Absolute and traversal paths never reach a decoder or native pack loader.</summary>
    [TestCase("../outside.png")]
    [TestCase("sub/../../outside.png")]
    [TestCase("res://outside.png")]
    [TestCase("/outside.png")]
    [TestCase("C:\\outside.png")]
    [TestCase("sub//image.png")]
    public void FileResolver_RejectsEscapingPaths(string path)
    {
        var files = new GodotContentFileResolver(_root);
        Assert.Throws<ArgumentException>(() => files.Resolve(path));
    }

    /// <summary>Package identities cannot become traversal segments in the native resource namespace.</summary>
    [TestCase(".")]
    [TestCase("..")]
    [TestCase("-x")]
    [TestCase("_x")]
    public void PackDescriptor_RejectsNonCanonicalIdentity(string id)
    {
        Assert.Throws<ArgumentException>(() => new GodotContentPackDescriptor(id, _root, "pack.pck"));
    }

    /// <summary>A nested symbolic link cannot redirect otherwise normal-looking package paths.</summary>
    [Test]
    public void FileResolver_RejectsLinks()
    {
        var target = Path.Combine(_root, "target");
        Directory.CreateDirectory(target);
        var link = Path.Combine(_root, "link");
        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (UnauthorizedAccessException) { Assert.Ignore("This environment does not permit creating symbolic links."); }
        var files = new GodotContentFileResolver(_root);
        Assert.Throws<ArgumentException>(() => files.Resolve("link/image.png"));
    }

    /// <summary>A package label cannot bypass the default-deny trust boundary.</summary>
    [Test]
    public void Mounter_WithoutPolicy_RejectsBeforeNativeCall()
    {
        var nativeCalls = 0;
        var mounter = new GodotContentPackMounter(trustPolicy: null, _ => { nativeCalls++; return true; });
        Assert.Throws<InvalidOperationException>(() => mounter.MountAll([Pack("official")]));
        Assert.That(nativeCalls, Is.Zero);
        Assert.That(mounter.RequiresRestart, Is.False);
    }

    /// <summary>Every PCK is checked before the irreversible first mount.</summary>
    [Test]
    public void Mounter_ValidatesCompleteSelectionAndActualHash()
    {
        var nativeCalls = 0;
        var first = Pack("first");
        var second = Pack("second");
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(first.FilePath))).ToLowerInvariant();
        var mounter = new GodotContentPackMounter((pack, hash) => string.Equals(pack.PackId, "first", StringComparison.Ordinal) && string.Equals(hash, actual, StringComparison.Ordinal),
            _ => { nativeCalls++; return true; });
        Assert.Throws<InvalidOperationException>(() => mounter.MountAll([first, second]));
        Assert.That(nativeCalls, Is.Zero);
    }

    /// <summary>Successful mounting preserves effective order and namespaced resource resolution.</summary>
    [Test]
    public void Mounter_MountsInOrderAndRejectsRepeatedSelection()
    {
        var paths = new List<string>();
        var first = Pack("first");
        var second = Pack("second");
        var mounter = new GodotContentPackMounter(static (_, _) => true, path => { paths.Add(path); return true; });
        mounter.MountAll([first, second]);
        Assert.That(paths, Is.EqualTo([first.FilePath, second.FilePath]));
        Assert.That(mounter.GetResourcePath("first", "arena.tscn"), Is.EqualTo("res://content/first/arena.tscn"));
        Assert.Throws<ArgumentException>(() => mounter.GetResourcePath("first", "../base.tscn"));
        Assert.Throws<InvalidOperationException>(() => mounter.MountAll([]));
    }

    /// <summary>One official package can contribute multiple distinct PCK files in declared order.</summary>
    [Test]
    public void Mounter_AllowsMultipleFilesForOnePackageAndRejectsRepeatedFile()
    {
        var first = Pack("official");
        File.WriteAllText(Path.Combine(_root, "extra.pck"), "extra official resources");
        var second = new GodotContentPackDescriptor("official", _root, "extra.pck");
        var calls = 0;
        var mounter = new GodotContentPackMounter(static (_, _) => true, _ => { calls++; return true; });
        mounter.MountAll([first, second]);
        Assert.That(calls, Is.EqualTo(2));
        var duplicateMounter = new GodotContentPackMounter(static (_, _) => true, static _ => true);
        Assert.Throws<InvalidOperationException>(() => duplicateMounter.MountAll([first, first]));
    }

    /// <summary>A native failure invalidates the whole session, including earlier mounted resources.</summary>
    [Test]
    public void Mounter_PartialFailure_RequiresRestartAndRefusesResourceUse()
    {
        var calls = 0;
        var mounter = new GodotContentPackMounter(static (_, _) => true, _ => ++calls == 1);
        Assert.Throws<IOException>(() => mounter.MountAll([Pack("first"), Pack("second")]));
        Assert.That(mounter.RequiresRestart, Is.True);
        Assert.Throws<InvalidOperationException>(() => mounter.GetResourcePath("first", "arena.tscn"));
    }

    /// <summary>Background callers cannot enter the engine mount boundary.</summary>
    [Test]
    public async Task Mounter_RejectsBackgroundThread()
    {
        var mounter = new GodotContentPackMounter(static (_, _) => true, static _ => true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => mounter.MountAll([])));
    }

    /// <summary>Loose media decoding also rejects worker-thread entry before calling native Godot APIs.</summary>
    [Test]
    public async Task LooseAssets_RejectBackgroundThreadBeforeNativeDecode()
    {
        var assets = new GodotLooseContentAssets(_root);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => assets.LoadTexture("image.png")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => assets.LoadAudio("music.ogg")));
    }

    private GodotContentPackDescriptor Pack(string id)
    {
        var path = Path.Combine(_root, id + ".pck");
        File.WriteAllText(path, "test fixture " + id);
        return new GodotContentPackDescriptor(id, _root, id + ".pck");
    }
}
