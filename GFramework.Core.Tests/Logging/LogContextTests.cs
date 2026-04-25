using GFramework.Core.Abstractions.Logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     测试 LogContext 的功能和行为
/// </summary>
[TestFixture]
public class LogContextTests
{
    [SetUp]
    public void SetUp()
    {
        LogContext.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        LogContext.Clear();
    }

    [Test]
    public void Current_WhenEmpty_ShouldReturnEmptyDictionary()
    {
        var current = LogContext.Current;

        Assert.That(current, Is.Not.Null);
        Assert.That(current.Count, Is.EqualTo(0));
    }

    [Test]
    public void Push_ShouldAddPropertyToContext()
    {
        using (LogContext.Push("Key1", "Value1"))
        {
            var current = LogContext.Current;

            Assert.That(current.Count, Is.EqualTo(1));
            Assert.That(current["Key1"], Is.EqualTo("Value1"));
        }
    }

    [Test]
    public void Push_WithMultipleProperties_ShouldAddAllProperties()
    {
        using (LogContext.PushProperties(("Key1", "Value1"), ("Key2", 123)))
        {
            var current = LogContext.Current;

            Assert.That(current.Count, Is.EqualTo(2));
            Assert.That(current["Key1"], Is.EqualTo("Value1"));
            Assert.That(current["Key2"], Is.EqualTo(123));
        }
    }

    [Test]
    public void Push_WithNestedContext_ShouldMergeProperties()
    {
        using (LogContext.Push("Key1", "Value1"))
        {
            using (LogContext.Push("Key2", "Value2"))
            {
                var current = LogContext.Current;

                Assert.That(current.Count, Is.EqualTo(2));
                Assert.That(current["Key1"], Is.EqualTo("Value1"));
                Assert.That(current["Key2"], Is.EqualTo("Value2"));
            }
        }
    }

    [Test]
    public void Push_WithSameKey_ShouldOverrideValue()
    {
        using (LogContext.Push("Key1", "Value1"))
        {
            using (LogContext.Push("Key1", "Value2"))
            {
                var current = LogContext.Current;

                Assert.That(current.Count, Is.EqualTo(1));
                Assert.That(current["Key1"], Is.EqualTo("Value2"));
            }

            // 释放后应该恢复原值
            var restored = LogContext.Current;
            Assert.That(restored["Key1"], Is.EqualTo("Value1"));
        }
    }

    [Test]
    public void Dispose_ShouldRestorePreviousValue()
    {
        using (LogContext.Push("Key1", "Value1"))
        {
            Assert.That(LogContext.Current["Key1"], Is.EqualTo("Value1"));
        }

        // 释放后应该清空
        Assert.That(LogContext.Current.Count, Is.EqualTo(0));
    }

    [Test]
    public void Clear_ShouldRemoveAllProperties()
    {
        using (LogContext.Push("Key1", "Value1"))
        {
            using (LogContext.Push("Key2", "Value2"))
            {
                LogContext.Clear();

                Assert.That(LogContext.Current.Count, Is.EqualTo(0));
            }
        }
    }

    [Test]
    public void Push_WithNullKey_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => LogContext.Push(null!, "Value"));
    }

    [Test]
    public void Push_WithEmptyKey_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => LogContext.Push("", "Value"));
    }

    [Test]
    public void Push_WithWhitespaceKey_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => LogContext.Push("   ", "Value"));
    }

    [Test]
    public void Push_WithNullValue_ShouldWork()
    {
        using (LogContext.Push("Key1", null))
        {
            var current = LogContext.Current;

            Assert.That(current.Count, Is.EqualTo(1));
            Assert.That(current["Key1"], Is.Null);
        }
    }

    [Test]
    public async Task Push_InAsyncContext_ShouldIsolateAcrossThreads()
    {
        var task1Values = new List<object?>();
        var task2Values = new List<object?>();

        var task1 = Task.Run(async () =>
        {
            using (LogContext.Push("TaskId", "Task1"))
            {
                task1Values.Add(LogContext.Current["TaskId"]);
                await Task.Delay(50);
                task1Values.Add(LogContext.Current["TaskId"]);
            }
        });

        var task2 = Task.Run(async () =>
        {
            using (LogContext.Push("TaskId", "Task2"))
            {
                task2Values.Add(LogContext.Current["TaskId"]);
                await Task.Delay(50);
                task2Values.Add(LogContext.Current["TaskId"]);
            }
        });

        await Task.WhenAll(task1, task2);

        Assert.That(task1Values, Has.All.EqualTo("Task1"));
        Assert.That(task2Values, Has.All.EqualTo("Task2"));
    }

    [Test]
    public void Push_WithComplexObject_ShouldStoreReference()
    {
        var obj = new { Name = "Test", Value = 123 };

        using (LogContext.Push("Object", obj))
        {
            var current = LogContext.Current;

            Assert.That(current["Object"], Is.SameAs(obj));
        }
    }

    [Test]
    public void Push_MultipleDispose_ShouldBeIdempotent()
    {
        var disposable = LogContext.Push("Key1", "Value1");

        disposable.Dispose();
        disposable.Dispose(); // 第二次调用不应该抛出异常

        Assert.That(LogContext.Current.Count, Is.EqualTo(0));
    }
}
