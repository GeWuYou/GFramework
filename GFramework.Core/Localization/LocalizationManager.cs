using System.Globalization;
using System.IO;
using System.Text.Json;
using GFramework.Core.Abstractions.Localization;
using GFramework.Core.Systems;

namespace GFramework.Core.Localization;

/// <summary>
/// 本地化管理器实现
/// </summary>
public class LocalizationManager : AbstractSystem, ILocalizationManager
{
    private readonly LocalizationConfig _config;
    private readonly Dictionary<string, ILocalizationFormatter> _formatters;
    private readonly List<Action<string>> _languageChangeCallbacks;
    private readonly Dictionary<string, Dictionary<string, ILocalizationTable>> _tables;
    private List<string> _availableLanguages;
    private CultureInfo _currentCulture;
    private string _currentLanguage;

    /// <summary>
    /// 初始化本地化管理器
    /// </summary>
    /// <param name="config">配置</param>
    public LocalizationManager(LocalizationConfig? config = null)
    {
        _config = config ?? new LocalizationConfig();
        _tables = new Dictionary<string, Dictionary<string, ILocalizationTable>>();
        _formatters = new Dictionary<string, ILocalizationFormatter>();
        _languageChangeCallbacks = new List<Action<string>>();
        _currentLanguage = _config.DefaultLanguage;
        _currentCulture = GetCultureInfo(_currentLanguage);
        _availableLanguages = new List<string>();
    }

    /// <inheritdoc/>
    public string CurrentLanguage => _currentLanguage;

    /// <inheritdoc/>
    public CultureInfo CurrentCulture => _currentCulture;

    /// <inheritdoc/>
    public IReadOnlyList<string> AvailableLanguages => _availableLanguages;

    /// <inheritdoc/>
    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            throw new ArgumentNullException(nameof(languageCode));
        }

        if (_currentLanguage == languageCode)
        {
            return;
        }

        LoadLanguage(languageCode);
        _currentLanguage = languageCode;
        _currentCulture = GetCultureInfo(languageCode);

        // 触发语言变化回调
        TriggerLanguageChange();
    }

    /// <inheritdoc/>
    public ILocalizationTable GetTable(string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            throw new ArgumentNullException(nameof(tableName));
        }

        if (!_tables.TryGetValue(_currentLanguage, out var languageTables))
        {
            throw new LocalizationTableNotFoundException(tableName);
        }

        if (!languageTables.TryGetValue(tableName, out var table))
        {
            throw new LocalizationTableNotFoundException(tableName);
        }

        return table;
    }

    /// <inheritdoc/>
    public string GetText(string table, string key)
    {
        return GetTable(table).GetRawText(key);
    }

    /// <inheritdoc/>
    public ILocalizationString GetString(string table, string key)
    {
        return new LocalizationString(this, table, key);
    }

    /// <inheritdoc/>
    public bool TryGetText(string table, string key, out string text)
    {
        try
        {
            text = GetText(table, key);
            return true;
        }
        catch (LocalizationException)
        {
            // 只捕获本地化相关的异常（键不存在、表不存在等）
            text = string.Empty;
            return false;
        }
    }

    /// <inheritdoc/>
    public void RegisterFormatter(string name, ILocalizationFormatter formatter)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        _formatters[name] = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <inheritdoc/>
    public ILocalizationFormatter? GetFormatter(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return _formatters.TryGetValue(name, out var formatter) ? formatter : null;
    }

    /// <inheritdoc/>
    public void SubscribeToLanguageChange(Action<string> callback)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        if (!_languageChangeCallbacks.Contains(callback))
        {
            _languageChangeCallbacks.Add(callback);
        }
    }

    /// <inheritdoc/>
    public void UnsubscribeFromLanguageChange(Action<string> callback)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        _languageChangeCallbacks.Remove(callback);
    }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        // 扫描可用语言
        ScanAvailableLanguages();

        // 加载默认语言
        LoadLanguage(_config.DefaultLanguage);
    }

    /// <inheritdoc/>
    protected override void OnDestroy()
    {
        _tables.Clear();
        _formatters.Clear();
        _languageChangeCallbacks.Clear();
    }

    /// <summary>
    /// 扫描可用语言
    /// </summary>
    private void ScanAvailableLanguages()
    {
        _availableLanguages.Clear();

        var localizationPath = _config.LocalizationPath;
        if (!Directory.Exists(localizationPath))
        {
            _availableLanguages.Add(_config.DefaultLanguage);
            return;
        }

        var directories = Directory.GetDirectories(localizationPath);
        foreach (var dir in directories)
        {
            var languageCode = Path.GetFileName(dir);
            if (!string.IsNullOrEmpty(languageCode))
            {
                _availableLanguages.Add(languageCode);
            }
        }

        if (_availableLanguages.Count == 0)
        {
            _availableLanguages.Add(_config.DefaultLanguage);
        }
    }

    /// <summary>
    /// 加载语言
    /// </summary>
    private void LoadLanguage(string languageCode)
    {
        if (_tables.ContainsKey(languageCode))
        {
            return; // 已加载
        }

        var languageTables = new Dictionary<string, ILocalizationTable>();

        // 加载回退语言（如果不是默认语言）
        Dictionary<string, ILocalizationTable>? fallbackTables = null;
        if (languageCode != _config.FallbackLanguage)
        {
            LoadLanguage(_config.FallbackLanguage);
            _tables.TryGetValue(_config.FallbackLanguage, out fallbackTables);
        }

        // 加载目标语言
        var languagePath = Path.Combine(_config.LocalizationPath, languageCode);
        if (Directory.Exists(languagePath))
        {
            var jsonFiles = Directory.GetFiles(languagePath, "*.json");
            foreach (var file in jsonFiles)
            {
                var tableName = Path.GetFileNameWithoutExtension(file);
                var data = LoadJsonFile(file);

                ILocalizationTable? fallback = null;
                fallbackTables?.TryGetValue(tableName, out fallback);

                languageTables[tableName] = new LocalizationTable(tableName, languageCode, data, fallback);
            }
        }

        _tables[languageCode] = languageTables;
    }

    /// <summary>
    /// 加载 JSON 文件
    /// </summary>
    private static Dictionary<string, string> LoadJsonFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return data ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// 获取文化信息
    /// </summary>
    private static CultureInfo GetCultureInfo(string languageCode)
    {
        try
        {
            // 尝试映射常见的语言代码
            var cultureCode = languageCode switch
            {
                "eng" => "en-US",
                "zhs" => "zh-CN",
                "zht" => "zh-TW",
                "jpn" => "ja-JP",
                "kor" => "ko-KR",
                "fra" => "fr-FR",
                "deu" => "de-DE",
                "spa" => "es-ES",
                "rus" => "ru-RU",
                _ => languageCode
            };

            return new CultureInfo(cultureCode);
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }

    /// <summary>
    /// 触发语言变化事件
    /// </summary>
    private void TriggerLanguageChange()
    {
        foreach (var callback in _languageChangeCallbacks.ToList())
        {
            try
            {
                callback(_currentLanguage);
            }
            catch
            {
                // 忽略回调异常
            }
        }
    }
}