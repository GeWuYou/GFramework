// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text;
using GFramework.Core.Abstractions.Concurrency;
using GFramework.Core.Abstractions.Serializer;
using GFramework.Core.Abstractions.Storage;
using GFramework.Core.Concurrency;
using GFramework.Godot.Extensions;
using Godot;
using FileAccess = Godot.FileAccess;

namespace GFramework.Godot.Storage;

/// <summary>
///     Godot 特化的文件存储实现，支持 res://、user:// 和普通文件路径
///     支持按 key 细粒度锁保证线程安全，使用异步安全的锁机制
/// </summary>
public sealed class GodotFileStorage : IStorage, IDisposable
{
    private readonly IAsyncKeyLockManager _lockManager;
    private readonly bool _ownsLockManager;
    private readonly ISerializer _serializer;
    private bool _disposed;

    /// <summary>
    ///     初始化 Godot 文件存储
    /// </summary>
    /// <param name="serializer">序列化器实例</param>
    /// <param name="lockManager">可选的锁管理器，用于依赖注入</param>
    public GodotFileStorage(ISerializer serializer, IAsyncKeyLockManager? lockManager = null)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

        if (lockManager == null)
        {
            _lockManager = new AsyncKeyLockManager();
            _ownsLockManager = true;
        }
        else
        {
            _lockManager = lockManager;
            _ownsLockManager = false;
        }
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // 只释放内部创建的锁管理器
        if (_ownsLockManager)
        {
            _lockManager.Dispose();
        }
    }

    #region Delete

    /// <summary>
    ///     删除指定键对应的文件
    /// </summary>
    /// <param name="key">存储键</param>
    /// <remarks>
    ///     此方法通过同步等待异步操作完成，可能在具有同步上下文的环境（例如 UI 线程、经典 ASP.NET）中导致死锁。
    ///     仅在无法使用异步 API 时使用。如果可能，请优先使用 <see cref="DeleteAsync"/>。
    /// </remarks>
    public void Delete(string key)
    {
        DeleteAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步删除指定键对应的文件
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>异步任务</returns>
    public async Task DeleteAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = ToAbsolutePath(key);
        var pathLock = await _lockManager.AcquireLockAsync(path).ConfigureAwait(false);

        await using (pathLock.ConfigureAwait(false))
        {
            // 处理Godot文件系统路径的删除操作
            if (path.IsGodotPath())
            {
                if (FileAccess.FileExists(path))
                {
                    var err = DirAccess.RemoveAbsolute(path);
                    if (err != Error.Ok)
                        throw new IOException($"Failed to delete Godot file: {path}, error: {err}");
                }
            }
            // 处理标准文件系统路径的删除操作
            else
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    ///     清理路径段中的无效字符，将无效文件名字符替换为下划线
    /// </summary>
    /// <param name="segment">要清理的路径段</param>
    /// <returns>清理后的路径段</returns>
    private static string SanitizeSegment(string segment)
    {
        return Path.GetInvalidFileNameChars().Aggregate(segment, (current, c) => current.Replace(c, '_'));
    }

    /// <summary>
    ///     将存储键转换为绝对路径，处理 Godot 虚拟路径和普通文件系统路径
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>绝对路径字符串</returns>
    private static string ToAbsolutePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Storage key cannot be empty", nameof(key));

        key = key.Replace('\\', '/');

        if (key.Contains(".."))
            throw new ArgumentException("Storage key cannot contain '..'", nameof(key));

        // Godot 虚拟路径直接使用 FileAccess 支持
        if (key.IsGodotPath())
            return key;

        // 普通文件系统路径
        var segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizeSegment)
            .ToArray();

        if (segments.Length == 0)
            throw new ArgumentException("Invalid storage key", nameof(key));

        var dir = Path.Combine(segments[..^1]);
        var fileName = segments[^1];

        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        return Path.Combine(dir, fileName);
    }

    #endregion

    #region Exists

    /// <summary>
    ///     检查指定键对应的文件是否存在
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>文件存在返回 true，否则返回 false</returns>
    /// <remarks>
    ///     此方法通过同步等待异步操作完成，可能在具有同步上下文的环境（例如 UI 线程、经典 ASP.NET）中导致死锁。
    ///     仅在无法使用异步 API 时使用。如果可能，请优先使用 <see cref="ExistsAsync"/>。
    /// </remarks>
    public bool Exists(string key)
    {
        return ExistsAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步检查指定键对应的文件是否存在
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>表示异步操作的任务，结果为布尔值表示文件是否存在</returns>
    public async Task<bool> ExistsAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = ToAbsolutePath(key);
        var pathLock = await _lockManager.AcquireLockAsync(path).ConfigureAwait(false);

        await using (pathLock.ConfigureAwait(false))
        {
            if (!path.IsGodotPath()) return File.Exists(path);
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            return file != null;
        }
    }

    #endregion

    #region Read

    /// <summary>
    ///     读取指定键对应的序列化数据并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <returns>反序列化后的对象实例</returns>
    /// <exception cref="FileNotFoundException">当指定键对应的文件不存在时抛出</exception>
    /// <remarks>
    ///     此方法通过同步等待异步操作完成，可能在具有同步上下文的环境（例如 UI 线程、经典 ASP.NET）中导致死锁。
    ///     仅在无法使用异步 API 时使用。如果可能，请优先使用 <see cref="ReadAsync{T}(string)"/>。
    /// </remarks>
    public T Read<T>(string key)
    {
        return ReadAsync<T>(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     读取指定键对应的序列化数据，如果文件不存在则返回默认值
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="defaultValue">当文件不存在时返回的默认值</param>
    /// <returns>反序列化后的对象实例或默认值</returns>
    public T Read<T>(string key, T defaultValue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            return Read<T>(key);
        }
        catch (FileNotFoundException)
        {
            return defaultValue;
        }
    }

    /// <summary>
    ///     异步读取指定键对应的序列化数据并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <returns>表示异步操作的任务，结果为反序列化后的对象实例</returns>
    public async Task<T> ReadAsync<T>(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = ToAbsolutePath(key);
        var pathLock = await _lockManager.AcquireLockAsync(path).ConfigureAwait(false);

        await using (pathLock.ConfigureAwait(false))
        {
            string content;

            if (path.IsGodotPath())
            {
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                if (file == null) throw new FileNotFoundException($"Storage key not found: {key}", path);
                content = file.GetAsText();
            }
            else
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Storage key not found: {key}", path);
                content = await File.ReadAllTextAsync(path, Encoding.UTF8).ConfigureAwait(false);
            }

            return _serializer.Deserialize<T>(content);
        }
    }

    #endregion

    #region Directory Operations

    /// <summary>
    ///     列举指定路径下的所有子目录名称
    /// </summary>
    /// <param name="path">要列举的路径，空字符串表示根目录</param>
    /// <returns>子目录名称列表</returns>
    public async Task<IReadOnlyList<string>> ListDirectoriesAsync(string path = "")
    {
        return await Task.Run(() =>
        {
            var fullPath = string.IsNullOrEmpty(path) ? "user://" : ToAbsolutePath(path);
            var dir = DirAccess.Open(fullPath);
            if (dir == null) return Array.Empty<string>();

            dir.ListDirBegin();
            var result = new List<string>();

            while (true)
            {
                var name = dir.GetNext();
                if (string.IsNullOrEmpty(name)) break;
                if (dir.CurrentIsDir() && !name.StartsWith(".", StringComparison.Ordinal))
                    result.Add(name);
            }

            dir.ListDirEnd();
            return (IReadOnlyList<string>)result;
        }).ConfigureAwait(false);
    }

    /// <summary>
    ///     列举指定路径下的所有文件名称
    /// </summary>
    /// <param name="path">要列举的路径，空字符串表示根目录</param>
    /// <returns>文件名称列表</returns>
    public async Task<IReadOnlyList<string>> ListFilesAsync(string path = "")
    {
        return await Task.Run(() =>
        {
            var fullPath = string.IsNullOrEmpty(path) ? "user://" : ToAbsolutePath(path);
            var dir = DirAccess.Open(fullPath);
            if (dir == null) return Array.Empty<string>();

            dir.ListDirBegin();
            var result = new List<string>();

            while (true)
            {
                var name = dir.GetNext();
                if (string.IsNullOrEmpty(name)) break;
                if (!dir.CurrentIsDir())
                    result.Add(name);
            }

            dir.ListDirEnd();
            return (IReadOnlyList<string>)result;
        }).ConfigureAwait(false);
    }

    /// <summary>
    ///     检查指定路径的目录是否存在
    /// </summary>
    /// <param name="path">要检查的目录路径</param>
    /// <returns>如果目录存在则返回true，否则返回false</returns>
    public Task<bool> DirectoryExistsAsync(string path)
    {
        var fullPath = ToAbsolutePath(path);
        return Task.FromResult(DirAccess.DirExistsAbsolute(fullPath));
    }

    /// <summary>
    ///     创建目录（递归创建父目录）
    /// </summary>
    /// <param name="path">要创建的目录路径</param>
    /// <returns>表示异步操作的Task</returns>
    public async Task CreateDirectoryAsync(string path)
    {
        await Task.Run(() =>
        {
            var fullPath = ToAbsolutePath(path);
            if (!DirAccess.DirExistsAbsolute(fullPath))
                DirAccess.MakeDirRecursiveAbsolute(fullPath);
        }).ConfigureAwait(false);
    }

    #endregion

    #region Write

    /// <summary>
    ///     将指定对象序列化并写入到指定键对应的文件中
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="value">要写入的对象实例</param>
    /// <remarks>
    ///     此方法通过同步等待异步操作完成，可能在具有同步上下文的环境（例如 UI 线程、经典 ASP.NET）中导致死锁。
    ///     仅在无法使用异步 API 时使用。如果可能，请优先使用 <see cref="WriteAsync{T}"/>。
    /// </remarks>
    public void Write<T>(string key, T value)
    {
        WriteAsync(key, value).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步将指定对象序列化并写入到指定键对应的文件中
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="value">要写入的对象实例</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task WriteAsync<T>(string key, T value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = ToAbsolutePath(key);
        var pathLock = await _lockManager.AcquireLockAsync(path).ConfigureAwait(false);

        await using (pathLock.ConfigureAwait(false))
        {
            var content = _serializer.Serialize(value);
            if (path.IsGodotPath())
            {
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
                if (file == null) throw new IOException($"Cannot write file: {path}");
                file.StoreString(content);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, content, Encoding.UTF8).ConfigureAwait(false);
            }
        }
    }

    #endregion
}
