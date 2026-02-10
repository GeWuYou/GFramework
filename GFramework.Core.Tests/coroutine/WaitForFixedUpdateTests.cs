using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForFixedUpdate的单元测试类
/// </summary>
[TestFixture]
public class WaitForFixedUpdateTests
{
    /// <summary>
    ///     验证WaitForFixedUpdate初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForFixedUpdate_Should_Not_Be_Done_Initially()
    {
        var wait = new WaitForFixedUpdate();

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForFixedUpdate在Update后应该完成
    /// </summary>
    [Test]
    public void WaitForFixedUpdate_Should_Be_Done_After_Update()
    {
        var wait = new WaitForFixedUpdate();

        wait.Update(0.016);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForFixedUpdate多次Update后仍保持完成状态
    /// </summary>
    [Test]
    public void WaitForFixedUpdate_Should_Remain_Done_After_Multiple_Updates()
    {
        var wait = new WaitForFixedUpdate();

        wait.Update(0.016);
        Assert.That(wait.IsDone, Is.True);

        wait.Update(0.016);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForFixedUpdate实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForFixedUpdate_Should_Implement_IYieldInstruction()
    {
        var wait = new WaitForFixedUpdate();

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }
}