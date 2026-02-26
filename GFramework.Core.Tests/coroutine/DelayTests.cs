using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine
{
    /// <summary>
    ///     Delay类的单元测试，用于验证延迟指令的功能
    /// </summary>
    [TestFixture]
    public class DelayTests
    {
        /// <summary>
        ///     测试构造函数设置初始剩余时间
        /// </summary>
        [Test]
        public void Constructor_SetsInitialRemainingTime()
        {
            // Arrange & Act
            var delay = new Delay(2.5);

            // Assert
            Assert.That(delay.IsDone, Is.False);
        }

        /// <summary>
        ///     测试Update方法减少剩余时间
        /// </summary>
        [Test]
        public void Update_ReducesRemainingTime()
        {
            // Arrange
            var delay = new Delay(2.0);

            // Act
            delay.Update(0.5);

            // Assert
            Assert.That(delay.IsDone, Is.False);
        }

        /// <summary>
        ///     测试多次Update后最终完成
        /// </summary>
        [Test]
        public void Update_MultipleTimes_EventuallyCompletes()
        {
            // Arrange
            var delay = new Delay(1.0);

            // Act
            delay.Update(0.5);
            delay.Update(0.6); // Total: 1.1 > 1.0, so should be done

            // Assert
            Assert.That(delay.IsDone, Is.True);
        }

        /// <summary>
        ///     测试负数时间被视为零
        /// </summary>
        [Test]
        public void NegativeTime_TreatedAsZero()
        {
            // Arrange & Act
            var delay = new Delay(-1.0);

            // Assert
            Assert.That(delay.IsDone, Is.True);
        }

        /// <summary>
        ///     测试零时间立即完成
        /// </summary>
        [Test]
        public void ZeroTime_CompletesImmediately()
        {
            // Arrange & Act
            var delay = new Delay(0.0);

            // Assert
            Assert.That(delay.IsDone, Is.True);
        }
    }
}