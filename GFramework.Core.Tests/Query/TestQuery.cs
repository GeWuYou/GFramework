using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="QueryExecutorTests" /> 提供整数结果的测试同步查询。
/// </summary>
public sealed class TestQuery : AbstractQuery<TestQueryInput, int>
{
    /// <summary>
    ///     初始化 <see cref="TestQuery" /> 的新实例。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    public TestQuery(TestQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行同步查询并返回输入值的双倍结果。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    /// <returns>输入值乘以 2 后的结果。</returns>
    protected override int OnDo(TestQueryInput input)
    {
        return input.Value * 2;
    }
}
