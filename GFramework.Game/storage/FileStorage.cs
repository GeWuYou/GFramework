using System.Collections.Concurrent;
using System.Text;
using GFramework.Game.Abstractions.serializer;
using GFramework.Game.Abstractions.storage;

namespace GFramework.Game.storage;

/// <summary>
/// 基于文件系统的存储实现，实现了IFileStorage接口，支持按key细粒度锁保证线程安全
/// </summary>
public sealed class FileStorage : IFileStorage
{
    private readonly string _extension;

    // 每个key对应的锁对象
    private readonly ConcurrentDictionary<string, object> _keyLocks = new();
    private readonly string _rootPath;
    private readonly ISerializer _serializer;

    /// <summary>
    /// 初始化FileStorage实例
    /// </summary>
    /// <param name="rootPath">存储根目录路径</param>
    /// <param name="serializer">序列化器实例</param>
    /// <param name="extension">存储文件的扩展名</param>
    public FileStorage(string rootPath, ISerializer serializer, string extension = ".dat")
    {
        _rootPath = rootPath;
        _serializer = serializer;
        _extension = extension;

        Directory.CreateDirectory(_rootPath);
    }

    #region Delete

    /// <summary>
    /// 删除指定键的存储项
    /// </summary>
    /// <param name="key">存储键</param>
    public void Delete(string key)
    {
        var path = ToPath(key);
        var keyLock = _keyLocks.GetOrAdd(path, _ => new object());

        lock (keyLock)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    #endregion

    /// <summary>
    /// 清理文件段字符串，将其中的无效文件名字符替换为下划线
    /// </summary>
    /// <param name="segment">需要清理的文件段字符串</param>
    /// <returns>清理后的字符串，其中所有无效文件名字符都被替换为下划线</returns>
    private static string SanitizeSegment(string segment)
    {
        return Path.GetInvalidFileNameChars().Aggregate(segment, (current, c) => current.Replace(c, '_'));
    }

    #region Helpers

    /// <summary>
    /// 将存储键转换为文件路径
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

    #region Exists

    /// <summary>
    /// 检查指定键的存储项是否存在
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>如果存储项存在则返回true，否则返回false</returns>
    public bool Exists(string key)
    {
        var path = ToPath(key);
        var keyLock = _keyLocks.GetOrAdd(path, _ => new object());

        lock (keyLock)
        {
            return File.Exists(path);
        }
    }

    /// <summary>
    /// 异步检查指定键的存储项是否存在
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>如果存储项存在则返回true，否则返回false</returns>
    public Task<bool> ExistsAsync(string key)
        => Task.FromResult(Exists(key));

    #endregion

    #region Read

    /// <summary>
    /// 读取指定键的存储项
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="FileNotFoundException">当存储键不存在时抛出</exception>
    public T Read<T>(string key)
    {
        var path = ToPath(key);
        var keyLock = _keyLocks.GetOrAdd(path, _ => new object());

        lock (keyLock)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Storage key not found: {key}", path);

            var content = File.ReadAllText(path, Encoding.UTF8);
            return _serializer.Deserialize<T>(content);
        }
    }

    /// <summary>
    /// 读取指定键的存储项，如果不存在则返回默认值
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="defaultValue">当存储键不存在时返回的默认值</param>
    /// <returns>反序列化后的对象或默认值</returns>
    public T Read<T>(string key, T defaultValue)
    {
        var path = ToPath(key);
        var keyLock = _keyLocks.GetOrAdd(path, _ => new object());

        lock (keyLock)
        {
            if (!File.Exists(path))
                return defaultValue;

            var content = File.ReadAllText(path, Encoding.UTF8);
            return _serializer.Deserialize<T>(content);
        }
    }

    /// <summary>
    /// 异步读取指定键的存储项
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="FileNotFoundException">当存储键不存在时抛出</exception>
    public async Task<T> ReadAsync<T>(string key)
    {
        var path = ToPath(key);
        var keyLock = _keyLocks.GetOrAdd(path, _ => new object());

        // 异步操作依然使用lock保护文件读写
        lock (keyLock)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Storage key not found: {key}", path);
        }

        // 读取文件内容可以使用异步IO，但要注意锁范围
        string content;
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var sr = new StreamReader(fs, Encoding.UTF8))
        {
            content = await sr.ReadToEndAsync();
        }

        return _serializer.Deserialize<T>(content);
    }

    #endregion

    #region Write

    /// <summary>
    /// 写入指定键的存储项
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="value">要存储的对象</param>
    public void Write<T>(string key, T value)
    {
        var path = ToPath(key);
        var keyLock = _keyLocks.GetOrAdd(path, _ => new object());
        var content = _serializer.Serialize(value);

        lock (keyLock)
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }
    }

    /// <summary>
    /// 异步写入指定键的存储项
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="value">要存储的对象</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task WriteAsync<T>(string key, T value)
    {
        var path = ToPath(key);
        var keyLock = _keyLocks.GetOrAdd(path, _ => new object());
        var content = _serializer.Serialize(value);

        // 异步写也需要锁
        lock (keyLock)
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var sw = new StreamWriter(fs, Encoding.UTF8);
            sw.WriteAsync(content);
        }

        await Task.CompletedTask;
    }

    #endregion
}