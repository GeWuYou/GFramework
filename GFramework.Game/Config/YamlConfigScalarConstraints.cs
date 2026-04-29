namespace GFramework.Game.Config;

/// <summary>
///     聚合一个标量节点上声明的数值约束与字符串约束。
///     该包装层保留“标量字段有约束”的统一入口，同时把不同语义的约束分成更小的专用模型。
/// </summary>
internal sealed class YamlConfigScalarConstraints
{
    /// <summary>
    ///     初始化标量约束模型。
    /// </summary>
    /// <param name="numericConstraints">数值约束分组。</param>
    /// <param name="stringConstraints">字符串约束分组。</param>
    public YamlConfigScalarConstraints(
        YamlConfigNumericConstraints? numericConstraints,
        YamlConfigStringConstraints? stringConstraints)
    {
        NumericConstraints = numericConstraints;
        StringConstraints = stringConstraints;
    }

    /// <summary>
    ///     获取数值约束分组。
    /// </summary>
    public YamlConfigNumericConstraints? NumericConstraints { get; }

    /// <summary>
    ///     获取字符串约束分组。
    /// </summary>
    public YamlConfigStringConstraints? StringConstraints { get; }
}
