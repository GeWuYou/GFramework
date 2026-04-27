using GFramework.Cqrs.Abstractions.Cqrs.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="QueryExecutorTests" /> 提供输入值的测试查询输入。
/// </summary>
public sealed class TestQueryInput : IQueryInput
{
    /// <summary>
    ///     获取或设置查询值。
    /// </summary>
    public int Value { get; init; }
}
