using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForNextFrame的单元测试类
/// </summary>
[TestFixture]
public class WaitForNextFrameTests
{
    /// <summary>
    ///     验证WaitForNextFrame初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForNextFrame_Should_Not_Be_Done_Initially()
    {
        var wait = new WaitForNextFrame();

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForNextFrame在Update后应该完成
    /// </summary>
    [Test]
    public void WaitForNextFrame_Should_Be_Done_After_Update()
    {
        var wait = new WaitForNextFrame();

        wait.Update(0.016);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForNextFrame多次Update后仍保持完成状态
    /// </summary>
    [Test]
    public void WaitForNextFrame_Should_Remain_Done_After_Multiple_Updates()
    {
        var wait = new WaitForNextFrame();

        wait.Update(0.016);
        Assert.That(wait.IsDone, Is.True);

        wait.Update(0.016);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForNextFrame实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForNextFrame_Should_Implement_IYieldInstruction()
    {
        var wait = new WaitForNextFrame();

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }
}