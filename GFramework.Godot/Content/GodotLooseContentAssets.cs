// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using Godot;

namespace GFramework.Godot.Content;

/// <summary>
/// Loads data-mod media through native image/audio decoders rather than ResourceLoader.
/// This entry point never accepts scenes, resources, scripts or assemblies. Call it on
/// the Godot thread and release returned resources after their final native consumer.
/// </summary>
public sealed class GodotLooseContentAssets
{
    private readonly GodotContentFileResolver _files;
    private readonly int _threadId;

    /// <summary>Creates an asset reader restricted to one package directory.</summary>
    /// <param name="rootDirectory">Host-selected physical package directory.</param>
    public GodotLooseContentAssets(string rootDirectory)
    {
        _files = new GodotContentFileResolver(rootDirectory);
        _threadId = System.Environment.CurrentManagedThreadId;
    }

    /// <summary>Decodes a PNG as a texture without invoking script-bearing resource loaders.</summary>
    /// <param name="relativePath">The PNG path relative to the package.</param>
    /// <returns>A caller-owned texture.</returns>
    public ImageTexture LoadTexture(string relativePath)
    {
        RequireHostThread();
        var path = RequireExtension(relativePath, ".png");
        using var image = Image.LoadFromFile(path);
        if (image is null || image.IsEmpty())
        {
            throw new IOException("Could not decode package PNG: " + relativePath);
        }
        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>Decodes an OGG or WAV stream without loading a native resource file.</summary>
    /// <param name="relativePath">The OGG or WAV path relative to the package.</param>
    /// <returns>A caller-owned audio stream.</returns>
    public AudioStream LoadAudio(string relativePath)
    {
        RequireHostThread();
        var path = _files.Resolve(relativePath);
        var extension = Path.GetExtension(path);
        AudioStream? stream;
        if (string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase))
        {
            stream = AudioStreamOggVorbis.LoadFromFile(path);
        }
        else if (string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
        {
            stream = AudioStreamWav.LoadFromFile(path);
        }
        else
        {
            throw new ArgumentException("Data-mod audio must be OGG or WAV.", nameof(relativePath));
        }
        return stream ?? throw new IOException("Could not decode package audio: " + relativePath);
    }

    private string RequireExtension(string relativePath, string extension)
    {
        var path = _files.Resolve(relativePath);
        if (!string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Data-mod images must be PNG.", nameof(relativePath));
        }
        return path;
    }

    private void RequireHostThread()
    {
        if (System.Environment.CurrentManagedThreadId != _threadId)
        {
            throw new InvalidOperationException("Native media decoding must run on the owning Godot thread.");
        }
    }
}
