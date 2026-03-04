using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using GFramework.Core.Abstractions.configuration;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;

namespace GFramework.Core.configuration;

/// <summary>
///     配置管理器实现，提供线程安全的配置存储和访问
///     线程安全：所有公共方法都是线程安全的
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    /// <summary>
    ///     Key 参数验证错误消息常量
    /// </summary>
    private const string KeyCannotBeNullOrEmptyMessage = "Key cannot be null or whitespace.";

    /// <summary>
    ///     Path 参数验证错误消息常量
    /// </summary>
    private const string PathCannotBeNullOrEmptyMessage = "Path cannot be null or whitespace.";

    /// <summary>
    ///     JSON 参数验证错误消息常量
    /// </summary>
    private const string JsonCannotBeNullOrEmptyMessage = "JSON cannot be null or whitespace.";

    /// <summary>
    ///     配置存储字典（线程安全）
    /// </summary>
    private readonly ConcurrentDictionary<string, object> _configs = new();

    private readonly ILogger _logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(ConfigurationManager));

    /// <summary>
    ///     用于保护监听器列表的锁
    /// </summary>
    private readonly object _watcherLock = new();

    /// <summary>
    ///     配置监听器字典（线程安全）
    ///     键：配置键，值：监听器列表
    /// </summary>
    private readonly ConcurrentDictionary<string, List<Delegate>> _watchers = new();

    /// <summary>
    ///     获取配置数量
    /// </summary>
    public int Count => _configs.Count;

    /// <summary>
    ///     获取指定键的配置值
    /// </summary>
    public T? GetConfig<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(KeyCannotBeNullOrEmptyMessage, nameof(key));

        if (_configs.TryGetValue(key, out var value))
        {
            return ConvertValue<T>(value);
        }

        return default;
    }

    /// <summary>
    ///     获取指定键的配置值，如果不存在则返回默认值
    /// </summary>
    public T GetConfig<T>(string key, T defaultValue)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(KeyCannotBeNullOrEmptyMessage, nameof(key));

        if (_configs.TryGetValue(key, out var value))
        {
            return ConvertValue<T>(value);
        }

        return defaultValue;
    }

    /// <summary>
    ///     设置指定键的配置值
    /// </summary>
    public void SetConfig<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(KeyCannotBeNullOrEmptyMessage, nameof(key));

        var oldValue = _configs.AddOrUpdate(key, value!, (_, _) => value!);

        // 触发监听器
        if (!EqualityComparer<object>.Default.Equals(oldValue, value))
        {
            NotifyWatchers(key, value);
        }
    }

    /// <summary>
    ///     检查指定键的配置是否存在
    /// </summary>
    public bool HasConfig(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(KeyCannotBeNullOrEmptyMessage, nameof(key));

        return _configs.ContainsKey(key);
    }

    /// <summary>
    ///     移除指定键的配置
    /// </summary>
    public bool RemoveConfig(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(KeyCannotBeNullOrEmptyMessage, nameof(key));

        var removed = _configs.TryRemove(key, out _);

        if (removed)
        {
            // 移除该键的所有监听器（使用锁保护）
            lock (_watcherLock)
            {
                _watchers.TryRemove(key, out _);
            }
        }

        return removed;
    }

    /// <summary>
    ///     清空所有配置
    /// </summary>
    public void Clear()
    {
        _configs.Clear();

        // 清空监听器（使用锁保护）
        lock (_watcherLock)
        {
            _watchers.Clear();
        }
    }

    /// <summary>
    ///     监听指定键的配置变化
    /// </summary>
    public IUnRegister WatchConfig<T>(string key, Action<T> onChange)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(KeyCannotBeNullOrEmptyMessage, nameof(key));

        if (onChange == null)
            throw new ArgumentNullException(nameof(onChange));

        lock (_watcherLock)
        {
            if (!_watchers.TryGetValue(key, out var watchers))
            {
                watchers = new List<Delegate>();
                _watchers[key] = watchers;
            }

            watchers.Add(onChange);
        }

        return new ConfigWatcherUnRegister(() => UnwatchConfig(key, onChange));
    }

    /// <summary>
    ///     从 JSON 字符串加载配置
    /// </summary>
    public void LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException(JsonCannotBeNullOrEmptyMessage, nameof(json));

        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (dict == null)
            return;

        foreach (var kvp in dict)
        {
            _configs[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    ///     将配置保存为 JSON 字符串
    /// </summary>
    public string SaveToJson()
    {
        var dict = _configs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return JsonSerializer.Serialize(dict, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    ///     从文件加载配置
    /// </summary>
    public void LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: {path}");

        var json = File.ReadAllText(path);
        LoadFromJson(json);
    }

    /// <summary>
    ///     将配置保存到文件
    /// </summary>
    public void SaveToFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = SaveToJson();
        File.WriteAllText(path, json);
    }

    /// <summary>
    ///     获取所有配置键
    /// </summary>
    public IEnumerable<string> GetAllKeys()
    {
        return _configs.Keys.ToList();
    }

    /// <summary>
    ///     取消监听指定键的配置变化
    /// </summary>
    private void UnwatchConfig<T>(string key, Action<T> onChange)
    {
        lock (_watcherLock)
        {
            if (_watchers.TryGetValue(key, out var watchers))
            {
                watchers.Remove(onChange);

                if (watchers.Count == 0)
                {
                    _watchers.TryRemove(key, out _);
                }
            }
        }
    }

    /// <summary>
    ///     通知监听器配置已变化
    /// </summary>
    private void NotifyWatchers<T>(string key, T newValue)
    {
        List<Delegate>? watchersCopy = null;

        lock (_watcherLock)
        {
            if (_watchers.TryGetValue(key, out var watchers))
            {
                watchersCopy = new List<Delegate>(watchers);
            }
        }

        if (watchersCopy == null)
            return;

        foreach (var watcher in watchersCopy)
        {
            try
            {
                if (watcher is Action<T> typedWatcher)
                {
                    typedWatcher(newValue);
                }
            }
            catch (Exception ex)
            {
                // 防止监听器异常影响其他监听器
                _logger.Error(
                    $"[ConfigurationManager] Error in config watcher for key '{key}': {ex.Message}");
            }
        }
    }

    /// <summary>
    ///     转换配置值到目标类型
    /// </summary>
    private static T ConvertValue<T>(object value)
    {
        if (value is T typedValue)
        {
            return typedValue;
        }

        // 处理 JsonElement 类型
        if (value is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText())!;
        }

        // 尝试类型转换
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default!;
        }
    }
}