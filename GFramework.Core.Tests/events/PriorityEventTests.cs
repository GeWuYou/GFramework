using GFramework.Core.Abstractions.events;
using GFramework.Core.events;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

/// <summary>
///     测试 PriorityEvent 的线程安全性和边界情况
/// </summary>
[TestFixture]
public class PriorityEventTests
{
    [Test]
    public void Trigger_Should_Not_Throw_When_Handler_Unregisters_Itself()
    {
        // Arrange
        var evt = new PriorityEvent<int>();
        IUnRegister? unregister = null;

        unregister = evt.Register(x => { unregister?.UnRegister(); });

        // Act & Assert
        Assert.DoesNotThrow(() => evt.Trigger(42));
    }

    [Test]
    public void Trigger_Should_Be_Thread_Safe()
    {
        // Arrange
        var evt = new PriorityEvent<int>();
        var counter = 0;
        evt.Register(x => Interlocked.Increment(ref counter));

        // Act
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            evt.Trigger(1);
            evt.Register(x => { });
        })).ToArray();

        // Assert
        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
    }

    [Test]
    public void Multiple_Handlers_Unregistering_During_Trigger_Should_Not_Throw()
    {
        // Arrange
        var evt = new PriorityEvent<int>();
        var unregisters = new List<IUnRegister>();

        for (int i = 0; i < 10; i++)
        {
            var index = i;
            var unreg = evt.Register(x =>
            {
                if (index % 2 == 0)
                {
                    unregisters[index].UnRegister();
                }
            });
            unregisters.Add(unreg);
        }

        // Act & Assert
        Assert.DoesNotThrow(() => evt.Trigger(1));
    }

    [Test]
    public void Context_Handler_Should_Receive_Correct_Data()
    {
        // Arrange
        var evt = new PriorityEvent<string>();
        string? receivedData = null;

        evt.RegisterWithContext(ctx => { receivedData = ctx.Data; });

        // Act
        evt.Trigger("test data", EventPropagation.All);

        // Assert
        Assert.That(receivedData, Is.EqualTo("test data"));
    }

    [Test]
    public void Context_Handler_MarkAsHandled_Should_Stop_UntilHandled_Propagation()
    {
        // Arrange
        var evt = new PriorityEvent<int>();
        var executionOrder = new List<int>();

        evt.RegisterWithContext(ctx =>
        {
            executionOrder.Add(1);
            ctx.MarkAsHandled();
        }, priority: 10);

        evt.RegisterWithContext(ctx => { executionOrder.Add(2); }, priority: 5);

        // Act
        evt.Trigger(42, EventPropagation.UntilHandled);

        // Assert
        Assert.That(executionOrder, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void Mixed_Normal_And_Context_Handlers_Should_Work_Together()
    {
        // Arrange
        var evt = new PriorityEvent<int>();
        var executionOrder = new List<string>();

        evt.Register(x => executionOrder.Add("normal"), priority: 5);
        evt.RegisterWithContext(ctx => executionOrder.Add("context"), priority: 10);

        // Act
        evt.Trigger(1, EventPropagation.All);

        // Assert
        Assert.That(executionOrder, Is.EqualTo(new[] { "context", "normal" }));
    }

    [Test]
    public void UntilHandled_With_Mixed_Handlers_Should_Respect_Priority()
    {
        // Arrange
        var evt = new PriorityEvent<int>();
        var executionOrder = new List<string>();

        evt.Register(x => executionOrder.Add("normal-low"), priority: 1);
        evt.RegisterWithContext(ctx =>
        {
            executionOrder.Add("context-high");
            ctx.MarkAsHandled();
        }, priority: 10);
        evt.Register(x => executionOrder.Add("normal-mid"), priority: 5);

        // Act
        evt.Trigger(1, EventPropagation.UntilHandled);

        // Assert
        Assert.That(executionOrder, Is.EqualTo(new[] { "context-high" }));
    }

    [Test]
    public void Highest_Propagation_Should_Execute_All_Highest_Priority_Handlers()
    {
        // Arrange
        var evt = new PriorityEvent<int>();
        var executionOrder = new List<string>();

        evt.Register(x => executionOrder.Add("normal-high"), priority: 10);
        evt.RegisterWithContext(ctx => executionOrder.Add("context-high"), priority: 10);
        evt.Register(x => executionOrder.Add("normal-low"), priority: 1);

        // Act
        evt.Trigger(1, EventPropagation.Highest);

        // Assert
        Assert.That(executionOrder.Count, Is.EqualTo(2));
        Assert.That(executionOrder, Does.Contain("normal-high"));
        Assert.That(executionOrder, Does.Contain("context-high"));
    }
}