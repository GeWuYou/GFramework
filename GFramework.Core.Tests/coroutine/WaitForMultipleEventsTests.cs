using GFramework.Core.Abstractions.events;
using GFramework.Core.coroutine.instructions;
using GFramework.Core.events;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine
{
    [TestFixture]
    public class WaitForMultipleEventsTests
    {
        [SetUp]
        public void SetUp()
        {
            eventBus = new EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            (eventBus as IDisposable)?.Dispose();
        }

        private IEventBus eventBus;

        [Test]
        public void Constructor_RegistersBothEventTypes()
        {
            // Arrange & Act
            var waitForMultipleEvents = new WaitForMultipleEvents<TestEvent1, TestEvent2>(eventBus);

            // Assert
            Assert.That(waitForMultipleEvents.IsDone, Is.False);
            Assert.That(waitForMultipleEvents.TriggeredBy, Is.EqualTo(0));
        }

        [Test]
        public async Task FirstEventWins_WhenBothEventsFired()
        {
            // Arrange
            var waitForMultipleEvents = new WaitForMultipleEvents<TestEvent1, TestEvent2>(eventBus);

            // Act
            eventBus.Send(new TestEvent1 { Data = "first_event" });
            eventBus.Send(new TestEvent2 { Data = "second_event" });

            // Assert
            Assert.That(waitForMultipleEvents.IsDone, Is.True);
            Assert.That(waitForMultipleEvents.TriggeredBy, Is.EqualTo(1)); // First event should win
            Assert.That(waitForMultipleEvents.FirstEventData?.Data, Is.EqualTo("first_event"));
            Assert.That(waitForMultipleEvents.SecondEventData, Is.Null);
        }

        [Test]
        public async Task SecondEventWins_WhenOnlySecondEventFired()
        {
            // Arrange
            var waitForMultipleEvents = new WaitForMultipleEvents<TestEvent1, TestEvent2>(eventBus);

            // Act
            eventBus.Send(new TestEvent2 { Data = "second_event" });

            // Assert
            Assert.That(waitForMultipleEvents.IsDone, Is.True);
            Assert.That(waitForMultipleEvents.TriggeredBy, Is.EqualTo(2)); // Second event should win
            Assert.That(waitForMultipleEvents.SecondEventData?.Data, Is.EqualTo("second_event"));
            Assert.That(waitForMultipleEvents.FirstEventData, Is.Null);
        }

        [Test]
        public async Task FirstEventWins_WhenBothEventsFiredInReverseOrder()
        {
            // Arrange
            var waitForMultipleEvents = new WaitForMultipleEvents<TestEvent1, TestEvent2>(eventBus);

            // Act
            eventBus.Send(new TestEvent2 { Data = "second_event" });
            eventBus.Send(new TestEvent1 { Data = "first_event" });

            // Assert
            Assert.That(waitForMultipleEvents.IsDone, Is.True);
            // Actually, the second event should win because it fired first and set _done = true
            Assert.That(waitForMultipleEvents.TriggeredBy, Is.EqualTo(2)); // Second event wins since it fired first
            Assert.That(waitForMultipleEvents.SecondEventData?.Data, Is.EqualTo("second_event"));
            Assert.That(waitForMultipleEvents.FirstEventData, Is.Null);
        }

        [Test]
        public async Task MultipleEvents_AfterCompletion_DoNotOverrideState()
        {
            // Arrange
            var waitForMultipleEvents = new WaitForMultipleEvents<TestEvent1, TestEvent2>(eventBus);

            // Act - Fire first event
            eventBus.Send(new TestEvent1 { Data = "first_event" });

            // Verify first event was processed
            Assert.That(waitForMultipleEvents.IsDone, Is.True);
            Assert.That(waitForMultipleEvents.TriggeredBy, Is.EqualTo(1));
            Assert.That(waitForMultipleEvents.FirstEventData?.Data, Is.EqualTo("first_event"));

            // Fire second event after completion
            eventBus.Send(new TestEvent2 { Data = "second_event" });

            // Assert - The state should not change
            Assert.That(waitForMultipleEvents.IsDone, Is.True);
            Assert.That(waitForMultipleEvents.TriggeredBy, Is.EqualTo(1)); // Should remain as 1, not change to 2
            Assert.That(waitForMultipleEvents.FirstEventData?.Data,
                Is.EqualTo("first_event")); // Should remain unchanged
            Assert.That(waitForMultipleEvents.SecondEventData, Is.Null); // Should remain null
        }

        [Test]
        public async Task Disposal_PreventsFurtherEventHandling()
        {
            // Arrange
            var waitForMultipleEvents = new WaitForMultipleEvents<TestEvent1, TestEvent2>(eventBus);

            // Act - Dispose the instance
            waitForMultipleEvents.Dispose();

            // Fire an event after disposal
            eventBus.Send(new TestEvent1 { Data = "after_disposal" });

            // Assert - Event should not be processed due to disposal
            // Since we disposed, no event data should be captured
            Assert.That(waitForMultipleEvents.FirstEventData, Is.Null);
            Assert.That(waitForMultipleEvents.IsDone, Is.False); // Should remain false after disposal
        }

        // Test event classes
        private class TestEvent1
        {
            public string Data { get; set; } = string.Empty;
        }

        private class TestEvent2
        {
            public string Data { get; set; } = string.Empty;
        }
    }
}