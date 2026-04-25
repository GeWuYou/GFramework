using System.IO;
using System.Text;
using GFramework.Core.Abstractions.Concurrency;
using GFramework.Core.Abstractions.Serializer;
using GFramework.Core.Concurrency;
using GFramework.Game.Abstractions.Storage;

namespace GFramework.Game.Storage;

/// <summary>
///     基于文件系统的存储实现，实现了IFileStorage接口，支持按key细粒度锁保证线程安全
///     使用异步安全的锁机制、原子写入和自动清理
/// </summary>
public sealed class FileStorage : IFileStorage, IDisposable
{
    private readonly int _bufferSize;
    private readonly string _extension;
    private readonly IAsyncKeyLockManager _lockManager;
    private readonly bool _ownsLockManager;
    private readonly string _rootPath;
    private readonly ISerializer _serializer;
    private bool _disposed;

    /// <summary>
    ///     初始化FileStorage实例
    /// </summary>
    /// <param name="rootPath">存储根目录路径</param>
    /// <param name="serializer">序列化器实例</param>
    /// <param name="extension">存储文件的扩展名</param>
    /// <param name="bufferSize">IO 缓冲区大小，默认 8KB</param>
    /// <param name="lockManager">可选的锁管理器，用于依赖注入</param>
    public FileStorage(string rootPath, ISerializer serializer, string extension = ".dat", int bufferSize = 8192,
        IAsyncKeyLockManager? lockManager = null)
    {
        _rootPath = rootPath;
        _serializer = serializer;
        _extension = extension;
        _bufferSize = bufferSize;

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

        Directory.CreateDirectory(_rootPath);
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

    /// <summary>
    ///     清理文件段字符串，将其中的无效文件名字符替换为下划线
    /// </summary>
    /// <param name="segment">需要清理的文件段字符串</param>
    /// <returns>清理后的字符串，其中所有无效文件名字符都被替换为下划线</returns>
    private static string SanitizeSegment(string segment)
    {
        return Path.GetInvalidFileNameChars().Aggregate(segment, (current, c) => current.Replace(c, '_'));
    }

    #region Helpers

    /// <summary>
    ///     将存储键转换为文件路径
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>对应的文件路径</returns>
    private string ToPath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Storage key cannot be empty", nameof(key));

        // 统一分隔符
        key = key.Replace('\\', '/');

        // 防止路径逃逸
        if (key.Contains(".."))
            throw new ArgumentException("Storage key cannot contain '..'", nameof(key));

        var segments = key
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizeSegment)
            .ToArray();

        if (segments.Length == 0)
            throw new ArgumentException("Invalid storage key", nameof(key));

        // 目录部分
        var dirSegments = segments[..^1];
        var fileName = segments[^1] + _extension;

        var dirPath = dirSegments.Length == 0
            ? _rootPath
            : Path.Combine(_rootPath, Path.Combine(dirSegments));

        Directory.CreateDirectory(dirPath);

