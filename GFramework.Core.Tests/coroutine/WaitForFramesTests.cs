using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine
{
    [TestFixture]
    public class WaitForFramesTests
    {
        [Test]
        public void Constructor_SetsInitialFrameCount()
        {
            // Arrange & Act
            var waitForFrames = new WaitForFrames(3);

            // Assert
            Assert.That(waitForFrames.IsDone, Is.False);
        }

        [Test]
        public void Update_ReducesFrameCount()
        {
            // Arrange
            var waitForFrames = new WaitForFrames(2);

            // Act
            waitForFrames.Update(0.1);

            // Assert
            Assert.That(waitForFrames.IsDone, Is.False);
        }

        [Test]
        public void MultipleUpdates_EventuallyCompletes()
        {
            // Arrange
            var waitForFrames = new WaitForFrames(2);

            // Act
            waitForFrames.Update(0.1); // 2-1 = 1 frame remaining
            waitForFrames.Update(0.1); // 1-1 = 0 frames remaining

            // Assert
            Assert.That(waitForFrames.IsDone, Is.True);
        }

        [Test]
        public void NegativeFrameCount_TreatedAsOne()
        {
            // Arrange & Act
            var waitForFrames = new WaitForFrames(-1);

            // Assert
            Assert.That(waitForFrames.IsDone, Is.False);

            // One update should complete it
            waitForFrames.Update(0.1);
            Assert.That(waitForFrames.IsDone, Is.True);
        }

        [Test]
        public void ZeroFrameCount_TreatedAsOne()
        {
            // Arrange & Act
            var waitForFrames = new WaitForFrames(0);

            // Assert
            Assert.That(waitForFrames.IsDone, Is.False);

            // One update should complete it
            waitForFrames.Update(0.1);
            Assert.That(waitForFrames.IsDone, Is.True);
        }
    }
}