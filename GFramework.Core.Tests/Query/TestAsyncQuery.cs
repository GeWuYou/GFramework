using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="AsyncQueryExecutorTests" /> 提供整数结果的测试异步查询。
/// </summary>
public sealed class TestAsyncQuery : AbstractAsyncQuery<TestAsyncQueryInput, int>
{
    /// <summary>
    ///     初始化 <see cref="TestAsyncQuery" /> 的新实例。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    public TestAsyncQuery(TestAsyncQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询并返回输入值的两倍。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    /// <returns>将输入值乘以 2 的结果。</returns>
    protected override Task<int> OnDoAsync(TestAsyncQueryInput input)
    {
        return Task.FromResult(input.Value * 2);
    }
}
