using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine
{
    [TestFixture]
    public class DelayTests
    {
        [Test]
        public void Constructor_SetsInitialRemainingTime()
        {
            // Arrange & Act
            var delay = new Delay(2.5);

            // Assert
            Assert.That(delay.IsDone, Is.False);
        }

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

        [Test]
        public void NegativeTime_TreatedAsZero()
        {
            // Arrange & Act
            var delay = new Delay(-1.0);

            // Assert
            Assert.That(delay.IsDone, Is.True);
        }

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