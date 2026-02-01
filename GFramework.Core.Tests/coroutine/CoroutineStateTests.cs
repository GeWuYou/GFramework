using GFramework.Core.Abstractions.coroutine;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     协程状态枚举的单元测试类
///     测试内容包括：
///     - 枚举值存在性验证
///     - 枚举值正确性
/// </summary>
[TestFixture]
public class CoroutineStateTests
{
    /// <summary>
    ///     验证协程状态枚举包含所有预期值
    /// </summary>
    [Test]
    public void CoroutineState_Should_Have_All_Expected_Values()
    {
        var values = Enum.GetValues<CoroutineState>();

        Assert.That(values, Has.Length.EqualTo(4), "CoroutineState should have 4 values");
        Assert.That(values.Contains(CoroutineState.Running), Is.True, "Should contain Running");
        Assert.That(values.Contains(CoroutineState.Paused), Is.True, "Should contain Paused");
        Assert.That(values.Contains(CoroutineState.Completed), Is.True, "Should contain Completed");
        Assert.That(values.Contains(CoroutineState.Cancelled), Is.True, "Should contain Cancelled");
    }

    /// <summary>
    ///     验证枚举基础值为整数类型
    /// </summary>
    [Test]
    public void CoroutineState_Should_Be_Integer_Based_Enum()
    {
        var runningValue = (int)CoroutineState.Running;
        var pausedValue = (int)CoroutineState.Paused;
        var completedValue = (int)CoroutineState.Completed;
        var cancelledValue = (int)CoroutineState.Cancelled;

        Assert.That(runningValue, Is.EqualTo(0));
        Assert.That(pausedValue, Is.EqualTo(1));
        Assert.That(completedValue, Is.EqualTo(2));
        Assert.That(cancelledValue, Is.EqualTo(3));
    }
}