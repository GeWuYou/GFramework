using GFramework.Core.Abstractions.query;
using GFramework.Core.extensions;
using GFramework.Core.query;
using GFramework.Core.Tests.model;

namespace GFramework.Core.Tests.query;

/// <summary>
/// 测试查询类，用于执行测试查询操作
/// </summary>
/// <param name="input">测试查询输入参数</param>
public class TestQuery(TestQueryInput input) : AbstractQuery<TestQueryInput, int>(input)
{
    /// <summary>
    /// 执行查询操作的重写方法
    /// </summary>
    /// <param name="input">测试查询输入参数</param>
    /// <returns>返回固定的整数值42</returns>
    protected override int OnDo(TestQueryInput input)
    {
        return this.GetModel<ITestModel>()!.GetCurrentXp;
    }
}

/// <summary>
/// 测试查询输入类，实现查询输入接口
/// </summary>
public sealed class TestQueryInput : IQueryInput;