using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="AsyncQueryExecutorTests" /> 提供字符串结果的测试异步查询。
/// </summary>
public sealed class TestAsyncStringQuery : AbstractAsyncQuery<TestAsyncQueryInput, string>
{
    /// <summary>
    ///     初始化 <see cref="TestAsyncStringQuery" /> 的新实例。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    public TestAsyncStringQuery(TestAsyncQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询并返回格式化的字符串结果。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    /// <returns>包含双倍值的格式化字符串。</returns>
    protected override Task<string> OnDoAsync(TestAsyncQueryInput input)
    {
        return Task.FromResult($"Result: {input.Value * 2}");
    }
}
