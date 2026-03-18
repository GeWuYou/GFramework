using System.Text.RegularExpressions;
using GFramework.Core.Abstractions.Localization;

namespace GFramework.Core.Localization;

/// <summary>
/// 本地化字符串实现
/// </summary>
public class LocalizationString : ILocalizationString
{
    private readonly ILocalizationManager _manager;
    private readonly Dictionary<string, object> _variables;

    /// <summary>
    /// 初始化本地化字符串
    /// </summary>
    /// <param name="manager">本地化管理器</param>
    /// <param name="table">表名</param>
    /// <param name="key">键名</param>
    public LocalizationString(ILocalizationManager manager, string table, string key)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        Table = table ?? throw new ArgumentNullException(nameof(table));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        _variables = new Dictionary<string, object>();
    }

    /// <inheritdoc/>
    public string Table { get; }

    /// <inheritdoc/>
    public string Key { get; }

    /// <inheritdoc/>
    public ILocalizationString WithVariable(string name, object value)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        _variables[name] = value;
        return this;
    }

    /// <inheritdoc/>
    public ILocalizationString WithVariables(params (string name, object value)[] variables)
    {
        if (variables == null)
        {
            throw new ArgumentNullException(nameof(variables));
        }

        foreach (var (name, value) in variables)
        {
            WithVariable(name, value);
        }

        return this;
    }

    /// <inheritdoc/>
    public string Format()
    {
        var rawText = GetRaw();
        return FormatString(rawText, _variables, _manager);
    }

    /// <inheritdoc/>
    public string GetRaw()
    {
        if (!_manager.TryGetText(Table, Key, out var text))
        {
            return $"[{Table}.{Key}]";
        }

        return text;
    }

    /// <inheritdoc/>
    public bool Exists()
    {
        return _manager.TryGetText(Table, Key, out _);
    }

    /// <summary>
    /// 格式化字符串（支持变量替换和格式化器）
    /// </summary>
    private static string FormatString(
        string template,
        Dictionary<string, object> variables,
        ILocalizationManager manager)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        // 匹配 {variableName} 或 {variableName:formatter:args}
        var pattern = @"\{([a-zA-Z_][a-zA-Z0-9_]*)(?::([a-zA-Z_][a-zA-Z0-9_]*)(?::([^}]+))?)?\}";
        var regex = new Regex(pattern);

        return regex.Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            var formatterName = match.Groups[2].Success ? match.Groups[2].Value : null;
            var formatterArgs = match.Groups[3].Success ? match.Groups[3].Value : null;

            if (!variables.TryGetValue(variableName, out var value))
            {
                return match.Value; // 保持原样
            }

            // 如果没有格式化器，直接转换为字符串
            if (string.IsNullOrEmpty(formatterName))
            {
                return value switch
                {
                    IFormattable formattable => formattable.ToString(null, manager.CurrentCulture),
                    _ => value?.ToString() ?? string.Empty
                };
            }

            // 尝试使用注册的格式化器
            var formatter = GetFormatter(manager, formatterName);
            if (formatter != null && formatter.TryFormat(formatterArgs ?? string.Empty, value, manager.CurrentCulture,
                    out var result))
            {
                return result;
            }

            // 格式化失败，返回基本格式化
            return value switch
            {
                IFormattable formattable => formattable.ToString(null, manager.CurrentCulture),
                _ => value?.ToString() ?? string.Empty
            };
        });
    }

    /// <summary>
    /// 获取格式化器
    /// </summary>
    private static ILocalizationFormatter? GetFormatter(ILocalizationManager manager, string name)
    {
        return manager.GetFormatter(name);
    }
}