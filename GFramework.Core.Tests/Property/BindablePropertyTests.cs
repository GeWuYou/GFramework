// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Property;
using NUnit.Framework;

namespace GFramework.Core.Tests.Property;

/// <summary>
///     BindableProperty类的单元测试
/// </summary>
[TestFixture]
public class BindablePropertyTests
{
    /// <summary>
    ///     测试清理方法，在每个测试方法执行后重置比较器
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        BindableProperty<string>.Comparer = (a, b) => a?.Equals(b) ?? b == null;
    }

    /// <summary>
    ///     测试获取值时应返回默认值
    /// </summary>
    [Test]
    public void Value_Get_Should_Return_Default_Value()
    {
        var property = new BindableProperty<int>(5);

        Assert.That(property.Value, Is.EqualTo(5));
    }

    /// <summary>
    ///     测试设置值时应触发事件
    /// </summary>
    [Test]
    public void Value_Set_Should_Trigger_Event()
    {
        var property = new BindableProperty<int>();
        var receivedValue = 0;

        property.Register(value => { receivedValue = value; });

        property.Value = 42;

        Assert.That(receivedValue, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试设置相同值时不触发事件
    /// </summary>
    [Test]
    public void Value_Set_To_Same_Value_Should_Not_Trigger_Event()
    {
        var property = new BindableProperty<int>(5);
        var count = 0;

        property.Register(_ => { count++; });

        property.Value = 5;

        Assert.That(count, Is.EqualTo(0));
    }

    /// <summary>
    ///     测试取消注册应移除处理器
    /// </summary>
    [Test]
    public void UnRegister_Should_Remove_Handler()
    {
        var property = new BindableProperty<int>();
        var count = 0;

        Action<int> handler = _ => { count++; };
        property.Register(handler);

        property.Value = 1;
        Assert.That(count, Is.EqualTo(1));

        property.UnRegister(handler);
        property.Value = 2;
        Assert.That(count, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试带初始值注册应立即调用处理器
    /// </summary>
    [Test]
    public void RegisterWithInitValue_Should_Call_Handler_Immediately()
    {
        var property = new BindableProperty<int>(5);
        var receivedValue = 0;

        property.RegisterWithInitValue(value => { receivedValue = value; });

        Assert.That(receivedValue, Is.EqualTo(5));
    }

    /// <summary>
    ///     测试无事件设置值不应触发事件
    /// </summary>
    [Test]
    public void SetValueWithoutEvent_Should_Not_Trigger_Event()
    {
        var property = new BindableProperty<int>();
        var count = 0;

        property.Register(_ => { count++; });

        property.SetValueWithoutEvent(42);

        Assert.That(count, Is.EqualTo(0));
        Assert.That(property.Value, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试使用自定义比较器
    /// </summary>
    [Test]
    public void WithComparer_Should_Use_Custom_Comparer()
    {
        var comparerWasCalled = false;
        var comparisonResult = false;

        // 设置自定义比较器
        BindableProperty<string>.Comparer = (a, b) =>
        {
            comparerWasCalled = true;
            comparisonResult = a.Length == b.Length;
            return comparisonResult;
        };

        var property = new BindableProperty<string>("test");
        var count = 0;

        property.Register(_ => { count++; });
        property.Value = "test";

        Assert.That(comparerWasCalled, Is.True, "自定义比较器应该被调用");
        Assert.That(comparisonResult, Is.True, "比较结果应该是true（相同长度）");
        Assert.That(count, Is.EqualTo(0), "不应该触发事件");
    }

    /// <summary>
    ///     测试多个处理器都应被调用
    /// </summary>
    [Test]
    public void Multiple_Handlers_Should_All_Be_Called()
    {
        var property = new BindableProperty<int>();
        var count1 = 0;
        var count2 = 0;

        property.Register(_ => { count1++; });
        property.Register(_ => { count2++; });

        property.Value = 42;

        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试注册应返回IUnRegister接口
    /// </summary>
    [Test]
    public void Register_Should_Return_IUnRegister()
    {
        var property = new BindableProperty<int>();
        var unRegister = property.Register(_ => { });

        Assert.That(unRegister, Is.Not.Null);
    }

    /// <summary>
    ///     测试ToString应返回值的字符串表示
    /// </summary>
    [Test]
    public void ToString_Should_Return_Value_As_String()
    {
        var property = new BindableProperty<int>(42);

        var result = property.ToString();

        Assert.That(result, Is.EqualTo("42"));
    }

    /// <summary>
    ///     测试并发场景下的属性值设置
    /// </summary>
    [Test]
    public void Concurrent_Value_Set_Should_Be_Thread_Safe()
    {
        var property = new BindableProperty<int>(0);
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();

        for (var i = 0; i < threadCount; i++)
        {
            var threadId = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    for (var j = 0; j < iterationsPerThread; j++)
                    {
                        property.Value = threadId * iterationsPerThread + j;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty, $"并发测试中发生异常: {string.Join(", ", exceptions.Select(e => e.Message))}");
    }

    /// <summary>
    ///     测试并发场景下的事件注册和触发
    /// </summary>
    [Test]
    public void Concurrent_Register_And_Trigger_Should_Be_Thread_Safe()
    {
        var property = new BindableProperty<int>(0);
        const int threadCount = 5;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();
        var callCounts = new int[threadCount];

        for (var i = 0; i < threadCount; i++)
        {
            var threadId = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    property.Register(value => { Interlocked.Increment(ref callCounts[threadId]); });
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        // 触发事件
        property.Value = 42;

        Assert.That(exceptions, Is.Empty);
        Assert.That(callCounts.Sum(), Is.EqualTo(threadCount), "所有注册的处理器都应该被调用");
    }

    /// <summary>
    ///     测试并发场景下的注册和取消注册
    /// </summary>
    [Test]
    public void Concurrent_Register_And_UnRegister_Should_Be_Thread_Safe()
    {
        var property = new BindableProperty<int>(0);
        const int threadCount = 10;
        const int iterationsPerThread = 50;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();

        for (var i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    for (var j = 0; j < iterationsPerThread; j++)
                    {
                        Action<int> handler = _ => { };
                        property.Register(handler);
                        property.UnRegister(handler);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty);
    }

    /// <summary>
    ///     测试并发场景下RegisterWithInitValue的线程安全性
    /// </summary>
    [Test]
    public void Concurrent_RegisterWithInitValue_Should_Be_Thread_Safe()
    {
        var property = new BindableProperty<int>(42);
        const int threadCount = 10;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();
        var receivedValues = new List<int>();

        for (var i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    property.RegisterWithInitValue(value =>
                    {
                        lock (receivedValues)
                        {
                            receivedValues.Add(value);
                        }
                    });
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty);
        Assert.That(receivedValues.Count, Is.EqualTo(threadCount));
        Assert.That(receivedValues.All(v => v == 42), Is.True, "所有初始值都应该是42");
    }
}