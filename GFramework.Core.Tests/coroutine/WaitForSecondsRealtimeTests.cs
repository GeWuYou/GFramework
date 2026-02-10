using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForSecondsRealtime的单元测试类
/// </summary>
[TestFixture]
public class WaitForSecondsRealtimeTests
{
    /// <summary>
    ///     验证WaitForSecondsRealtime初始状态根据时间设置
    /// </summary>
    [Test]
    public void WaitForSecondsRealtime_Should_Handle_Initial_State_Correctly()
    {
        var waitZero = new WaitForSecondsRealtime(0);
        var waitPositive = new WaitForSecondsRealtime(1.0);

        Assert.That(waitZero.IsDone, Is.True);
        Assert.That(waitPositive.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForSecondsRealtime应该在指定时间后完成
    /// </summary>
    [Test]
    public void WaitForSecondsRealtime_Should_Be_Done_After_Specified_Time()
    {
        var wait = new WaitForSecondsRealtime(1.0);

        wait.Update(0.5);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.5);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForSecondsRealtime可以处理负数时间
    /// </summary>
    [Test]
    public void WaitForSecondsRealtime_Should_Handle_Negative_Time()
    {
        var wait = new WaitForSecondsRealtime(-1.0);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForSecondsRealtime多次更新累积时间
    /// </summary>
    [Test]
    public void WaitForSecondsRealtime_Should_Accumulate_Time_Over_Multiple_Updates()
    {
        var wait = new WaitForSecondsRealtime(2.0);

        wait.Update(0.5);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(1.0);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.5);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForSecondsRealtime实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForSecondsRealtime_Should_Implement_IYieldInstruction()
    {
        var wait = new WaitForSecondsRealtime(1.0);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }
}