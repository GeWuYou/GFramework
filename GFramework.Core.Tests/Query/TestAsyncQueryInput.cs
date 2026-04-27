using GFramework.Cqrs.Abstractions.Cqrs.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="AsyncQueryExecutorTests" /> 提供输入值的测试查询输入。
/// </summary>
public sealed class TestAsyncQueryInput : IQueryInput
{
    /// <summary>
    ///     获取查询值；该值只能在对象初始化阶段设置。
    /// </summary>
    public int Value { get; init; }
}
