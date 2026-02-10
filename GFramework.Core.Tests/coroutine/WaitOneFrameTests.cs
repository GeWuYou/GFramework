using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine
{
    [TestFixture]
    public class WaitOneFrameTests
    {
        [Test]
        public void Constructor_CreatesInstance()
        {
            // Act
            var waitOneFrame = new WaitOneFrame();

            // Assert
            Assert.That(waitOneFrame.IsDone, Is.False);
        }

        [Test]
        public void Update_MakesItDone()
        {
            // Arrange
            var waitOneFrame = new WaitOneFrame();

            // Act
            waitOneFrame.Update(0.1);

            // Assert
            Assert.That(waitOneFrame.IsDone, Is.True);
        }

        [Test]
        public void Update_MultipleTimes_RemainsDone()
        {
            // Arrange
            var waitOneFrame = new WaitOneFrame();

            // Act
            waitOneFrame.Update(0.1);
            waitOneFrame.Update(0.1);

            // Assert
            Assert.That(waitOneFrame.IsDone, Is.True);
        }
    }
}