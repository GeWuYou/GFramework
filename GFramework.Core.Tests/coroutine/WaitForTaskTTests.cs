using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine
{
    [TestFixture]
    public class WaitForTaskTTests
    {
        [Test]
        public void Constructor_WithNullTask_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WaitForTask<string>(null!));
        }

        [Test]
        public async Task Constructor_WithCompletedTask_IsDoneImmediately()
        {
            // Arrange
            var completedTask = Task.FromResult("test");

            // Act
            var waitForTask = new WaitForTask<string>(completedTask);

            // Assert
            Assert.That(waitForTask.IsDone, Is.True);
            Assert.That(waitForTask.Result, Is.EqualTo("test"));
        }

        [Test]
        public async Task Constructor_WithIncompleteTask_IsNotDoneInitially()
        {
            // Arrange
            var tcs = new TaskCompletionSource<string>();
            var incompleteTask = tcs.Task;

            // Act
            var waitForTask = new WaitForTask<string>(incompleteTask);

            // Assert
            Assert.That(waitForTask.IsDone, Is.False);
        }

        [Test]
        public async Task TaskCompletes_CallbackSetsDoneFlag()
        {
            // Arrange
            var tcs = new TaskCompletionSource<string>();
            var task = tcs.Task;
            var waitForTask = new WaitForTask<string>(task);

            // Assert initial state
            Assert.That(waitForTask.IsDone, Is.False);

            // Act
            tcs.SetResult("completed");
            await Task.Delay(10); // Allow time for continuation

            // Assert final state
            Assert.That(waitForTask.IsDone, Is.True);
            Assert.That(waitForTask.Result, Is.EqualTo("completed"));
        }

        [Test]
        public async Task Update_DoesNotChangeState()
        {
            // Arrange
            var completedTask = Task.FromResult("test");
            var waitForTask = new WaitForTask<string>(completedTask);

            // Act
            waitForTask.Update(0.1);

            // Assert
            Assert.That(waitForTask.IsDone, Is.True);
        }

        [Test]
        public async Task TaskWithException_HoldsException()
        {
            // Arrange
            var tcs = new TaskCompletionSource<string>();
            var task = tcs.Task;
            var waitForTask = new WaitForTask<string>(task);

            // Act
            tcs.SetException(new InvalidOperationException("Test exception"));
            await Task.Delay(10); // Allow time for continuation

            // Assert
            Assert.That(waitForTask.IsDone, Is.True);
            Assert.That(waitForTask.Exception, Is.Not.Null);
            Assert.That(waitForTask.Exception?.InnerException, Is.TypeOf<InvalidOperationException>());
        }
    }
}