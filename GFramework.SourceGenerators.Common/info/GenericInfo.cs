namespace GFramework.SourceGenerators.Common.info;

/// <summary>
///     表示泛型信息的数据结构
/// </summary>
/// <param name="Parameters">泛型参数字符串</param>
/// <param name="Constraints">泛型约束列表</param>
public record struct GenericInfo(
    string Parameters,
    IReadOnlyList<string> Constraints
);