using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForProgress的单元测试类
///     测试内容包括：
///     - 初始化和基本功能
///     - 进度回调
///     - 边界条件
///     - 异常处理
/// </summary>
[TestFixture]
public class WaitForProgressTests
{
    /// <summary>
    ///     验证WaitForProgress初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Not_Be_Done_Initially()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForProgress应该在指定时间后完成
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Be_Done_After_Duration()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        wait.Update(0.5);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.5);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForProgress应该在Update时调用进度回调
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Call_Progress_Callback_On_Update()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        wait.Update(0.25);

        Assert.That(progressValues.Count, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证进度值应该在0到1之间
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Have_Progress_Between_0_And_1()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        while (!wait.IsDone) wait.Update(0.1);

        foreach (var progress in progressValues)
        {
            Assert.That(progress, Is.GreaterThanOrEqualTo(0.0f));
            Assert.That(progress, Is.LessThanOrEqualTo(1.0f));
        }
    }

    /// <summary>
    ///     验证进度值应该随着时间增加
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Increase_Progress_Over_Time()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        wait.Update(0.25);
        Assert.That(progressValues[0], Is.EqualTo(0.25f).Within(0.01f));

        wait.Update(0.25);
        Assert.That(progressValues[1], Is.EqualTo(0.5f).Within(0.01f));

        wait.Update(0.25);
        Assert.That(progressValues[2], Is.EqualTo(0.75f).Within(0.01f));

        wait.Update(0.25);
        Assert.That(progressValues[^1], Is.EqualTo(1.0f).Within(0.01f));
    }

    /// <summary>
    ///     验证WaitForProgress应该抛出ArgumentNullException当回调为null
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Throw_ArgumentNullException_When_Callback_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitForProgress(1.0, null!));
    }

    /// <summary>
    ///     验证WaitForProgress应该抛出ArgumentException当duration为0
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Throw_ArgumentException_When_Duration_Is_Zero()
    {
        Assert.Throws<ArgumentException>(() => new WaitForProgress(0, _ => { }));
    }

    /// <summary>
    ///     验证WaitForProgress应该抛出ArgumentException当duration为负数
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Throw_ArgumentException_When_Duration_Is_Negative()
    {
        Assert.Throws<ArgumentException>(() => new WaitForProgress(-1.0, _ => { }));
    }

    /// <summary>
    ///     验证WaitForProgress应该处理超过duration的更新
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Clamp_Progress_To_1_When_Exceeding_Duration()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        wait.Update(0.5);
        wait.Update(0.5);
        wait.Update(0.5);

        Assert.That(progressValues[^1], Is.EqualTo(1.0f).Within(0.01f));
    }

    /// <summary>
    ///     验证WaitForProgress可以处理精确的duration
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Handle_Exact_Duration()
    {
        var progressValues = new List<float>();
        var duration = 1.0;
        var wait = new WaitForProgress(duration, progressValues.Add);

        wait.Update(duration);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(progressValues[^1], Is.EqualTo(1.0f).Within(0.01f));
    }

    /// <summary>
    ///     验证WaitForProgress可以处理不同的delta time
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Handle_Variable_Delta_Time()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        wait.Update(0.1);
        wait.Update(0.05);
        wait.Update(0.15);
        wait.Update(0.2);
        wait.Update(0.5);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(progressValues[^1], Is.EqualTo(1.0f).Within(0.01f));
    }

    /// <summary>
    ///     验证WaitForProgress可以处理多次Update
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Handle_Multiple_Updates()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        var updateCount = 0;
        while (!wait.IsDone && updateCount < 100)
        {
            wait.Update(0.01);
            updateCount++;
        }

        Assert.That(wait.IsDone, Is.True);
        Assert.That(progressValues.Count, Is.GreaterThan(0));
    }

    /// <summary>
    ///     验证WaitForProgress应该确保最后一个进度为1.0
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Ensure_Final_Progress_Is_1()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        while (!wait.IsDone) wait.Update(0.1);

        Assert.That(progressValues[^1], Is.EqualTo(1.0f).Within(0.01f));
    }

    /// <summary>
    ///     验证WaitForProgress实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Implement_IYieldInstruction()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    ///     验证WaitForProgress可以处理很短的duration
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Handle_Short_Duration()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(0.001, progressValues.Add);

        wait.Update(0.001);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForProgress可以处理很长的duration
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Handle_Long_Duration()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(100.0, progressValues.Add);

        wait.Update(50.0);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(50.0);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForProgress在完成前不会超过1.0
    /// </summary>
    [Test]
    public void WaitForProgress_Should_Not_Exceed_1_Before_Completion()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        wait.Update(0.9);
        Assert.That(progressValues[^1], Is.LessThan(1.0f));

        wait.Update(0.05);
        Assert.That(progressValues[^1], Is.LessThanOrEqualTo(1.0f));

        wait.Update(0.05);
        Assert.That(progressValues[^1], Is.EqualTo(1.0f).Within(0.01f));
    }

    /// <summary>
    ///     验证WaitForProgress的Update方法不影响未完成状态
    /// </summary>
    [Test]
    public void WaitForProgress_Update_Should_Not_Affect_Before_Completion()
    {
        var progressValues = new List<float>();
        var wait = new WaitForProgress(1.0, progressValues.Add);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);
    }
}