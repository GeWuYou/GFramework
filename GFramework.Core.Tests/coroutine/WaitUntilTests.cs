using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine
{
    [TestFixture]
    public class WaitUntilTests
    {
        [Test]
        public void Constructor_WithNullPredicate_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WaitUntil(null!));
        }

        [Test]
        public void IsDone_ReturnsPredicateResult_True()
        {
            // Arrange
            var condition = false;
            var waitUntil = new WaitUntil(() => condition);

            // Act
            condition = true;

            // Assert
            Assert.That(waitUntil.IsDone, Is.True);
        }

        [Test]
        public void IsDone_ReturnsPredicateResult_False()
        {
            // Arrange
            var condition = false;
            var waitUntil = new WaitUntil(() => condition);

            // Assert
            Assert.That(waitUntil.IsDone, Is.False);
        }

        [Test]
        public void Update_DoesNotChangeState()
        {
            // Arrange
            var condition = false;
            var waitUntil = new WaitUntil(() => condition);

            // Act
            waitUntil.Update(0.1);

            // Assert
            Assert.That(waitUntil.IsDone, Is.False);
        }
    }
}