using System.Collections.Concurrent;
using System.Text;
using GFramework.Core.Abstractions.storage;
using GFramework.Game.Abstractions.serializer;
using GFramework.Godot.extensions;
using FileAccess = Godot.FileAccess;

namespace GFramework.Godot.storage;

/// <summary>
/// Godot 特化的文件存储实现，支持 res://、user:// 和普通文件路径
/// 支持按 key 细粒度锁保证线程安全
/// </summary>
public sealed class GodotFileStorage : IStorage
{
    /// <summary>
    /// 每个 key 对应的锁对象
    /// </summary>
    private readonly ConcurrentDictionary<string, object> _keyLocks = new();

    private readonly ISerializer _serializer;

    /// <summary>
    /// 初始化 Godot 文件存储
    /// </summary>
    /// <param name="serializer">序列化器实例</param>
    public GodotFileStorage(ISerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    #region Delete

    /// <summary>
    /// 删除指定键对应的文件
    /// </summary>
    /// <param name="key">存储键</param>
    public void Delete(string key)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 清理路径段中的无效字符，将无效文件名字符替换为下划线
    /// </summary>
    /// <param name="segment">要清理的路径段</param>
    /// <returns>清理后的路径段</returns>
    private static string SanitizeSegment(string segment)
        => Path.GetInvalidFileNameChars().Aggregate(segment, (current, c) => current.Replace(c, '_'));

    /// <summary>
    /// 将存储键转换为绝对路径，处理 Godot 虚拟路径和普通文件系统路径
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

    /// <summary>
    /// 获取指定路径对应的锁对象，如果不存在则创建新的锁对象
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>对应路径的锁对象</returns>
    private object GetLock(string path) => _keyLocks.GetOrAdd(path, _ => new object());

    #endregion

    #region Exists

    /// <summary>
    /// 检查指定键对应的文件是否存在
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>文件存在返回 true，否则返回 false</returns>
    public bool Exists(string key)
    {
        var path = ToAbsolutePath(key);
        var keyLock = GetLock(path);

        lock (keyLock)
        {
            if (!path.IsGodotPath()) return File.Exists(path);
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            return file != null;
        }
    }

    /// <summary>
    /// 异步检查指定键对应的文件是否存在
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>表示异步操作的任务，结果为布尔值表示文件是否存在</returns>
    public Task<bool> ExistsAsync(string key)
        => Task.FromResult(Exists(key));

    #endregion

    #region Read

    /// <summary>
    /// 读取指定键对应的序列化数据并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <returns>反序列化后的对象实例</returns>
    /// <exception cref="FileNotFoundException">当指定键对应的文件不存在时抛出</exception>
    public T Read<T>(string key)
    {
        var path = ToAbsolutePath(key);
        var keyLock = GetLock(path);

        lock (keyLock)
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
                content = File.ReadAllText(path, Encoding.UTF8);
            }

            return _serializer.Deserialize<T>(content);
        }
    }

    /// <summary>
    /// 读取指定键对应的序列化数据，如果文件不存在则返回默认值
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="defaultValue">当文件不存在时返回的默认值</param>
    /// <returns>反序列化后的对象实例或默认值</returns>
    public T Read<T>(string key, T defaultValue)
    {
        var path = ToAbsolutePath(key);
        var keyLock = GetLock(path);

        lock (keyLock)
        {
            if (path.IsGodotPath() && !FileAccess.FileExists(path) || !path.IsGodotPath() && !File.Exists(path))
                return defaultValue;

            return Read<T>(key);
        }
    }

    /// <summary>
    /// 异步读取指定键对应的序列化数据并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">要反序列化的类型</typeparam>
    /// <param name="key">存储键</param>
    /// <returns>表示异步操作的任务，结果为反序列化后的对象实例</returns>
    public async Task<T> ReadAsync<T>(string key)
    {
        var path = ToAbsolutePath(key);
        var keyLock = GetLock(path);

        return await Task.Run(() =>
        {
            lock (keyLock)
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
                    content = File.ReadAllText(path, Encoding.UTF8);
                }

                return _serializer.Deserialize<T>(content);
            }
        });
    }

    #endregion

    #region Write

    /// <summary>
    /// 将指定对象序列化并写入到指定键对应的文件中
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="value">要写入的对象实例</param>
    public void Write<T>(string key, T value)
    {
        var path = ToAbsolutePath(key);
        var keyLock = GetLock(path);

        lock (keyLock)
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
                File.WriteAllText(path, content, Encoding.UTF8);
            }
        }
    }

    /// <summary>
    /// 异步将指定对象序列化并写入到指定键对应的文件中
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="value">要写入的对象实例</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task WriteAsync<T>(string key, T value)
    {
        await Task.Run(() => Write(key, value));
    }

    #endregion
}