using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine
{
    [TestFixture]
    public class WaitWhileTests
    {
        [Test]
        public void Constructor_WithNullPredicate_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WaitWhile(null!));
        }

        [Test]
        public void IsDone_ReturnsInverseOfPredicateResult_True()
        {
            // Arrange
            var condition = true;
            var waitWhile = new WaitWhile(() => condition);

            // Act
            condition = false;

            // Assert
            Assert.That(waitWhile.IsDone, Is.True);
        }

        [Test]
        public void IsDone_ReturnsInverseOfPredicateResult_False()
        {
            // Arrange
            var condition = false;
            var waitWhile = new WaitWhile(() => condition);

            // Assert
            Assert.That(waitWhile.IsDone, Is.True); // Because !false = true
        }

        [Test]
        public void IsDone_WhenPredicateReturnsTrue()
        {
            // Arrange
            var condition = true;
            var waitWhile = new WaitWhile(() => condition);

            // Assert
            Assert.That(waitWhile.IsDone, Is.False); // Because !true = false
        }

        [Test]
        public void Update_DoesNotChangeState()
        {
            // Arrange
            var condition = true;
            var waitWhile = new WaitWhile(() => condition);

            // Act
            waitWhile.Update(0.1);

            // Assert
            Assert.That(waitWhile.IsDone, Is.False);
        }
    }
}