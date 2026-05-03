// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using GFramework.Core.Abstractions.Localization;

namespace GFramework.Core.Localization;

/// <summary>
/// 本地化字符串实现
/// </summary>
public class LocalizationString : ILocalizationString
{
    /// <summary>
    /// 正则分组名：变量名。
    /// </summary>
    private const string VariableGroupName = "variable";

    /// <summary>
    /// 正则分组名：格式化器名。
    /// </summary>
    private const string FormatterGroupName = "formatter";

    /// <summary>
    /// 正则分组名：格式化器参数。
    /// </summary>
    private const string FormatterArgsGroupName = "args";

    /// <summary>
    /// 匹配 {variableName} 或 {variableName:formatter:args} 的正则表达式模式
    /// </summary>
    private const string FormatVariablePattern =
        @"\{(?<variable>[a-zA-Z_][a-zA-Z0-9_]*)(?::(?<formatter>[a-zA-Z_][a-zA-Z0-9_]*)(?::(?<args>[^}]+))?)?\}";

    /// <summary>
    /// 预编译的静态正则表达式，用于格式化字符串中的变量替换
    /// </summary>
    private static readonly Regex FormatVariableRegex =
        new(
            FormatVariablePattern,
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
            TimeSpan.FromSeconds(1));

    private readonly ILocalizationManager _manager;
    private readonly Dictionary<string, object> _variables;

    /// <summary>
    /// 初始化本地化字符串
    /// </summary>
    /// <param name="manager">本地化管理器实例</param>
    /// <param name="table">本地化表名，用于定位本地化资源表</param>
    /// <param name="key">本地化键名，用于在表中定位具体的本地化文本</param>
    /// <exception cref="ArgumentNullException">当 manager、table 或 key 为 null 时抛出</exception>
    public LocalizationString(ILocalizationManager manager, string table, string key)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        Table = table ?? throw new ArgumentNullException(nameof(table));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        _variables = new Dictionary<string, object>(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public string Table { get; }

    /// <inheritdoc/>
    public string Key { get; }

    /// <summary>
    /// 添加单个变量到本地化字符串中
    /// </summary>
    /// <param name="name">变量名称，用于在模板中匹配对应的占位符</param>
    /// <param name="value">变量值，将被转换为字符串并替换到对应位置</param>
    /// <returns>返回当前的 LocalizationString 实例，支持链式调用</returns>
    /// <exception cref="ArgumentNullException">当 name 为 null 时抛出</exception>
    public ILocalizationString WithVariable(string name, object value)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        _variables[name] = value;
        return this;
    }

    /// <summary>
    /// 批量添加多个变量到本地化字符串中
    /// </summary>
    /// <param name="variables">变量元组数组，每个元组包含变量名称和对应的值</param>
    /// <returns>返回当前的 LocalizationString 实例，支持链式调用</returns>
    /// <exception cref="ArgumentNullException">当 variables 为 null 时抛出</exception>
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

    /// <summary>
    /// 格式化本地化字符串，将模板中的变量占位符替换为实际值
    /// </summary>
    /// <returns>格式化后的完整字符串。如果本地化文本不存在，则返回 "[Table.Key]" 格式的占位符</returns>
    /// <remarks>
    /// 支持两种格式：
    /// 1. {variableName} - 简单变量替换
    /// 2. {variableName:formatter:args} - 使用格式化器进行格式化
    /// </remarks>
    public string Format()
    {
        var rawText = GetRaw();
        return FormatString(rawText, _variables, _manager);
    }

    /// <summary>
    /// 获取原始的本地化文本，不进行任何变量替换
    /// </summary>
    /// <returns>本地化文本。如果在本地化管理器中未找到对应的文本，则返回 "[Table.Key]" 格式的占位符</returns>
    public string GetRaw()
    {
        if (!_manager.TryGetText(Table, Key, out var text))
        {
            return $"[{Table}.{Key}]";
        }

        return text;
    }

    /// <summary>
    /// 检查当前本地化键是否存在于本地化管理器中
    /// </summary>
    /// <returns>如果存在返回 true；否则返回 false</returns>
    public bool Exists()
    {
        return _manager.TryGetText(Table, Key, out _);
    }

