using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForEndOfFrame的单元测试类
/// </summary>
[TestFixture]
public class WaitForEndOfFrameTests
{
    /// <summary>
    ///     验证WaitForEndOfFrame初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForEndOfFrame_Should_Not_Be_Done_Initially()
    {
        var wait = new WaitForEndOfFrame();

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForEndOfFrame在Update后应该完成
    /// </summary>
    [Test]
    public void WaitForEndOfFrame_Should_Be_Done_After_Update()
    {
        var wait = new WaitForEndOfFrame();

        wait.Update(0.016);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForEndOfFrame多次Update后仍保持完成状态
    /// </summary>
    [Test]
    public void WaitForEndOfFrame_Should_Remain_Done_After_Multiple_Updates()
    {
        var wait = new WaitForEndOfFrame();

        wait.Update(0.016);
        Assert.That(wait.IsDone, Is.True);

        wait.Update(0.016);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForEndOfFrame实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForEndOfFrame_Should_Implement_IYieldInstruction()
    {
        var wait = new WaitForEndOfFrame();

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }
}