        return Path.Combine(dirPath, fileName);
    }

    #endregion

    #region Delete

    /// <summary>
    ///     删除指定键的存储项
    /// </summary>
    /// <param name="key">存储键，用于标识要删除的存储项</param>
    /// <remarks>
    ///     此方法通过同步等待异步操作完成，可能在具有同步上下文的环境（例如 UI 线程、经典 ASP.NET）中导致死锁。
    ///     仅在无法使用异步 API 时使用。如果可能，请优先使用 <see cref="DeleteAsync"/>。
    /// </remarks>
    public void Delete(string key)
    {
        DeleteAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步删除指定键的存储项
    /// </summary>
    /// <param name="key">存储键，用于标识要删除的存储项</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task DeleteAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = ToPath(key);

        var pathLock = await _lockManager.AcquireLockAsync(path).ConfigureAwait(false);
        await using var configuredPathLock = pathLock.ConfigureAwait(false);

        if (File.Exists(path))
            File.Delete(path);
    }

    #endregion

    #region Exists

    /// <summary>
    ///     检查指定键的存储项是否存在
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>如果存储项存在则返回true，否则返回false</returns>
    /// <remarks>
    ///     此方法通过同步等待异步操作完成，可能在具有同步上下文的环境（例如 UI 线程、经典 ASP.NET）中导致死锁。
    ///     仅在无法使用异步 API 时使用。如果可能，请优先使用 <see cref="ExistsAsync"/>。
    /// </remarks>
    public bool Exists(string key)
    {
        return ExistsAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步检查指定键的存储项是否存在
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>如果存储项存在则返回true，否则返回false</returns>
    public async Task<bool> ExistsAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = ToPath(key);

        var pathLock = await _lockManager.AcquireLockAsync(path).ConfigureAwait(false);
        await using var configuredPathLock = pathLock.ConfigureAwait(false);

        return File.Exists(path);
    }

    #endregion

    #region Read

    /// <summary>
    ///     读取指定键的存储项
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="FileNotFoundException">当存储键不存在时抛出</exception>
    /// <remarks>
    ///     此方法通过同步等待异步操作完成，可能在具有同步上下文的环境（例如 UI 线程、经典 ASP.NET）中导致死锁。
    ///     仅在无法使用异步 API 时使用。如果可能，请优先使用 <see cref="ReadAsync{T}(string)"/>。
    /// </remarks>
    public T Read<T>(string key)
    {
        return ReadAsync<T>(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     读取指定键的存储项，如果不存在则返回默认值
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="defaultValue">当存储键不存在时返回的默认值</param>
    /// <returns>反序列化后的对象或默认值</returns>
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
    ///     异步读取指定键的存储项
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="FileNotFoundException">当存储键不存在时抛出</exception>
    public async Task<T> ReadAsync<T>(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = ToPath(key);

        var pathLock = await _lockManager.AcquireLockAsync(path).ConfigureAwait(false);
        await using var configuredPathLock = pathLock.ConfigureAwait(false);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Storage key not found: {key}", path);

        var fs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            _bufferSize,
            useAsync: true);
        await using var configuredFileStream = fs.ConfigureAwait(false);

        using var sr = new StreamReader(fs, Encoding.UTF8, true, -1, leaveOpen: true);
        var content = await sr.ReadToEndAsync().ConfigureAwait(false);
        return _serializer.Deserialize<T>(content);
    }

    #endregion

    #region Directory Operations

    /// <summary>
    ///     列举指定路径下的所有子目录名称
    /// </summary>
    /// <param name="path">要列举的路径，空字符串表示根目录</param>
    /// <returns>子目录名称列表</returns>
    public Task<IReadOnlyList<string>> ListDirectoriesAsync(string path = "")
    {
        var fullPath = string.IsNullOrEmpty(path) ? _rootPath : Path.Combine(_rootPath, path);
        if (!Directory.Exists(fullPath))
            return Task.FromResult<IReadOnlyList<string>>([]);

        var dirs = Directory.GetDirectories(fullPath)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith('.'))
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(dirs);
    }

    /// <summary>
    ///     列举指定路径下的所有文件名称
    /// </summary>
    /// <param name="path">要列举的路径，空字符串表示根目录</param>
    /// <returns>文件名称列表</returns>
    public Task<IReadOnlyList<string>> ListFilesAsync(string path = "")
    {
        var fullPath = string.IsNullOrEmpty(path) ? _rootPath : Path.Combine(_rootPath, path);
        if (!Directory.Exists(fullPath))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var files = Directory.GetFiles(fullPath)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    /// <summary>
    ///     检查指定路径的目录是否存在
    /// </summary>
    /// <param name="path">要检查的目录路径</param>
    /// <returns>如果目录存在则返回true，否则返回false</returns>
    public Task<bool> DirectoryExistsAsync(string path)
    {
        var fullPath = string.IsNullOrEmpty(path) ? _rootPath : Path.Combine(_rootPath, path);
        return Task.FromResult(Directory.Exists(fullPath));
    }

    /// <summary>
    ///     创建目录（递归创建父目录）
    /// </summary>
    /// <param name="path">要创建的目录路径</param>
    /// <returns>表示异步操作的Task</returns>
    public Task CreateDirectoryAsync(string path)
    {
        var fullPath = string.IsNullOrEmpty(path) ? _rootPath : Path.Combine(_rootPath, path);
        Directory.CreateDirectory(fullPath);
        return Task.CompletedTask;
    }

    #endregion

    #region Write

    /// <summary>
    ///     写入指定键的存储项
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="value">要存储的对象</param>
    /// <remarks>
    ///     此方法通过同步等待异步操作完成，可能在具有同步上下文的环境（例如 UI 线程、经典 ASP.NET）中导致死锁。
    ///     仅在无法使用异步 API 时使用。如果可能，请优先使用 <see cref="WriteAsync{T}"/>。
    /// </remarks>
    public void Write<T>(string key, T value)
    {
        WriteAsync(key, value).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步写入指定键的存储项，使用原子写入防止文件损坏
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="value">要存储的对象</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task WriteAsync<T>(string key, T value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = ToPath(key);
        var tempPath = path + ".tmp";

        var pathLock = await _lockManager.AcquireLockAsync(path).ConfigureAwait(false);
        await using var configuredPathLock = pathLock.ConfigureAwait(false);

        try
        {
            var content = _serializer.Serialize(value);

            // 先写入临时文件
            {
                var fs = new FileStream(
                    tempPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    _bufferSize,
                    useAsync: true);
                await using var configuredFileStream = fs.ConfigureAwait(false);

                var sw = new StreamWriter(fs, Encoding.UTF8, leaveOpen: true);
                await using var configuredStreamWriter = sw.ConfigureAwait(false);

                await sw.WriteAsync(content).ConfigureAwait(false);
                await sw.FlushAsync().ConfigureAwait(false);
            }

            // 原子性替换目标文件
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            // 清理临时文件
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    #endregion
}