    /// <summary>
    /// 格式化字符串（支持变量替换和格式化器）
    /// </summary>
    /// <param name="template">包含占位符的模板字符串</param>
    /// <param name="variables">包含变量名称和值的字典</param>
    /// <param name="manager">本地化管理器实例，用于获取格式化器</param>
    /// <returns>格式化后的字符串。如果模板为空或 null，则直接返回原模板</returns>
    private static string FormatString(
        string template,
        Dictionary<string, object> variables,
        ILocalizationManager manager)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        // 使用预编译的静态正则表达式匹配 {variableName} 或 {variableName:formatter:args}
        return FormatVariableRegex.Replace(template, match => FormatMatch(match, variables, manager));
    }

    /// <summary>
    /// 处理单个正则表达式匹配项，根据是否有格式化器决定如何处理变量值
    /// </summary>
    /// <param name="match">正则表达式匹配结果</param>
    /// <param name="variables">变量字典</param>
    /// <param name="manager">本地化管理器实例</param>
    /// <returns>替换后的字符串。如果变量不存在则返回原始匹配值；如果有格式化器则尝试格式化，失败则使用默认格式化</returns>
    private static string FormatMatch(
        Match match,
        Dictionary<string, object> variables,
        ILocalizationManager manager)
    {
        var variableName = match.Groups[VariableGroupName].Value;
        if (!variables.TryGetValue(variableName, out var value))
        {
            return match.Value;
        }

        var formatterName = GetOptionalGroupValue(match, FormatterGroupName);
        if (string.IsNullOrEmpty(formatterName))
        {
            return FormatValue(value, manager);
        }

        return TryFormatValue(match, value, formatterName, manager, out var result)
            ? result
            : FormatValue(value, manager);
    }

    /// <summary>
    /// 尝试使用指定的格式化器格式化变量值
    /// </summary>
    /// <param name="match">正则表达式匹配结果，用于获取格式化参数</param>
    /// <param name="value">要格式化的变量值</param>
    /// <param name="formatterName">格式化器名称</param>
    /// <param name="manager">本地化管理器实例</param>
    /// <param name="result">格式化后的结果字符串</param>
    /// <returns>如果格式化成功返回 true；否则返回 false，此时 result 为空字符串</returns>
    private static bool TryFormatValue(
        Match match,
        object value,
        string formatterName,
        ILocalizationManager manager,
        out string result)
    {
        var formatterArgs = GetOptionalGroupValue(match, FormatterArgsGroupName) ?? string.Empty;
        if (GetFormatter(manager, formatterName) is { } formatter &&
            formatter.TryFormat(formatterArgs, value, manager.CurrentCulture, out result))
        {
            return true;
        }

        result = string.Empty;
        return false;
    }

    /// <summary>
    /// 对变量值进行默认格式化，不使用自定义格式化器
    /// </summary>
    /// <param name="value">要格式化的值</param>
    /// <param name="manager">本地化管理器实例，提供当前文化信息</param>
    /// <returns>格式化后的字符串。如果值实现 IFormattable 接口则使用其 ToString 方法，否则调用默认的 ToString 方法</returns>
    private static string FormatValue(object value, ILocalizationManager manager)
    {
        return value switch
        {
            IFormattable formattable => formattable.ToString(null, manager.CurrentCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// 获取正则表达式匹配组中的可选值
    /// </summary>
    /// <param name="match">正则表达式匹配结果</param>
    /// <param name="groupName">要获取的命名组</param>
    /// <returns>如果该组匹配成功则返回其值；否则返回 null</returns>
    private static string? GetOptionalGroupValue(Match match, string groupName)
    {
        return match.Groups[groupName].Success ? match.Groups[groupName].Value : null;
    }

    /// <summary>
    /// 从本地化管理器获取指定名称的格式化器
    /// </summary>
    /// <param name="manager">本地化管理器实例</param>
    /// <param name="name">格式化器名称</param>
    /// <returns>如果找到对应的格式化器则返回；否则返回 null</returns>
    private static ILocalizationFormatter? GetFormatter(ILocalizationManager manager, string name)
    {
        return manager.GetFormatter(name);
    }
}
